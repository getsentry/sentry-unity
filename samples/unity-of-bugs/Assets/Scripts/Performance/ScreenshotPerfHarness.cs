using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Sentry;
using Sentry.Extensibility;
using Sentry.Unity;
using Unity.Profiling;
using UnityEngine;
using Debug = UnityEngine.Debug;

/// <summary>
/// Measures the performance impact of attaching screenshots to events.
/// </summary>
/// <remarks>
/// Runs every configuration in a single session, re-initializing the SDK for each, and writes the results to
/// `persistentDataPath/screenshot-perf/`. Configurations are repeated round-robin, so slow drift such as thermal
/// throttling spreads evenly across all of them instead of skewing the ones that run last. Only relies on the public SDK API so the same harness can
/// be built against different SDK versions for comparison.
/// </remarks>
public class ScreenshotPerfHarness : MonoBehaviour
{
    public const string SceneName = "ScreenshotPerf";

    // Events get sent to the sample's project, so the screenshot encode - deferred until the envelope gets
    // serialized - actually happens. The DSN is read from the sample's options at runtime.
    private const string ConfigTag = "screenshot_perf";

    private const float WarmupSeconds = 5f;
    private const float IdleSeconds = 20f;
    private const float PeriodicSeconds = 20f;
    private const float PeriodicErrorIntervalSeconds = 1f;
    private const float BurstSettleSeconds = 3f;
    private const int BurstSize = 50;
    private const int CubeCount = 400;
    private const int DefaultRounds = 3;

    // Optional filters, passed as environment variables on launch
    private const string ConfigsVariable = "SCREENSHOT_PERF_CONFIGS";
    private const string RoundsVariable = "SCREENSHOT_PERF_ROUNDS";

    private readonly struct Config
    {
        public readonly string Name;
        public readonly bool AttachScreenshot;
        public readonly ScreenshotQuality Quality;
        public readonly int IntervalMs;

        public Config(string name, bool attachScreenshot, ScreenshotQuality quality, int intervalMs)
        {
            Name = name;
            AttachScreenshot = attachScreenshot;
            Quality = quality;
            IntervalMs = intervalMs;
        }
    }

    private static readonly Config[] AllConfigs =
    {
        new("baseline", false, ScreenshotQuality.High, 250),
        new("high-250ms", true, ScreenshotQuality.High, 250),
        new("high-100ms", true, ScreenshotQuality.High, 100),
        new("high-1000ms", true, ScreenshotQuality.High, 1000),
        new("low-250ms", true, ScreenshotQuality.Low, 250),
        new("full-250ms", true, ScreenshotQuality.Full, 250),
    };

    private struct FrameSample
    {
        public int PhaseIndex;
        public float DeltaMs;
        public float CpuMainMs;
        public float CpuMainWaitMs;
        public float CpuRenderMs;
        public float GpuMs;
        public long GcBytes;
        public float RefreshMs;
        public float ReadbackMs;
        public float EncodeMs;
        public float ProcessMs;
    }

    private struct CaptureSample
    {
        public int ConfigIndex;
        public int Round;
        public int PhaseIndex;
        public bool MainThread;
        public double Ms;
        public double BeforeProcessorsMs;
        public double ProcessorsMs;
        public double AfterProcessorsMs;
    }

    // Splits each capture into segments, taken on the capturing thread. The harness' event processor gets
    // registered before the SDK's own ones and BeforeSend runs once all processors are done.
    // Filled from background threads: the SDK's worker logs encodes, the transport sends envelopes
    private readonly ConcurrentQueue<string> _encodes = new();
    private readonly ConcurrentQueue<string> _sent = new();
    private string? _dsn;

    [ThreadStatic] private static long _processorsStart;
    [ThreadStatic] private static long _processorsEnd;

    // Only picks up the screenshot encode timings and ignores everything else, so having debug logging on stays cheap
    private sealed class EncodeLogCollector : IDiagnosticLogger
    {
        private readonly ScreenshotPerfHarness _harness;

        public EncodeLogCollector(ScreenshotPerfHarness harness) => _harness = harness;

        public bool IsEnabled(SentryLevel level) => level == SentryLevel.Debug;

        public void Log(SentryLevel logLevel, string message, Exception? exception = null, params object?[] args)
        {
            if (!message.StartsWith("Screenshot encoded in", StringComparison.Ordinal) || args.Length < 2)
            {
                return;
            }

            // Encodes run on the SDK's worker shortly after the capture and get attributed to the current phase
            _harness._encodes.Enqueue(string.Join(",",
                _harness._configs[Math.Min(_harness._configIndex, _harness._configs.Length - 1)].Name,
                Math.Min(_harness._round, _harness._rounds - 1), Phases[Math.Max(0, _harness._phaseIndex)],
                FormatMs(Convert.ToDouble(args[0], CultureInfo.InvariantCulture)), args[1]));
        }
    }

    // Records whether events leave the device with their screenshot attached
    private sealed class EnvelopeInspector : DelegatingHandler
    {
        private static readonly Regex ConfigTagPattern = new(@"""screenshot_perf"":""([^""/]+)/(\d+)""");
        private static readonly Regex ScreenshotHeaderPattern = new(@"\{[^\n]*""filename"":""screenshot\.jpg""[^\n]*\}");
        private static readonly Regex LengthPattern = new(@"""length"":(\d+)");

        private readonly ScreenshotPerfHarness _harness;

        public EnvelopeInspector(ScreenshotPerfHarness harness)
        {
            _harness = harness;
            InnerHandler = new HttpClientHandler();
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            try
            {
                if (request.Content is { } content)
                {
                    await content.LoadIntoBufferAsync().ConfigureAwait(false);
                    var body = await content.ReadAsByteArrayAsync().ConfigureAwait(false);
                    if (content.Headers.ContentEncoding.Contains("gzip"))
                    {
                        using var compressed = new GZipStream(new MemoryStream(body), CompressionMode.Decompress);
                        using var decompressed = new MemoryStream();
                        compressed.CopyTo(decompressed);
                        body = decompressed.ToArray();
                    }

                    Inspect(Encoding.UTF8.GetString(body));
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ScreenshotPerf] Failed to inspect an envelope: {e.Message}");
            }

            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

        private void Inspect(string envelope)
        {
            var tag = ConfigTagPattern.Match(envelope);
            if (!tag.Success)
            {
                return;
            }

            var header = ScreenshotHeaderPattern.Match(envelope);
            var length = header.Success ? LengthPattern.Match(header.Value) : Match.Empty;
            _harness._sent.Enqueue(string.Join(",", tag.Groups[1].Value, tag.Groups[2].Value,
                header.Success ? 1 : 0, length.Success ? length.Groups[1].Value : "0"));
        }
    }

    private sealed class TimestampProcessor : ISentryEventProcessor
    {
        public SentryEvent Process(SentryEvent @event)
        {
            _processorsStart = Stopwatch.GetTimestamp();
            return @event;
        }
    }

    private static readonly string[] Phases = { "warmup", "idle", "periodic", "burst-main", "burst-background" };

    // Preallocated so the harness itself doesn't allocate while measuring
    private readonly FrameSample[] _frames = new FrameSample[60 * 120];
    private int _frameCount;
    private readonly ConcurrentQueue<CaptureSample> _captures = new();
    private readonly FrameTiming[] _frameTimings = new FrameTiming[1];
    private readonly StringBuilder _memory = new();

    private ProfilerRecorder _gcAlloc;
    private ProfilerRecorder _gfxMemory;
    private ProfilerRecorder _refresh;
    private ProfilerRecorder _readback;
    private ProfilerRecorder _encode;
    private ProfilerRecorder _process;

    private Config[] _configs = AllConfigs;
    private int _rounds = DefaultRounds;
    private int _configIndex;
    private int _round;
    private int _phaseIndex = -1;
    private string _outputDirectory = null!;
    private Transform[] _cubes = Array.Empty<Transform>();
    private TextMesh? _status;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == SceneName
            && FindObjectOfType<ScreenshotPerfHarness>() == null)
        {
            new GameObject(nameof(ScreenshotPerfHarness)).AddComponent<ScreenshotPerfHarness>();
        }
    }

    private void Awake()
    {
        Application.targetFrameRate = 60;
        BuildScene();

        _gcAlloc = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
        _gfxMemory = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "Gfx Used Memory");
        _refresh = ProfilerRecorder.StartNew(ProfilerCategory.Scripts, "Sentry.Screenshot.Refresh");
        _readback = ProfilerRecorder.StartNew(ProfilerCategory.Scripts, "Sentry.Screenshot.ReadbackComplete");
        _encode = ProfilerRecorder.StartNew(ProfilerCategory.Scripts, "Sentry.Screenshot.Encode");
        _process = ProfilerRecorder.StartNew(ProfilerCategory.Scripts, "Sentry.Screenshot.Process");

        ReadFilters();

        _dsn = Resources.Load<ScriptableSentryUnityOptions>("Sentry/SentryOptions")?.Dsn;
        if (string.IsNullOrEmpty(_dsn))
        {
            Debug.LogError("[ScreenshotPerf] The sample's options contain no DSN. Envelopes won't get sent.");
        }

        var runId = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
        _outputDirectory = Path.Combine(Application.persistentDataPath, "screenshot-perf", runId);
        Directory.CreateDirectory(_outputDirectory);
    }

    private void OnDestroy()
    {
        _gcAlloc.Dispose();
        _gfxMemory.Dispose();
        _refresh.Dispose();
        _readback.Dispose();
        _encode.Dispose();
        _process.Dispose();
    }

    private IEnumerator Start()
    {
        // Let the auto-initialized SDK (if any) settle, then take over
        yield return null;
        SentrySdk.Close();

        WriteHeaders();

        for (_round = 0; _round < _rounds; _round++)
        for (_configIndex = 0; _configIndex < _configs.Length; _configIndex++)
        {
            var config = _configs[_configIndex];
            SetStatus($"{config.Name} ({_configIndex + 1}/{_configs.Length}), round {_round + 1}/{_rounds}");
            Debug.Log($"[ScreenshotPerf] Round {_round + 1}/{_rounds}: starting configuration '{config.Name}'.");

            InitSdk(config);
            SentrySdk.ConfigureScope(scope => scope.SetTag(ConfigTag, $"{config.Name}/{_round}"));
            _frameCount = 0;

            yield return RunPhase(0, WarmupSeconds, captureInterval: 0f);
            yield return RunPhase(1, IdleSeconds, captureInterval: 0f);
            _memory.AppendLine(string.Join(",", config.Name, _round, ToMb(_gfxMemory.LastValue), ToMb(GC.GetTotalMemory(false))));
            yield return RunPhase(2, PeriodicSeconds, PeriodicErrorIntervalSeconds);
            yield return RunBurst(3, mainThread: true);
            yield return RunBurst(4, mainThread: false);

            SentrySdk.Close();
            AppendResults(config);
            yield return new WaitForSecondsRealtime(1f);
        }

        AppendLines("encodes.csv", _encodes);
        AppendLines("sent.csv", _sent);
        File.WriteAllText(Path.Combine(_outputDirectory, "memory.csv"), "config,round,gfx_used_mb,gc_used_mb\n" + _memory);
        File.WriteAllText(Path.Combine(_outputDirectory, "done"), DateTime.UtcNow.ToString("O"));
        SetStatus("Done");
        Debug.Log($"[ScreenshotPerf] Done. Results in {_outputDirectory}");

        yield return new WaitForSecondsRealtime(1f);
        Application.Quit();
    }

    private void ReadFilters()
    {
        var configs = Environment.GetEnvironmentVariable(ConfigsVariable);
        if (!string.IsNullOrWhiteSpace(configs))
        {
            var names = new HashSet<string>(configs.Split(',').Select(name => name.Trim()));
            _configs = AllConfigs.Where(config => names.Contains(config.Name)).ToArray();
            if (_configs.Length == 0)
            {
                Debug.LogError($"[ScreenshotPerf] No configuration matches '{configs}'. Running all of them.");
                _configs = AllConfigs;
            }
        }

        if (int.TryParse(Environment.GetEnvironmentVariable(RoundsVariable), out var rounds) && rounds > 0)
        {
            _rounds = rounds;
        }

        Debug.Log($"[ScreenshotPerf] Running {string.Join(", ", _configs.Select(config => config.Name))} for {_rounds} round(s).");
    }

    private void InitSdk(Config config)
    {
        SentrySdk.Init(options =>
        {
            options.Dsn = _dsn;

            // Offline caching stays on, as it is by default. It serializes envelopes to disk on the SDK's worker,
            // which is where the deferred screenshot encode happens.
            options.Debug = true;
            options.DiagnosticLevel = SentryLevel.Debug;
            options.DiagnosticLogger = new EncodeLogCollector(this);
            options.CreateHttpMessageHandler = () => new EnvelopeInspector(this);

            options.AddEventProcessor(new TimestampProcessor());
            options.SetBeforeSend((@event, _) =>
            {
                _processorsEnd = Stopwatch.GetTimestamp();
                return @event;
            });

            options.AutoSessionTracking = false;

            // Native SDKs don't play a part in managed screenshots and re-initializing them per configuration
            // would only add noise
            options.IosNativeSupportEnabled = false;
            options.AndroidNativeSupportEnabled = false;

            options.AttachScreenshot = config.AttachScreenshot;
            options.ScreenshotQuality = config.Quality;

            // Not available in every SDK version
            var interval = typeof(SentryUnityOptions).GetProperty("ScreenshotCaptureInterval", BindingFlags.Public | BindingFlags.Instance);
            interval?.SetValue(options, TimeSpan.FromMilliseconds(config.IntervalMs));
        });
    }

    private IEnumerator RunPhase(int phaseIndex, float seconds, float captureInterval)
    {
        _phaseIndex = phaseIndex;
        var end = Time.realtimeSinceStartup + seconds;
        var nextCapture = Time.realtimeSinceStartup + captureInterval;
        var mainThread = true;

        while (Time.realtimeSinceStartup < end)
        {
            if (captureInterval > 0 && Time.realtimeSinceStartup >= nextCapture)
            {
                Capture(mainThread);
                mainThread = !mainThread;
                nextCapture += captureInterval;
            }

            yield return null;
        }
    }

    private IEnumerator RunBurst(int phaseIndex, bool mainThread)
    {
        _phaseIndex = phaseIndex;
        yield return null;

        if (mainThread)
        {
            for (var i = 0; i < BurstSize; i++)
            {
                Capture(true);
            }
        }
        else
        {
            // Parallel.For blocks the calling thread, so it gets kicked off from the thread pool
            Task.Run(() => Parallel.For(0, BurstSize, _ => CaptureAndRecord(false)));
        }

        var end = Time.realtimeSinceStartup + BurstSettleSeconds;
        while (Time.realtimeSinceStartup < end)
        {
            yield return null;
        }
    }

    private void Capture(bool mainThread)
    {
        if (mainThread)
        {
            CaptureAndRecord(true);
        }
        else
        {
            Task.Run(() => CaptureAndRecord(false));
        }
    }

    private void CaptureAndRecord(bool mainThread)
    {
        var configIndex = _configIndex;
        var round = _round;
        var phaseIndex = _phaseIndex;
        _processorsStart = 0;
        _processorsEnd = 0;

        var start = Stopwatch.GetTimestamp();
        SentrySdk.CaptureException(new InvalidOperationException("Screenshot performance test"));
        var end = Stopwatch.GetTimestamp();

        // Segments stay negative when a timestamp was not taken, e.g. the event got dropped before BeforeSend
        var processorsStart = _processorsStart;
        var processorsEnd = _processorsEnd;
        var hasSegments = processorsStart != 0 && processorsEnd != 0;

        _captures.Enqueue(new CaptureSample
        {
            ConfigIndex = configIndex,
            Round = round,
            PhaseIndex = phaseIndex,
            MainThread = mainThread,
            Ms = TicksToMs(end - start),
            BeforeProcessorsMs = hasSegments ? TicksToMs(processorsStart - start) : -1,
            ProcessorsMs = hasSegments ? TicksToMs(processorsEnd - processorsStart) : -1,
            AfterProcessorsMs = hasSegments ? TicksToMs(end - processorsEnd) : -1,
        });
    }

    private void Update()
    {
        for (var i = 0; i < _cubes.Length; i++)
        {
            _cubes[i].Rotate(30f * Time.deltaTime, 45f * Time.deltaTime, 0f);
        }

        if (_phaseIndex < 0 || _frameCount >= _frames.Length)
        {
            return;
        }

        // Frame timings lag a few frames behind. The main thread's frame time already excludes waiting for present,
        // which is reported separately.
        FrameTimingManager.CaptureFrameTimings();
        var hasTiming = FrameTimingManager.GetLatestTimings(1, _frameTimings) > 0;
        var timing = _frameTimings[0];

        // Recorders report the previous frame
        _frames[_frameCount++] = new FrameSample
        {
            PhaseIndex = _phaseIndex,
            DeltaMs = Time.unscaledDeltaTime * 1000f,
            CpuMainMs = hasTiming ? (float)timing.cpuMainThreadFrameTime : -1f,
            CpuMainWaitMs = hasTiming ? (float)timing.cpuMainThreadPresentWaitTime : -1f,
            CpuRenderMs = hasTiming ? (float)timing.cpuRenderThreadFrameTime : -1f,
            GpuMs = hasTiming ? (float)timing.gpuFrameTime : -1f,
            GcBytes = _gcAlloc.LastValue,
            RefreshMs = ToMs(_refresh.LastValue),
            ReadbackMs = ToMs(_readback.LastValue),
            EncodeMs = ToMs(_encode.LastValue),
            ProcessMs = ToMs(_process.LastValue),
        };
    }

    private void WriteHeaders()
    {
        File.WriteAllText(Path.Combine(_outputDirectory, "frames.csv"),
            "config,round,phase,dt_ms,cpu_main_ms,cpu_main_wait_ms,cpu_render_ms,gpu_ms,gc_bytes,refresh_ms,readback_ms,encode_ms,process_ms\n");
        File.WriteAllText(Path.Combine(_outputDirectory, "encodes.csv"), "config,round,phase,ms,bytes\n");
        File.WriteAllText(Path.Combine(_outputDirectory, "sent.csv"), "config,round,has_screenshot,screenshot_bytes\n");
        File.WriteAllText(Path.Combine(_outputDirectory, "captures.csv"),
            "config,round,phase,thread,ms,before_processors_ms,processors_ms,after_processors_ms\n");

        var device = new StringBuilder();
        device.AppendLine($"device_model,{SystemInfo.deviceModel}");
        device.AppendLine($"os,{SystemInfo.operatingSystem}");
        device.AppendLine($"graphics_api,{SystemInfo.graphicsDeviceType}");
        device.AppendLine($"gpu,{SystemInfo.graphicsDeviceName}");
        device.AppendLine($"screen,{Screen.width}x{Screen.height}");
        device.AppendLine($"async_gpu_readback,{SystemInfo.supportsAsyncGPUReadback}");
        device.AppendLine($"unity,{Application.unityVersion}");
        device.AppendLine($"development_build,{Debug.isDebugBuild}");
        device.AppendLine($"rounds,{_rounds}");
        File.WriteAllText(Path.Combine(_outputDirectory, "device.csv"), device.ToString());
    }

    private void AppendResults(Config config)
    {
        var frames = new StringBuilder();
        for (var i = 0; i < _frameCount; i++)
        {
            var f = _frames[i];
            frames.Append(config.Name).Append(',').Append(_round).Append(',').Append(Phases[f.PhaseIndex]).Append(',')
                .Append(Format(f.DeltaMs)).Append(',').Append(Format(f.CpuMainMs)).Append(',')
                .Append(Format(f.CpuMainWaitMs)).Append(',').Append(Format(f.CpuRenderMs)).Append(',')
                .Append(Format(f.GpuMs)).Append(',').Append(f.GcBytes).Append(',')
                .Append(Format(f.RefreshMs)).Append(',').Append(Format(f.ReadbackMs)).Append(',')
                .Append(Format(f.EncodeMs)).Append(',').Append(Format(f.ProcessMs)).Append('\n');
        }
        File.AppendAllText(Path.Combine(_outputDirectory, "frames.csv"), frames.ToString());

        var captures = new StringBuilder();
        while (_captures.TryDequeue(out var c))
        {
            captures.Append(_configs[c.ConfigIndex].Name).Append(',').Append(c.Round).Append(',').Append(Phases[c.PhaseIndex]).Append(',')
                .Append(c.MainThread ? "main" : "background").Append(',')
                .Append(FormatMs(c.Ms)).Append(',').Append(FormatMs(c.BeforeProcessorsMs)).Append(',')
                .Append(FormatMs(c.ProcessorsMs)).Append(',').Append(FormatMs(c.AfterProcessorsMs)).Append('\n');
        }
        File.AppendAllText(Path.Combine(_outputDirectory, "captures.csv"), captures.ToString());

        AppendLines("encodes.csv", _encodes);
        AppendLines("sent.csv", _sent);
    }

    private void AppendLines(string fileName, ConcurrentQueue<string> lines)
    {
        var text = new StringBuilder();
        while (lines.TryDequeue(out var line))
        {
            text.Append(line).Append('\n');
        }
        File.AppendAllText(Path.Combine(_outputDirectory, fileName), text.ToString());
    }

    private void BuildScene()
    {
        var camera = new GameObject("Camera").AddComponent<Camera>();
        camera.transform.position = new Vector3(0f, 0f, -22f);
        camera.backgroundColor = new Color(0.1f, 0.1f, 0.15f);
        camera.clearFlags = CameraClearFlags.SolidColor;

        var light = new GameObject("Light").AddComponent<Light>();
        light.type = LightType.Directional;
        light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        var random = new System.Random(42);
        _cubes = new Transform[CubeCount];
        for (var i = 0; i < CubeCount; i++)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.position = new Vector3((i % 20 - 9.5f) * 1.6f, (i / 20 - 9.5f) * 1.6f, (float)random.NextDouble() * 4f);
            cube.GetComponent<Renderer>().material.color = Color.HSVToRGB((float)random.NextDouble(), 0.7f, 0.9f);
            _cubes[i] = cube.transform;
        }

        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var statusObject = new GameObject("Status");
        statusObject.AddComponent<MeshRenderer>().sharedMaterial = font.material;
        _status = statusObject.AddComponent<TextMesh>();
        _status.font = font;
        _status.transform.position = new Vector3(-14f, 17f, -2f);
        _status.characterSize = 0.5f;
        _status.fontSize = 48;
        _status.color = Color.white;
    }

    private void SetStatus(string text)
    {
        if (_status != null)
        {
            _status.text = $"Screenshot perf: {text}";
        }
    }

    private static float ToMs(long nanoseconds) => nanoseconds / 1_000_000f;
    private static double TicksToMs(long ticks) => ticks * 1000.0 / Stopwatch.Frequency;
    private static string FormatMs(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
    private static string ToMb(long bytes) => (bytes / (1024f * 1024f)).ToString("0.0", CultureInfo.InvariantCulture);
    private static string Format(float value) => value.ToString("0.###", CultureInfo.InvariantCulture);
}
