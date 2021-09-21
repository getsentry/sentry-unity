using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using Sentry.Unity.Editor.Android;

namespace Sentry.Unity.Editor.Tests
{
    public class AndroidManifestTests
    {
        private class Fixture
        {
            public SentryUnityOptions? SentryUnityOptions { get; set; }
            public Func<SentryUnityOptions?> GetOptions { get; set; }
            public TestUnityLoggerInterceptor LoggerInterceptor { get; set; }

            public Fixture()
            {
                LoggerInterceptor = new();
                // Options configured to initialize the Android SDK, tests will change from there:
                SentryUnityOptions = new()
                {
                    Enabled = true,
                    Dsn = "https://k@h/p",
                    AndroidNativeSupportEnabled = true,
                    Debug = true
                };
                SentryUnityOptions.DiagnosticLogger = new UnityLogger(SentryUnityOptions, LoggerInterceptor);

                GetOptions = () => SentryUnityOptions;
            }

            public AndroidManifestConfiguration GetSut() => new(GetOptions, LoggerInterceptor);
        }

        [SetUp]
        public void SetUp() => _fixture = new Fixture();
        private Fixture _fixture = null!;

        [Test]
        public void OnPostGenerateGradleAndroidProject_BrokenPath_ThrowsFileNotFound()
        {
            var sut = _fixture.GetSut();
            const string brokenBasePath = "broken-path";
            var ex = Assert.Throws<FileNotFoundException>(() => sut.OnPostGenerateGradleAndroidProject(brokenBasePath));

            Assert.AreEqual(
                Path.Combine(brokenBasePath, "src", "main", "AndroidManifest.xml"),
                ex.FileName);

            Assert.AreEqual(
                "Can't configure native Android SDK nor set auto-init:false.",
                ex.Message);
        }

        [Test]
        public void OnPostGenerateGradleAndroidProject_LoadSentryUnityOptions_NullOptions_LogWarningAndDisableInit()
        {
            _fixture.SentryUnityOptions = null;
            var sut = _fixture.GetSut();
            var manifest = WithAndroidManifest(basePath => sut.OnPostGenerateGradleAndroidProject(basePath));

            AssertLogContains(SentryLevel.Warning, "Couldn't load SentryOptions. Can't configure native Android SDK.");

            Assert.True(manifest.Contains(
                    "<meta-data android:name=\"io.sentry.auto-init\" android:value=\"false\" />"),
                "Expected 'auto-init' to be disabled");
        }

        [Test]
        public void OnPostGenerateGradleAndroidProject_UnityOptions_EnabledFalse_LogDebugAndDisableInit()
        {
            _fixture.SentryUnityOptions!.Enabled = false;
            var sut = _fixture.GetSut();
            var manifest = WithAndroidManifest(basePath => sut.OnPostGenerateGradleAndroidProject(basePath));

            AssertLogContains(SentryLevel.Debug, "Sentry SDK has been disabled.\nYou can disable this log by raising the debug verbosity level above 'Debug'.");

            Assert.True(manifest.Contains(
                    "<meta-data android:name=\"io.sentry.auto-init\" android:value=\"false\" />"),
                "Expected 'auto-init' to be disabled");
        }

        [Test]
        [TestCase(null)]
        [TestCase("")]
        [TestCase("  ")]
        public void OnPostGenerateGradleAndroidProject_UnityOptions_EnabledWithoutDsn_LogWarningAndDisableInit(string? dsn)
        {
            _fixture.SentryUnityOptions!.Dsn = dsn;
            var sut = _fixture.GetSut();
            var manifest = WithAndroidManifest(basePath => sut.OnPostGenerateGradleAndroidProject(basePath));

            AssertLogContains(SentryLevel.Warning, "No Sentry DSN configured. Sentry will be disabled.");

            Assert.True(manifest.Contains(
                    "<meta-data android:name=\"io.sentry.auto-init\" android:value=\"false\" />"),
                "Expected 'auto-init' to be disabled");
        }

        [Test]
        public void OnPostGenerateGradleAndroidProject_UnityOptions_AndroidNativeSupportEnabledFalse_LogDebugAndDisableInit()
        {
            _fixture.SentryUnityOptions!.AndroidNativeSupportEnabled = false;
            var sut = _fixture.GetSut();
            var manifest = WithAndroidManifest(basePath => sut.OnPostGenerateGradleAndroidProject(basePath));

            AssertLogContains(SentryLevel.Debug, $"Android Native support disabled via options.");

            Assert.True(manifest.Contains(
                    "<meta-data android:name=\"io.sentry.auto-init\" android:value=\"false\" />"),
                "Expected 'auto-init' to be disabled");
        }

        [Test]
        public void OnPostGenerateGradleAndroidProject_ManifestHasDsn()
        {
            var expected = _fixture.SentryUnityOptions!.Dsn;
            var sut = _fixture.GetSut();
            var manifest = WithAndroidManifest(basePath => sut.OnPostGenerateGradleAndroidProject(basePath));

            AssertLogContains(SentryLevel.Debug, $"Setting DSN: {expected}");

            Assert.True(manifest.Contains(
                    $"<meta-data android:name=\"io.sentry.dsn\" android:value=\"{expected}\" />"),
                $"Expected 'DSN' in Manifest:\n{manifest}");
        }

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void OnPostGenerateGradleAndroidProject_DebugOnlyInEditor_ControlsDebugMode(bool debugOnlyInEditor)
        {
            _fixture.SentryUnityOptions!.DebugOnlyInEditor = debugOnlyInEditor;
            // Debug Only In Editor means: Don't set debug=true in any player build
            var expectDebugFlag = !debugOnlyInEditor;

            var sut = _fixture.GetSut();
            var manifest = WithAndroidManifest(basePath => sut.OnPostGenerateGradleAndroidProject(basePath));

            Assert.AreEqual(expectDebugFlag, manifest.Contains(
                    "<meta-data android:name=\"io.sentry.debug\""),
                $"'debug' in Manifest:\n{manifest}");
        }

        [Test]
        public void OnPostGenerateGradleAndroidProject_ReleaseIsNull_ReleaseNotSet()
        {
            _fixture.SentryUnityOptions!.Release = null;
            var sut = _fixture.GetSut();
            var manifest = WithAndroidManifest(basePath => sut.OnPostGenerateGradleAndroidProject(basePath));

            Assert.False(manifest.Contains(
                    "<meta-data android:name=\"io.sentry.release\""),
                $"Expected sentry 'release' not set in Manifest:\n{manifest}");
        }

        [Test]
        public void OnPostGenerateGradleAndroidProject_ReleaseIsNotNull_SetRelease()
        {
            const string? expected = "expected release";
            _fixture.SentryUnityOptions!.Release = expected;
            var sut = _fixture.GetSut();
            var manifest = WithAndroidManifest(basePath => sut.OnPostGenerateGradleAndroidProject(basePath));

            AssertLogContains(SentryLevel.Debug, $"Setting Release: {expected}");

            Assert.True(manifest.Contains(
                    $"<meta-data android:name=\"io.sentry.release\" android:value=\"{expected}\" />"),
                $"Expected 'release' in Manifest:\n{manifest}");
        }

        [Test]
        public void OnPostGenerateGradleAndroidProject_EnvironmentIsNull_EnvironmentNotSet()
        {
            _fixture.SentryUnityOptions!.Environment = null;
            var sut = _fixture.GetSut();
            var manifest = WithAndroidManifest(basePath => sut.OnPostGenerateGradleAndroidProject(basePath));

            Assert.False(manifest.Contains(
                    "<meta-data android:name=\"io.sentry.environment\""),
                $"Expected sentry 'environment' not set in Manifest:\n{manifest}");
        }

        [Test]
        public void OnPostGenerateGradleAndroidProject_EnvironmentIsNotNull_SetEnvironment()
        {
            const string? expected = "expected env";
            _fixture.SentryUnityOptions!.Environment = expected;
            var sut = _fixture.GetSut();
            var manifest = WithAndroidManifest(basePath => sut.OnPostGenerateGradleAndroidProject(basePath));

            AssertLogContains(SentryLevel.Debug, $"Setting Environment: {expected}");

            Assert.True(manifest.Contains(
                    $"<meta-data android:name=\"io.sentry.environment\" android:value=\"{expected}\" />"),
                $"Expected 'environment' in Manifest:\n{manifest}");
        }

        // options.setDiagnosticLevel(SentryLevel.valueOf(level.toUpperCase(Locale.ROOT)));
        // src/sentry-java/sentry-android-core/src/main/java/io/sentry/android/core/ManifestMetadataReader.java
        // src/sentry-java/sentry/src/main/java/io/sentry/SentryLevel.java
        private static readonly SentryJavaLevel[] SentryJavaLevels =
        {
            new () { SentryLevel = SentryLevel.Debug, JavaLevel = "debug" },
            new () { SentryLevel = SentryLevel.Error, JavaLevel = "error" },
            new () { SentryLevel = SentryLevel.Fatal, JavaLevel = "fatal" },
            new () { SentryLevel = SentryLevel.Info, JavaLevel = "info" },
            new () { SentryLevel = SentryLevel.Warning, JavaLevel = "warning" }
        };

        [Test]
        public void OnPostGenerateGradleAndroidProject_DiagnosticLevel_TestCases(
            [ValueSource(nameof(SentryJavaLevels))] SentryJavaLevel levels)
        {
            _fixture.SentryUnityOptions!.DiagnosticLevel = levels.SentryLevel;
            var sut = _fixture.GetSut();
            var manifest = WithAndroidManifest(basePath => sut.OnPostGenerateGradleAndroidProject(basePath));

            // Debug message is only logged if level is Debug:
            if (levels.SentryLevel == SentryLevel.Debug)
            {
                AssertLogContains(SentryLevel.Debug, $"Setting DiagnosticLevel: {levels.SentryLevel}");
            }

            Assert.True(manifest.Contains(
                    $"<meta-data android:name=\"io.sentry.debug.level\" android:value=\"{levels.JavaLevel}\" />"),
                $"Expected 'io.sentry.debug.level' in Manifest:\n{manifest}");
        }

        [Test]
        public void OnPostGenerateGradleAndroidProject_SampleRate_SetIfNotNull()
        {
            const float expected = 0.6f;
            _fixture.SentryUnityOptions!.SampleRate = expected;
            var sut = _fixture.GetSut();
            var manifest = WithAndroidManifest(basePath => sut.OnPostGenerateGradleAndroidProject(basePath));

            AssertLogContains(SentryLevel.Debug, $"Setting SampleRate: {expected}");

            Assert.True(manifest.Contains(
                    $"<meta-data android:name=\"io.sentry.sample-rate\" android:value=\"{expected}\" />"),
                $"Expected 'io.sentry.sample-rate' in Manifest:\n{manifest}");
        }

        private void AssertLogContains(SentryLevel sentryLevel, string message)
            => Assert.Contains((sentryLevel, $"Sentry: ({sentryLevel.ToString()}) {message} "),
                _fixture.LoggerInterceptor.Messages);

        private string WithAndroidManifest(Action<string> callback)
        {
            var basePath = GetFakeManifestFileBasePath();
            var androidManifest = AndroidManifestConfiguration.GetManifestPath(basePath);
            try
            {
                callback(basePath);
                return File.ReadAllText(androidManifest);
            }
            finally
            {
                File.Delete(androidManifest);
            }
        }

        private string GetFakeManifestFileBasePath()
        {
            var assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var newAndroidManifest = Path.Combine(assemblyPath, "TestFiles", "Android", "Empty-AndroidManifest.xml");

            var basePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var destination = Path.Combine(basePath, "src", "main");
            Directory.CreateDirectory(destination);
            File.Copy(newAndroidManifest, Path.Combine(destination, "AndroidManifest.xml"), false);
            return basePath;
        }

        public struct SentryJavaLevel
        {
            public SentryLevel SentryLevel;
            public string JavaLevel;
        }
    }
}
