using System;
using System.Collections.Generic;
using Sentry.Extensibility;

namespace Sentry.Unity.Android.Tests;

internal class TestSentryJava : ISentryJava
{
    public bool Enabled { get; set; } = true;
    public bool InitSuccessful { get; set; } = true;
    public bool SentryPresent { get; set; } = true;
    public string? InstallationId { get; set; }
    public bool? IsCrashedLastRun { get; set; }
    public bool? MainProcess { get; set; } = true;
    public bool? NdkAppHangTracking { get; set; }
    public bool InitCalled { get; private set; }
    public bool CloseCalled { get; private set; }
    public Dictionary<string, string?> Options { get; } = new();
    public string? TraceHeader { get; set; }
    public string? BaggageHeader { get; set; }
    public string? MainProcessTraceHeader { get; set; }
    public SentryUser? User { get; set; }
    public SentryUser? MainProcessUser { get; set; }
    public bool MainProcessUserReadable { get; set; } = true;
    public (SentryId TraceId, SpanId SpanId)? Trace { get; private set; }

    public bool? IsEnabled() => Enabled;

    public bool? IsMainProcess() => MainProcess;

    public void Init(SentryUnityOptions options)
    {
        InitCalled = true;
        Enabled = InitSuccessful;
    }

    public string? GetOption(string getter) => Options.TryGetValue(getter, out var value) ? value : null;

    public bool? IsNdkAppHangTrackingEnabled() => NdkAppHangTracking;

    public string? GetTraceHeader() => TraceHeader;

    public string? GetBaggageHeader() => BaggageHeader;

    public string? GetMainProcessTraceHeader() => MainProcessTraceHeader;

    public SentryUser? GetUser() => User;

    public bool TryGetMainProcessUser(out SentryUser? user)
    {
        user = MainProcessUserReadable ? MainProcessUser : null;
        return MainProcessUserReadable;
    }

    public string? GetInstallationId() => InstallationId;

    public bool? CrashedLastRun() => IsCrashedLastRun;

    public void Close() => CloseCalled = true;

    public void WriteScope(
        string? AppStartTime,
        string? AppBuildType,
        int? GpuId,
        string? GpuName,
        string? GpuVendorName,
        int? GpuMemorySize,
        string? GpuNpotSupport,
        string? GpuVersion,
        string? GpuApiType,
        int? GpuMaxTextureSize,
        bool? GpuSupportsDrawCallInstancing,
        bool? GpuSupportsRayTracing,
        bool? GpuSupportsComputeShaders,
        bool? GpuSupportsGeometryShaders,
        string? GpuVendorId,
        bool? GpuMultiThreadedRendering,
        string? GpuGraphicsShaderLevel)
    { }

    public bool IsSentryJavaPresent() => SentryPresent;
    public void AddBreadcrumb(Breadcrumb breadcrumb) { }

    public void SetExtra(string key, string? value) { }
    public void SetTag(string key, string? value) { }

    public void UnsetTag(string key) { }

    public void SetUser(SentryUser user) { }

    public void UnsetUser() { }

    public void SetTrace(SentryId traceId, SpanId spanId) => Trace = (traceId, spanId);

    public void SetEnvironment(string? environment) { }

    public void AddAttachment(string path, string fileName, string? contentType) { }

    public void AddAttachmentBytes(byte[] data, string fileName, string? contentType) { }

    public void ClearAttachments() { }
}
