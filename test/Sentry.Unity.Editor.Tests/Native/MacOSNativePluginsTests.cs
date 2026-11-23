using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Sentry.Unity.Editor.Native;
using Sentry.Unity.Tests.SharedClasses;

namespace Sentry.Unity.Editor.Tests.Native;

public class MacOSNativePluginsTests
{
    private const string Root = "Packages/io.sentry.unity/Plugins/macOS/";
    private const string CocoaLibrary = Root + "Sentry/Sentry.dylib";
    private const string NativeLibrary = Root + "SentryNative/libsentry-native.dylib";
    private const string CrashBundle = Root + "SentryNative/SentryCrash.bundle";

    private static SentryUnityOptions Options(MacosBackend backend, bool nativeSupport = true, string? dsn = "https://key@sentry.io/1")
    {
        var options = new SentryUnityOptions { Dsn = dsn, MacosNativeSupportEnabled = nativeSupport };
        options.Experimental.MacosBackend = backend;
        return options;
    }

    [TestCase(MacosBackend.Cocoa, CocoaLibrary, true)]
    [TestCase(MacosBackend.Cocoa, NativeLibrary, false)]
    [TestCase(MacosBackend.Cocoa, CrashBundle, false)]
    [TestCase(MacosBackend.Native, CocoaLibrary, false)]
    [TestCase(MacosBackend.Native, NativeLibrary, true)]
    [TestCase(MacosBackend.Native, CrashBundle, true)]
    public void ShouldIncludeInBuild_IncludesOnlyTheSelectedBackend(MacosBackend backend, string assetPath, bool expected)
    {
        Assert.AreEqual(expected, MacOSNativePlugins.ShouldIncludeInBuild(assetPath, Options(backend)));
    }

    [TestCase(CocoaLibrary)]
    [TestCase(NativeLibrary)]
    [TestCase(CrashBundle)]
    public void ShouldIncludeInBuild_NativeSupportOff_IncludesNone(string assetPath)
    {
        Assert.IsFalse(MacOSNativePlugins.ShouldIncludeInBuild(assetPath, Options(MacosBackend.Cocoa, nativeSupport: false)));
        Assert.IsFalse(MacOSNativePlugins.ShouldIncludeInBuild(assetPath, Options(MacosBackend.Native, nativeSupport: false)));
    }

    [TestCase(CocoaLibrary)]
    [TestCase(NativeLibrary)]
    [TestCase(CrashBundle)]
    public void ShouldIncludeInBuild_NoOptions_IncludesNone(string assetPath)
    {
        Assert.IsFalse(MacOSNativePlugins.ShouldIncludeInBuild(assetPath, null));
    }

    [TestCase(CocoaLibrary)]
    [TestCase(NativeLibrary)]
    [TestCase(CrashBundle)]
    public void ShouldIncludeInBuild_InvalidOptions_IncludesNone(string assetPath)
    {
        Assert.IsFalse(MacOSNativePlugins.ShouldIncludeInBuild(assetPath, Options(MacosBackend.Cocoa, dsn: null)));
        Assert.IsFalse(MacOSNativePlugins.ShouldIncludeInBuild(assetPath, Options(MacosBackend.Native, dsn: null)));
    }

    [TestCase("Packages/io.sentry.unity/Plugins/macOS/SentryNativeBridge.m")]
    [TestCase("Assets/Plugins/macOS/Sentry.dylib")]
    [TestCase("Assets/Plugins/libsentry-native.dylib")]
    public void ShouldIncludeInBuild_UnrelatedPath_Includes(string assetPath)
    {
        Assert.IsTrue(MacOSNativePlugins.ShouldIncludeInBuild(assetPath, null));
    }

    [Test]
    public void ShouldIncludeInBuild_DevPackagePath_MatchesBackend()
    {
        var assetPath = "Packages/io.sentry.unity.dev/Plugins/macOS/SentryNative/SentryCrash.bundle";

        Assert.IsTrue(MacOSNativePlugins.ShouldIncludeInBuild(assetPath, Options(MacosBackend.Native)));
        Assert.IsFalse(MacOSNativePlugins.ShouldIncludeInBuild(assetPath, Options(MacosBackend.Cocoa)));
    }

    [Test]
    public void RequiredFiles_Cocoa_IsTheCocoaLibrary()
    {
        CollectionAssert.AreEqual(new[] { "Sentry/Sentry.dylib" }, MacOSNativePlugins.RequiredFiles(MacosBackend.Cocoa));
    }

    [Test]
    public void RequiredFiles_Native_AreTheLibraryAndTheCrashHandler()
    {
        CollectionAssert.AreEqual(
            new[] { "SentryNative/libsentry-native.dylib", "SentryNative/SentryCrash.bundle/Contents/MacOS/sentry-crash" },
            MacOSNativePlugins.RequiredFiles(MacosBackend.Native));
    }

    [Test]
    public void RequiredFiles_NativeSupportOff_IsEmpty()
    {
        CollectionAssert.IsEmpty(MacOSNativePlugins.RequiredFiles(MacOSNativePlugins.SelectedBackend(Options(MacosBackend.Native, nativeSupport: false))));
    }

    [TestCase(MacosBackend.Cocoa, "BuildCocoaSDK")]
    [TestCase(MacosBackend.Native, "BuildMacOSNativeSDK")]
    public void MissingFileMessage_DevPackage_NamesTheBuildTargets(MacosBackend backend, string target)
    {
        var message = MacOSNativePlugins.MissingFileMessage(backend, "/pkg/file", isDevPackage: true);

        StringAssert.Contains("/pkg/file", message);
        StringAssert.Contains($"dotnet msbuild /t:{target} src/Sentry.Unity", message);
        StringAssert.Contains("dotnet msbuild /t:DownloadNativeSDKs src/Sentry.Unity", message);
    }

    [Test]
    public void MissingFileMessage_Package_SaysToReinstall()
    {
        var message = MacOSNativePlugins.MissingFileMessage(MacosBackend.Cocoa, "/pkg/file", isDevPackage: false);

        StringAssert.Contains("/pkg/file", message);
        StringAssert.Contains("Reinstall the Sentry package", message);
        StringAssert.DoesNotContain("dotnet", message);
    }

    private static string[] StaleNames(SentryUnityOptions? options) =>
        MacOSNativePlugins.StaleArtifacts("/build/game.app", options)
            .Select(path => path.Replace('\\', '/'))
            .ToArray();

    [Test]
    public void StaleArtifacts_Cocoa_RemovesNativeAndLegacyFiles()
    {
        CollectionAssert.AreEquivalent(new[]
        {
            "/build/game.app/Contents/PlugIns/libsentry-native.dylib",
            "/build/game.app/Contents/PlugIns/SentryCrash.bundle",
            "/build/game.app/Contents/PlugIns/libsentry.dylib",
            "/build/game.app/Contents/MacOS/sentry-crash",
        }, StaleNames(Options(MacosBackend.Cocoa)));
    }

    [Test]
    public void StaleArtifacts_Native_RemovesCocoaAndLegacyFiles()
    {
        CollectionAssert.AreEquivalent(new[]
        {
            "/build/game.app/Contents/PlugIns/Sentry.dylib",
            "/build/game.app/Contents/PlugIns/libsentry.dylib",
            "/build/game.app/Contents/MacOS/sentry-crash",
        }, StaleNames(Options(MacosBackend.Native)));
    }

    [Test]
    public void StaleArtifacts_NativeSupportOff_RemovesAll()
    {
        CollectionAssert.AreEquivalent(new[]
        {
            "/build/game.app/Contents/PlugIns/Sentry.dylib",
            "/build/game.app/Contents/PlugIns/libsentry-native.dylib",
            "/build/game.app/Contents/PlugIns/SentryCrash.bundle",
            "/build/game.app/Contents/PlugIns/libsentry.dylib",
            "/build/game.app/Contents/MacOS/sentry-crash",
        }, StaleNames(Options(MacosBackend.Native, nativeSupport: false)));
    }

    [Test]
    public void RemoveStaleArtifacts_DeletesFilesAndBundles_KeepsTheSelectedBackend()
    {
        var app = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "game.app");
        var plugIns = Path.Combine(app, "Contents", "PlugIns");
        var bundleExecutable = Path.Combine(plugIns, "SentryCrash.bundle", "Contents", "MacOS", "sentry-crash");
        var legacyHandler = Path.Combine(app, "Contents", "MacOS", "sentry-crash");
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(bundleExecutable)!);
            Directory.CreateDirectory(Path.GetDirectoryName(legacyHandler)!);
            File.WriteAllText(bundleExecutable, "");
            File.WriteAllText(legacyHandler, "");
            File.WriteAllText(Path.Combine(plugIns, "libsentry-native.dylib"), "");
            File.WriteAllText(Path.Combine(plugIns, "Sentry.dylib"), "");

            MacOSNativePlugins.RemoveStaleArtifacts(new TestLogger(), app, Options(MacosBackend.Cocoa));

            Assert.IsTrue(File.Exists(Path.Combine(plugIns, "Sentry.dylib")));
            Assert.IsFalse(File.Exists(Path.Combine(plugIns, "libsentry-native.dylib")));
            Assert.IsFalse(Directory.Exists(Path.Combine(plugIns, "SentryCrash.bundle")));
            Assert.IsFalse(File.Exists(legacyHandler));
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(app)!, recursive: true);
        }
    }

    [Test]
    public void BuiltCrashExecutables_FindsTheBundleInAnAppAndAnXcodeExport()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var inApp = Path.Combine(root, "game.app", "Contents", "PlugIns", "SentryCrash.bundle", "Contents", "MacOS", "sentry-crash");
        var inExport = Path.Combine(root, "export", "game", "PlugIns", "SentryCrash.bundle", "Contents", "MacOS", "sentry-crash");
        try
        {
            foreach (var file in new[] { inApp, inExport })
            {
                Directory.CreateDirectory(Path.GetDirectoryName(file)!);
                File.WriteAllText(file, "");
            }

            CollectionAssert.AreEquivalent(new[] { inApp }, MacOSNativePlugins.BuiltCrashExecutables(Path.Combine(root, "game.app")).ToArray());
            CollectionAssert.AreEquivalent(new[] { inExport }, MacOSNativePlugins.BuiltCrashExecutables(Path.Combine(root, "export")).ToArray());
            CollectionAssert.IsEmpty(MacOSNativePlugins.BuiltCrashExecutables(Path.Combine(root, "missing")).ToArray());
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
