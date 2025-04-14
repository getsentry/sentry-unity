using System;
using Sentry.Extensibility;

namespace Sentry.Unity.Android.Tests;

internal class TestSentryJava : ISentryJava
{
    public bool Enabled { get; set; } = true;
    public bool InitSuccessful { get; set; } = true;
    public bool SentryPresent { get; set; } = true;
    public string? InstallationId { get; set; }
    public bool? IsCrashedLastRun { get; set; }

    public bool? IsEnabled(TimeSpan timeout) => Enabled;

    public void Init(SentryUnityOptions options, TimeSpan timeout) { }

    public string? GetInstallationId() => InstallationId;

    public bool? CrashedLastRun() => IsCrashedLastRun;

    public void Close() { }

    public void WriteScope(
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
    public void AddBreadCrumb(Breadcrumb breadcrumb) { }

    public void SetExtra(string key, string? value) { }
    public void SetTag(string key, string? value) { }

    public void UnsetTag(string key) { }

    public void SetUser(SentryUser user) { }

    public void UnsetUser() { }

    public void SetTrace(SentryId traceId, SpanId spanId) { }
    public void SetLogger(IDiagnosticLogger? optionsDiagnosticLogger)
    {

    }
}
