using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Sentry.Unity.Tests.SharedClasses;

namespace Sentry.Unity.Editor.Tests.Native;

/// <summary>
/// <c>Sentry.Unity.Native.dll</c> is excluded from the Editor, so it is loaded from the package and the bridge is
/// reached through reflection. Nothing here calls into sentry-native.
/// </summary>
public class SentryNativeBridgeTests
{
    private const int SentryThreadStackwalkModeAll = 2;

    private static void SetAppHangThreadStackwalkMode(SentryUnityOptions options, Action<int> setMode)
    {
        var packageRoot = Path.GetFullPath(Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "..", ".."));
        var nativeAssemblyPath = Path.Combine(packageRoot, "Runtime", "Sentry.Unity.Native.dll");
        Assert.That(File.Exists(nativeAssemblyPath), Is.True, $"Native assembly not found at {nativeAssemblyPath}");

        var bridge = Assembly.LoadFrom(nativeAssemblyPath).GetType("Sentry.Unity.Native.SentryNativeBridge", throwOnError: true)!;
        var method = bridge.GetMethod(nameof(SetAppHangThreadStackwalkMode), BindingFlags.NonPublic | BindingFlags.Static);
        Assert.That(method, Is.Not.Null);

        try
        {
            method!.Invoke(null, new object[] { options, setMode });
        }
        catch (TargetInvocationException e) when (e.InnerException is not null)
        {
            throw e.InnerException;
        }
    }

    [Test]
    public void SetAppHangThreadStackwalkMode_OptionOff_DoesNotCallNative()
    {
        var options = new SentryUnityOptions { EnableAppHangTracking = true };
        var modes = new List<int>();

        SetAppHangThreadStackwalkMode(options, modes.Add);

        Assert.That(modes, Is.Empty);
    }

    [Test]
    public void SetAppHangThreadStackwalkMode_OptionOn_SetsModeAll()
    {
        var options = new SentryUnityOptions { EnableAppHangTracking = true, AttachAllThreadsToAppHangs = true };
        var modes = new List<int>();

        SetAppHangThreadStackwalkMode(options, modes.Add);

        Assert.That(modes, Is.EqualTo(new[] { SentryThreadStackwalkModeAll }));
    }

    [Test]
    public void SetAppHangThreadStackwalkMode_NativeFunctionMissing_LogsWarningAndContinues()
    {
        var logger = new TestLogger();
        var options = new SentryUnityOptions
        {
            AttachAllThreadsToAppHangs = true,
            Debug = true,
            DiagnosticLevel = SentryLevel.Debug,
            DiagnosticLogger = logger,
        };

        Assert.DoesNotThrow(() => SetAppHangThreadStackwalkMode(options,
            _ => throw new EntryPointNotFoundException("sentry_options_set_app_hang_thread_stackwalk_mode")));

        Assert.That(logger.Logs.Any(log => log.logLevel == SentryLevel.Warning
                                           && log.message.Contains("AttachAllThreadsToAppHangs")), Is.True);
    }
}
