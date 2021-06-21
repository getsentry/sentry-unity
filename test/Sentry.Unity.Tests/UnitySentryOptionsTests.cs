using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;
using NUnit.Framework;
using Sentry.Unity.Json;
using UnityEditor;
using UnityEngine;

namespace Sentry.Unity.Tests
{
    public sealed class UnitySentryOptionsTests
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
        public void ToSentryUnityOptions_MapsOfOptions()
        {
            var optionsExpected = new SentryUnityOptions
            {
                Enabled = true,
                Dsn = "https://test.com",
                CaptureInEditor = true,
                Debug = true,
                DebugOnlyInEditor = true,
                DiagnosticLevel = SentryLevel.Info,
                AttachStacktrace = true,
                SampleRate = 1f,
            };

            var scriptableSentryUnity = ScriptableObject.CreateInstance<ScriptableSentryUnityOptions>();
            scriptableSentryUnity.Enabled = optionsExpected.Enabled;
            scriptableSentryUnity.Dsn = optionsExpected.Dsn;
            scriptableSentryUnity.CaptureInEditor = optionsExpected.CaptureInEditor;
            scriptableSentryUnity.Debug = optionsExpected.Debug;
            scriptableSentryUnity.DebugOnlyInEditor = optionsExpected.DebugOnlyInEditor;
            scriptableSentryUnity.DiagnosticLevel = optionsExpected.DiagnosticLevel;
            scriptableSentryUnity.AttachStacktrace = optionsExpected.AttachStacktrace;
            scriptableSentryUnity.SampleRate = (float)optionsExpected.SampleRate;

            var optionsActual = ScriptableSentryUnityOptions.ToSentryUnityOptions(scriptableSentryUnity);

            AssertOptions(optionsExpected, optionsActual);
        }

        [Test]
        public void Options_JsonAndScriptable_Equal()
        {
            var jsonTextAsset = new TextAsset(File.ReadAllText(GetTestOptionsFilePath()));
            var scriptableOptions = ScriptableObject.CreateInstance<ScriptableSentryUnityOptions>();

            var expectedOptions = JsonSentryUnityOptions.LoadFromJson(jsonTextAsset);

            JsonSentryUnityOptions.ToScriptableOptions(jsonTextAsset, scriptableOptions);
            var actualOptions = ScriptableSentryUnityOptions.ToSentryUnityOptions(scriptableOptions);

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
            Assert.AreEqual(expected.Debug, actual.Debug);
            Assert.AreEqual(expected.DebugOnlyInEditor, actual.DebugOnlyInEditor);
            Assert.AreEqual(expected.DiagnosticLevel, actual.DiagnosticLevel);
            Assert.AreEqual(expected.AttachStacktrace, actual.AttachStacktrace);

            // SampleRate cannot be null in the editor-window so it defaults to 1.0f.
            var expectedSampleRate = expected.SampleRate == 1.0f ? null : expected.SampleRate;
            var actualSampleRate = actual.SampleRate == 1.0f ? null : actual.SampleRate;
            Assert.AreEqual(expectedSampleRate, actualSampleRate);
        }

        private static string GetTestOptionsFilePath()
        {
            var assemblyFolderPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Assert.NotNull(assemblyFolderPath);
            return Path.Combine(assemblyFolderPath!, TestSentryOptionsFileName);
        }
    }
}
