using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using Sentry.Unity.Json;
using UnityEngine;

namespace Sentry.Unity.Tests
{
    public sealed class SentryUnityOptionsTests
    {
        private const string TestSentryOptionsFileName = "TestSentryOptions.json";

        [Test]
        public void Options_ReadFromJson_Success()
        {
            var optionsFilePath = GetTestOptionsFilePath();
            Assert.IsTrue(File.Exists(optionsFilePath));

            var jsonTextAsset = new TextAsset(File.ReadAllText(GetTestOptionsFilePath()));

            JsonSentryUnityOptions.LoadFromJson(jsonTextAsset);
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public void ToSentryUnityOptions_ValueMapping_AreEqual(bool isBuilding)
        {
            var expectedOptions = new SentryUnityOptions
            {
                Enabled = false,
                Dsn = "test",
                CaptureInEditor = false,
                EnableLogDebouncing = true,
                TracesSampleRate = 1.0f,
                AutoSessionTracking = false,
                AutoSessionTrackingInterval = TimeSpan.FromSeconds(1),
                AttachStacktrace = true,
                MaxBreadcrumbs = 1,
                ReportAssembliesMode = ReportAssembliesMode.None,
                SendDefaultPii = true,
                IsEnvironmentUser = true,
                MaxCacheItems = 1,
                InitCacheFlushTimeout = TimeSpan.FromSeconds(1),
                SampleRate = 0.5f,
                ShutdownTimeout = TimeSpan.FromSeconds(1),
                MaxQueueItems = 1,
                Release = "testRelease",
                Environment = "testEnvironment",
                Debug = true,
                DebugOnlyInEditor = true,
                DiagnosticLevel = SentryLevel.Info,
            };

            var scriptableOptions = ScriptableObject.CreateInstance<ScriptableSentryUnityOptions>();
            scriptableOptions.Enabled = expectedOptions.Enabled;
            scriptableOptions.Dsn = expectedOptions.Dsn;
            scriptableOptions.CaptureInEditor = expectedOptions.CaptureInEditor;
            scriptableOptions.EnableLogDebouncing = expectedOptions.EnableLogDebouncing;
            scriptableOptions.TracesSampleRate = expectedOptions.TracesSampleRate;
            scriptableOptions.AutoSessionTracking = expectedOptions.AutoSessionTracking;
            scriptableOptions.AutoSessionTrackingInterval = (int)expectedOptions.AutoSessionTrackingInterval.TotalMilliseconds;
            scriptableOptions.AttachStacktrace = expectedOptions.AttachStacktrace;
            scriptableOptions.MaxBreadcrumbs = expectedOptions.MaxBreadcrumbs;
            scriptableOptions.ReportAssembliesMode = expectedOptions.ReportAssembliesMode;
            scriptableOptions.SendDefaultPii = expectedOptions.SendDefaultPii;
            scriptableOptions.IsEnvironmentUser = expectedOptions.IsEnvironmentUser;
            scriptableOptions.MaxCacheItems = expectedOptions.MaxCacheItems;
            scriptableOptions.InitCacheFlushTimeout = (int)expectedOptions.InitCacheFlushTimeout.TotalMilliseconds;
            scriptableOptions.SampleRate = expectedOptions.SampleRate;
            scriptableOptions.ShutdownTimeout = (int)expectedOptions.ShutdownTimeout.TotalMilliseconds;
            scriptableOptions.MaxQueueItems = expectedOptions.MaxQueueItems;
            scriptableOptions.ReleaseOverride = expectedOptions.Release;
            scriptableOptions.EnvironmentOverride = expectedOptions.Environment;
            scriptableOptions.Debug = expectedOptions.Debug;
            scriptableOptions.DebugOnlyInEditor = expectedOptions.DebugOnlyInEditor;
            scriptableOptions.DiagnosticLevel = expectedOptions.DiagnosticLevel;

            var optionsActual = ScriptableSentryUnityOptions.ToSentryUnityOptions(scriptableOptions, isBuilding);

            AssertOptions(expectedOptions, optionsActual);
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public void ToScriptableOptions_ConvertJsonOptions_AreEqual(bool isBuilding)
        {
            var jsonTextAsset = new TextAsset(File.ReadAllText(GetTestOptionsFilePath()));
            var expectedOptions = JsonSentryUnityOptions.LoadFromJson(jsonTextAsset);

            var scriptableOptions = ScriptableObject.CreateInstance<ScriptableSentryUnityOptions>();
            SentryOptionsUtility.SetDefaults(scriptableOptions);
            JsonSentryUnityOptions.ToScriptableOptions(jsonTextAsset, scriptableOptions);

            var actualOptions = ScriptableSentryUnityOptions.ToSentryUnityOptions(scriptableOptions, isBuilding);

            AssertOptions(expectedOptions, actualOptions);
        }

        [Test]
        public void Ctor_Release_IsNull() => Assert.IsNull(new SentryUnityOptions().Release);

        [Test]
        public void Ctor_Environment_IsNull() => Assert.IsNull(new SentryUnityOptions().Environment);

        [Test]
        public void Ctor_CacheDirectoryPath_IsNull() => Assert.IsNull(new SentryUnityOptions().CacheDirectoryPath);

        public static void AssertOptions(SentryUnityOptions expected, SentryUnityOptions actual)
        {
            Assert.AreEqual(expected.Enabled, actual.Enabled);
            Assert.AreEqual(expected.Dsn, actual.Dsn);
            Assert.AreEqual(expected.CaptureInEditor, actual.CaptureInEditor);
            Assert.AreEqual(expected.EnableLogDebouncing, actual.EnableLogDebouncing);
            Assert.AreEqual(expected.TracesSampleRate, actual.TracesSampleRate);
            Assert.AreEqual(expected.AutoSessionTracking, actual.AutoSessionTracking);
            Assert.AreEqual(expected.AutoSessionTrackingInterval, actual.AutoSessionTrackingInterval);
            Assert.AreEqual(expected.AttachStacktrace, actual.AttachStacktrace);
            Assert.AreEqual(expected.MaxBreadcrumbs, actual.MaxBreadcrumbs);
            Assert.AreEqual(expected.ReportAssembliesMode, actual.ReportAssembliesMode);
            Assert.AreEqual(expected.SendDefaultPii, actual.SendDefaultPii);
            Assert.AreEqual(expected.IsEnvironmentUser, actual.IsEnvironmentUser);
            Assert.AreEqual(expected.MaxCacheItems, actual.MaxCacheItems);
            Assert.AreEqual(expected.InitCacheFlushTimeout, actual.InitCacheFlushTimeout);
            Assert.AreEqual(expected.SampleRate, actual.SampleRate);
            Assert.AreEqual(expected.ShutdownTimeout, actual.ShutdownTimeout);
            Assert.AreEqual(expected.MaxQueueItems, actual.MaxQueueItems);
            Assert.AreEqual(expected.Release, actual.Release);
            Assert.AreEqual(expected.Environment, actual.Environment);
            Assert.AreEqual(expected.CacheDirectoryPath, actual.CacheDirectoryPath);
            Assert.AreEqual(expected.Debug, actual.Debug);
            Assert.AreEqual(expected.DebugOnlyInEditor, actual.DebugOnlyInEditor);
            Assert.AreEqual(expected.DiagnosticLevel, actual.DiagnosticLevel);
        }

        private static string GetTestOptionsFilePath()
        {
            var assemblyFolderPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Assert.NotNull(assemblyFolderPath);
            return Path.Combine(assemblyFolderPath!, TestSentryOptionsFileName);
        }
    }
}
