#if UNITY_ANDROID && !UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Spins a cube above the sample's UI, so session replays show motion, and logs frame timings to logcat under the
// 'ReplayBenchmark' tag, to compare session replay's capture modes (see SentryOptionConfiguration).
// Android players only. The GPU and per-thread timings need 'Frame Timing Stats' in the player settings.
public class ReplayBenchmark : MonoBehaviour
{
    private const string LogTag = "ReplayBenchmark";
    private const float WarmUpSeconds = 5f;
    private const float ReportSeconds = 10f;
    // A layer of its own, so the cube's camera and light leave the sample's scenes alone.
    private const int CubeLayer = 31;

    private readonly List<float> _frameTimes = new();
    private readonly FrameTiming[] _timings = new FrameTiming[1];
    private double _cpuMainMs;
    private double _cpuRenderMs;
    private double _gpuMs;
    private int _timingCount;
    private float _collectFrom;
    private float _nextReport;
    private Transform _cube;
    private string _mode;
    private AndroidJavaClass _log;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Spawn()
    {
        var benchmark = new GameObject("ReplayBenchmark");
        DontDestroyOnLoad(benchmark);
        benchmark.AddComponent<ReplayBenchmark>();
    }

    private void Start()
    {
        Application.targetFrameRate = 60;
        _mode = $"replay={SentryOptionConfiguration.GetLaunchExtra("sentry_replay") ?? "error"} " +
                $"capture={SentryOptionConfiguration.GetLaunchExtra("sentry_replay_capture") ?? "java"}";
        _log = new AndroidJavaClass("android.util.Log");
        _collectFrom = Time.realtimeSinceStartup + WarmUpSeconds;
        _nextReport = _collectFrom + ReportSeconds;

        var texture = new RenderTexture(256, 256, 16);

        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.layer = CubeLayer;
        cube.transform.SetParent(transform);
        cube.transform.position = new Vector3(0, -1000, 0);
        // A shader that builds always include. The primitive's default material doesn't make it into the player.
        var shader = Shader.Find("Legacy Shaders/Diffuse");
        var cubeRenderer = cube.GetComponent<Renderer>();
        if (shader != null)
        {
            cubeRenderer.material = new Material(shader);
        }
        cubeRenderer.material.color = new Color(1f, 0.45f, 0.1f);
        _cube = cube.transform;

        var cubeCamera = new GameObject("ReplayBenchmarkCamera").AddComponent<Camera>();
        cubeCamera.transform.SetParent(transform);
        cubeCamera.transform.position = _cube.position + new Vector3(0, 0, -2.5f);
        cubeCamera.cullingMask = 1 << CubeLayer;
        cubeCamera.clearFlags = CameraClearFlags.SolidColor;
        cubeCamera.backgroundColor = new Color(0.1f, 0.5f, 0.9f);
        cubeCamera.targetTexture = texture;

        var cubeLight = new GameObject("ReplayBenchmarkLight").AddComponent<Light>();
        cubeLight.transform.SetParent(transform);
        cubeLight.type = LightType.Directional;
        cubeLight.cullingMask = 1 << CubeLayer;
        cubeLight.transform.rotation = Quaternion.Euler(40, -30, 0);

        // Above the sample's UI, in the top right corner.
        var canvas = new GameObject("ReplayBenchmarkCanvas").AddComponent<Canvas>();
        canvas.transform.SetParent(transform);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        var image = new GameObject("ReplayBenchmarkCube").AddComponent<RawImage>();
        image.texture = texture;
        image.raycastTarget = false;
        var rect = image.rectTransform;
        rect.SetParent(canvas.transform, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1, 1);
        rect.anchoredPosition = new Vector2(-16, -16);
        rect.sizeDelta = new Vector2(220, 220);
    }

    private void Update()
    {
        _cube.Rotate(new Vector3(30, 60, 15) * Time.deltaTime);

        var now = Time.realtimeSinceStartup;
        if (now < _collectFrom)
        {
            return;
        }

        _frameTimes.Add(Time.unscaledDeltaTime * 1000f);
        FrameTimingManager.CaptureFrameTimings();
        if (FrameTimingManager.GetLatestTimings(1, _timings) > 0)
        {
#if UNITY_2022_1_OR_NEWER
            _cpuMainMs += _timings[0].cpuMainThreadFrameTime;
            _cpuRenderMs += _timings[0].cpuRenderThreadFrameTime;
#else
            _cpuMainMs += _timings[0].cpuFrameTime;
#endif
            _gpuMs += _timings[0].gpuFrameTime;
            _timingCount++;
        }

        if (now >= _nextReport)
        {
            Report();
            _nextReport = now + ReportSeconds;
        }
    }

    private void Report()
    {
        if (_frameTimes.Count == 0)
        {
            return;
        }

        var sum = 0f;
        foreach (var frameTime in _frameTimes)
        {
            sum += frameTime;
        }

        _frameTimes.Sort();
        var p95 = _frameTimes[Mathf.Min(_frameTimes.Count - 1, (int)(_frameTimes.Count * 0.95f))];
        var timings = _timingCount == 0
            ? "frame timing stats off"
            : $"cpu main {_cpuMainMs / _timingCount:F2} ms, " +
              $"cpu render {_cpuRenderMs / _timingCount:F2} ms, " +
              $"gpu {_gpuMs / _timingCount:F2} ms";
        _log.CallStatic<int>("i", LogTag,
            $"{_mode} frames {_frameTimes.Count}, frame avg {sum / _frameTimes.Count:F2} ms, " +
            $"p95 {p95:F2} ms, {timings}");

        _frameTimes.Clear();
        _cpuMainMs = _cpuRenderMs = _gpuMs = 0;
        _timingCount = 0;
    }
}
#endif
