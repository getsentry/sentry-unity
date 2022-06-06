using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using Sentry.Unity.Editor.Android;
using UnityEditor;
using UnityEngine;

namespace Sentry.Unity.Editor.Tests.Android
{
    public class AndroidManifestTests
    {
        private class Fixture
        {
            public SentryUnityOptions? SentryUnityOptions { get; set; }
            public Func<SentryUnityOptions?> GetSentryUnityOptions { get; set; }
            public SentryCliOptions? SentryCliOptions { get; set; }
            public Func<SentryCliOptions?> GetSentryCliOptions { get; set; }
            public TestUnityLoggerInterceptor LoggerInterceptor { get; set; }
            public bool IsDevelopmentBuild { get; set; }
            public ScriptingImplementation ScriptingImplementation { get; set; } = ScriptingImplementation.IL2CPP;

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

                SentryCliOptions = ScriptableObject.CreateInstance<SentryCliOptions>();
                SentryCliOptions.Auth = "test_auth_token";
                SentryCliOptions.Organization = "test_organization";
                SentryCliOptions.Project = "test_project";

                GetSentryUnityOptions = () => SentryUnityOptions;
                GetSentryCliOptions = () => SentryCliOptions;
            }

            public AndroidManifestConfiguration GetSut() =>
                new(GetSentryUnityOptions,
                    GetSentryCliOptions,
                    IsDevelopmentBuild,
                    ScriptingImplementation,
                    LoggerInterceptor);
        }

        [SetUp]
        public void SetUp() => _fixture = new Fixture();
        private Fixture _fixture = null!;

        [Test]
        public void ModifyManifest_BrokenPath_ThrowsFileNotFound()
        {
            var sut = _fixture.GetSut();
            const string brokenBasePath = "broken-path";
            var ex = Assert.Throws<FileNotFoundException>(() => sut.ModifyManifest(brokenBasePath));

            Assert.AreEqual(
                Path.Combine(brokenBasePath, "src", "main", "AndroidManifest.xml"),
                ex.FileName);

            Assert.AreEqual(
                "Can't configure native Android SDK nor set auto-init:false.",
                ex.Message);
        }

        [Test]
        public void ModifyManifest_LoadSentryUnityOptions_NullOptions_LogWarningAndDisableInit()
        {
            _fixture.SentryUnityOptions = null;
            var sut = _fixture.GetSut();
            var manifest = WithAndroidManifest(basePath => sut.ModifyManifest(basePath));

            _fixture.LoggerInterceptor.AssertLogContains(SentryLevel.Warning, "Android Native support disabled. " +
                                                                              "Sentry has not been configured. You can do that through the editor: Tools -> Sentry");

            Assert.True(manifest.Contains(
                    "<meta-data android:name=\"io.sentry.auto-init\" android:value=\"false\" />"),
                "Expected 'auto-init' to be disabled");
        }

        [Test]
        public void ModifyManifest_UnityOptions_EnabledFalse_LogDebugAndDisableInit()
        {
            _fixture.SentryUnityOptions!.Enabled = false;
            var sut = _fixture.GetSut();
            var manifest = WithAndroidManifest(basePath => sut.ModifyManifest(basePath));

            _fixture.LoggerInterceptor.AssertLogContains(SentryLevel.Debug, "Sentry SDK has been disabled.\nYou can disable this log by raising the debug verbosity level above 'Debug'.");

            Assert.True(manifest.Contains(
                    "<meta-data android:name=\"io.sentry.auto-init\" android:value=\"false\" />"),
                "Expected 'auto-init' to be disabled");
        }

        [Test]
        [TestCase(null)]
        [TestCase("")]
        [TestCase("  ")]
        public void ModifyManifest_UnityOptions_EnabledWithoutDsn_LogWarningAndDisableInit(string? dsn)
        {
            _fixture.SentryUnityOptions!.Dsn = dsn;
            var sut = _fixture.GetSut();
            var manifest = WithAndroidManifest(basePath => sut.ModifyManifest(basePath));

            _fixture.LoggerInterceptor.AssertLogContains(SentryLevel.Warning, "No Sentry DSN configured. Sentry will be disabled.");

            Assert.True(manifest.Contains(
                    "<meta-data android:name=\"io.sentry.auto-init\" android:value=\"false\" />"),
                "Expected 'auto-init' to be disabled");
        }

        [Test]
        public void ModifyManifest_UnityOptions_AndroidNativeSupportEnabledFalse_LogDebugAndDisableInit()
        {
            _fixture.SentryUnityOptions!.AndroidNativeSupportEnabled = false;
            var sut = _fixture.GetSut();
            var manifest = WithAndroidManifest(basePath => sut.ModifyManifest(basePath));

            _fixture.LoggerInterceptor.AssertLogContains(SentryLevel.Debug, "Android Native support disabled through the options.");

            Assert.True(manifest.Contains(
                    "<meta-data android:name=\"io.sentry.auto-init\" android:value=\"false\" />"),
                "Expected 'auto-init' to be disabled");
        }

        [Test]
        public void ModifyManifest_ManifestHasDsn()
        {
            var expected = _fixture.SentryUnityOptions!.Dsn;
            var sut = _fixture.GetSut();
            var manifest = WithAndroidManifest(basePath => sut.ModifyManifest(basePath));

            _fixture.LoggerInterceptor.AssertLogContains(SentryLevel.Debug, $"Setting DSN: {expected}");

            Assert.True(manifest.Contains(
                    $"<meta-data android:name=\"io.sentry.dsn\" android:value=\"{expected}\" />"),
                $"Expected 'DSN' in Manifest:\n{manifest}");
        }

        [Test]
        public void ModifyManifest_ReleaseIsNull_ReleaseNotSet()
        {
            _fixture.SentryUnityOptions!.Release = null;
            var sut = _fixture.GetSut();
            var manifest = WithAndroidManifest(basePath => sut.ModifyManifest(basePath));

            Assert.False(manifest.Contains(
                    "<meta-data android:name=\"io.sentry.release\""),
                $"Expected sentry 'release' not set in Manifest:\n{manifest}");
        }

        [Test]
        public void ModifyManifest_ReleaseIsNotNull_SetRelease()
        {
            const string? expected = "expected release";
            _fixture.SentryUnityOptions!.Release = expected;
            var sut = _fixture.GetSut();
            var manifest = WithAndroidManifest(basePath => sut.ModifyManifest(basePath));

            _fixture.LoggerInterceptor.AssertLogContains(SentryLevel.Debug, $"Setting Release: {expected}");

            Assert.True(manifest.Contains(
                    $"<meta-data android:name=\"io.sentry.release\" android:value=\"{expected}\" />"),
                $"Expected 'release' in Manifest:\n{manifest}");
        }

        [Test]
        public void ModifyManifest_EnvironmentIsNull_EnvironmentNotSet()
        {
            _fixture.SentryUnityOptions!.Environment = null;
            var sut = _fixture.GetSut();
            var manifest = WithAndroidManifest(basePath => sut.ModifyManifest(basePath));

            Assert.False(manifest.Contains(
                    "<meta-data android:name=\"io.sentry.environment\""),
                $"Expected sentry 'environment' not set in Manifest:\n{manifest}");
        }

        [Test]
        public void ModifyManifest_EnvironmentIsNotNull_SetEnvironment()
        {
            const string? expected = "expected env";
            _fixture.SentryUnityOptions!.Environment = expected;
            var sut = _fixture.GetSut();
            var manifest = WithAndroidManifest(basePath => sut.ModifyManifest(basePath));

            _fixture.LoggerInterceptor.AssertLogContains(SentryLevel.Debug, $"Setting Environment: {expected}");

            Assert.True(manifest.Contains(
                    $"<meta-data android:name=\"io.sentry.environment\" android:value=\"{expected}\" />"),
                $"Expected 'environment' in Manifest:\n{manifest}");
        }

        // options.setDiagnosticLevel(SentryLevel.valueOf(level.toUpperCase(Locale.ROOT)));
        // modules/sentry-java/sentry-android-core/src/main/java/io/sentry/android/core/ManifestMetadataReader.java
        // modules/sentry-java/sentry/src/main/java/io/sentry/SentryLevel.java
        private static readonly SentryJavaLevel[] SentryJavaLevels =
        {
            new () { SentryLevel = SentryLevel.Debug, JavaLevel = "debug" },
            new () { SentryLevel = SentryLevel.Error, JavaLevel = "error" },
            new () { SentryLevel = SentryLevel.Fatal, JavaLevel = "fatal" },
            new () { SentryLevel = SentryLevel.Info, JavaLevel = "info" },
            new () { SentryLevel = SentryLevel.Warning, JavaLevel = "warning" }
        };

        [Test]
        public void ModifyManifest_DiagnosticLevel_TestCases(
            [ValueSource(nameof(SentryJavaLevels))] SentryJavaLevel levels)
        {
            _fixture.SentryUnityOptions!.DiagnosticLevel = levels.SentryLevel;
            var sut = _fixture.GetSut();
            var manifest = WithAndroidManifest(basePath => sut.ModifyManifest(basePath));

            // Debug message is only logged if level is Debug:
            if (levels.SentryLevel == SentryLevel.Debug)
            {
                _fixture.LoggerInterceptor.AssertLogContains(SentryLevel.Debug, $"Setting DiagnosticLevel: {levels.SentryLevel}");
            }

            Assert.True(manifest.Contains(
                    $"<meta-data android:name=\"io.sentry.debug.level\" android:value=\"{levels.JavaLevel}\" />"),
                $"Expected 'io.sentry.debug.level' in Manifest:\n{manifest}");
        }

        [Test]
        public void ModifyManifest_SampleRate_SetIfNotNull()
        {
            const float expected = 0.6f;
            _fixture.SentryUnityOptions!.SampleRate = expected;
            var sut = _fixture.GetSut();
            var manifest = WithAndroidManifest(basePath => sut.ModifyManifest(basePath));

            _fixture.LoggerInterceptor.AssertLogContains(SentryLevel.Debug, $"Setting SampleRate: {expected}");

            Assert.True(manifest.Contains(
                    $"<meta-data android:name=\"io.sentry.sample-rate\" android:value=\"{expected}\" />"),
                $"Expected 'io.sentry.sample-rate' in Manifest:\n{manifest}");
        }

        [Test]
        public void ModifyManifest_RepeatedRunProducesSameResult()
        {
            var sut = _fixture.GetSut();
            var manifest1 = WithAndroidManifest(basePath => sut.ModifyManifest(basePath));
            var manifest2 = WithAndroidManifest((basePath) =>
            {
                sut.ModifyManifest(basePath);
                sut.ModifyManifest(basePath);
            });

            Debug.Log(manifest2);
            Assert.True(manifest1.Contains("sentry.dsn"));
            Assert.AreEqual(manifest1, manifest2);
        }

        [Test]
        public void ModifyManifest_RepeatedRunRemovesConfigs()
        {
            var fixture2 = new Fixture { SentryUnityOptions = null };
            var manifest1 = WithAndroidManifest(basePath => _fixture.GetSut().ModifyManifest(basePath));
            var manifest2 = WithAndroidManifest((basePath) =>
            {
                _fixture.GetSut().ModifyManifest(basePath);
                fixture2.GetSut().ModifyManifest(basePath);
            });

            Debug.Log($"Manifest 1 (before):\n{manifest1}");
            Debug.Log($"Manifest 2 (after):\n{manifest2}");
            Assert.True(manifest1.Contains("sentry.dsn"));
            Assert.False(manifest2.Contains("sentry.dsn"));
        }

        [Test]
        public void SetupSymbolsUpload_SentryCliOptionsNull_LogsWarningAndReturns()
        {
            var fakeProjectPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            DebugSymbolUploadTests.SetupFakeProject(fakeProjectPath);
            var gradleProjectPath = Path.Combine(fakeProjectPath, "GradleProject");

            _fixture.SentryCliOptions = null;
            var sut = _fixture.GetSut();

            sut.SetupSymbolsUpload("unity_project_path", gradleProjectPath);

            _fixture.LoggerInterceptor.AssertLogContains(SentryLevel.Warning, "Failed to load sentry-cli options.");

            Directory.Delete(Path.GetFullPath(fakeProjectPath), true);
        }

        [Test]
        public void SetupSymbolsUpload_SymbolsUploadDisabled_LogsAndReturns()
        {
            var fakeProjectPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            DebugSymbolUploadTests.SetupFakeProject(fakeProjectPath);
            var gradleProjectPath = Path.Combine(fakeProjectPath, "GradleProject");

            _fixture.SentryCliOptions!.UploadSymbols = false;
            var sut = _fixture.GetSut();

            sut.SetupSymbolsUpload("unity_project_path", gradleProjectPath);

            _fixture.LoggerInterceptor.AssertLogContains(SentryLevel.Debug, "Automated symbols upload has been disabled.");

            Directory.Delete(Path.GetFullPath(fakeProjectPath), true);
        }

        [Test]
        public void SetupSymbolsUpload_DevelopmentBuildDevUploadDisabled_LogsAndReturns()
        {
            var fakeProjectPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            DebugSymbolUploadTests.SetupFakeProject(fakeProjectPath);
            var gradleProjectPath = Path.Combine(fakeProjectPath, "GradleProject");

            _fixture.IsDevelopmentBuild = true;
            var sut = _fixture.GetSut();

            sut.SetupSymbolsUpload("unity_project_path", gradleProjectPath);

            _fixture.LoggerInterceptor.AssertLogContains(SentryLevel.Debug, "Automated symbols upload for development builds has been disabled.");

            Directory.Delete(Path.GetFullPath(fakeProjectPath), true);
        }

        [Test]
        public void SetupSymbolsUpload_SentryCliOptionsInvalid_LogsAndReturns()
        {
            var fakeProjectPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            DebugSymbolUploadTests.SetupFakeProject(fakeProjectPath);
            var gradleProjectPath = Path.Combine(fakeProjectPath, "GradleProject");

            _fixture.SentryCliOptions!.Auth = string.Empty;
            var sut = _fixture.GetSut();

            sut.SetupSymbolsUpload("unity_project_path", gradleProjectPath);

            _fixture.LoggerInterceptor.AssertLogContains(SentryLevel.Warning, "sentry-cli validation failed. Symbols will not be uploaded." +
                                                   "\nYou can disable this warning by disabling the automated symbols upload under " +
                                                   SentryCliOptions.EditorMenuPath);

            Directory.Delete(Path.GetFullPath(fakeProjectPath), true);
        }

        [Test]
        [TestCase(ScriptingImplementation.IL2CPP)]
        [TestCase(ScriptingImplementation.Mono2x)]
        public void SetupSymbolsUpload_ValidConfiguration_AppendsUploadTaskToGradleAndCreatesSentryProperties(ScriptingImplementation scriptingImplementation)
        {
            var fakeProjectPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            DebugSymbolUploadTests.SetupFakeProject(fakeProjectPath);

            _fixture.ScriptingImplementation = scriptingImplementation;
            var sut = _fixture.GetSut();
            var unityProjectPath = Path.Combine(fakeProjectPath, "UnityProject");
            var gradleProjectPath = Path.Combine(fakeProjectPath, "GradleProject");

            sut.SetupSymbolsUpload(unityProjectPath, gradleProjectPath);

            StringAssert.Contains("println 'Uploading symbols to Sentry'",
                File.ReadAllText(Path.Combine(gradleProjectPath, "build.gradle")));
            Assert.True(File.Exists(Path.Combine(gradleProjectPath, "sentry.properties")));

            Directory.Delete(Path.GetFullPath(fakeProjectPath), true);
        }

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
                Directory.Delete(basePath, true);
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
