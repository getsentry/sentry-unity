using System.IO;
using System.Text;
using System.Text.Json;
using NUnit.Framework;

namespace Sentry.Unity.Tests
{
    public sealed class UnitySentryOptionsTest
    {
        [Test]
        public void Options_WriteRead_Equals()
        {
            // arrange
            var optionsExpected = new UnitySentryOptions
            {
                Enabled = true,
                Dsn = "http://test.com",
                CaptureInEditor = true,
                Debug = true,
                DebugOnlyInEditor = false,
                DiagnosticsLevel = SentryLevel.Info,
                RequestBodyCompressionLevel = SentryUnityCompression.Optimal,
                AttachStacktrace = true,
                SampleRate = 1.15f,
                Release = "release",
                Environment = "test"
            };

            // act
            using var memory = new MemoryStream();
            using var writer = new Utf8JsonWriter(memory);
            optionsExpected.WriteTo(writer);

            var jsonRaw = Encoding.UTF8.GetString(memory.ToArray());
            using var jsonDocument = JsonDocument.Parse(jsonRaw);
            var optionsActual = UnitySentryOptions.FromJson(jsonDocument.RootElement);

            // assert
            Assert.AreEqual(optionsExpected.Enabled, optionsActual.Enabled);
            Assert.AreEqual(optionsExpected.Dsn, optionsActual.Dsn);
            Assert.AreEqual(optionsExpected.CaptureInEditor, optionsActual.CaptureInEditor);
            Assert.AreEqual(optionsExpected.Debug, optionsActual.Debug);
            Assert.AreEqual(optionsExpected.DebugOnlyInEditor, optionsActual.DebugOnlyInEditor);
            Assert.AreEqual(optionsExpected.DiagnosticsLevel, optionsActual.DiagnosticsLevel);
            Assert.AreEqual(optionsExpected.RequestBodyCompressionLevel, optionsActual.RequestBodyCompressionLevel);
            Assert.AreEqual(optionsExpected.AttachStacktrace, optionsActual.AttachStacktrace);
            Assert.AreEqual(optionsExpected.SampleRate, optionsActual.SampleRate);
            Assert.AreEqual(optionsExpected.Release, optionsActual.Release);
            Assert.AreEqual(optionsExpected.Environment, optionsActual.Environment);
        }
    }
}
