using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Sentry.Unity.Editor.Tests
{
    public sealed class EditorModeTests
    {
        [Test]
        public void ValidateDsn_WrongFormat_CreatesError()
        {
            LogAssert.ignoreFailingMessages = true; // mandatory

            // arrange
            var validationErrors = new List<ValidationError>();

            // act

            // Do the 'act' phase inside 'using', not outside. There is no window 'outside'.
            using (var window = SentryTestWindow.Open())
            {
                window.OnValidationError += error => validationErrors.Add(error);
                window.Options.Dsn = "qwerty";
            }

            // assert
            Assert.GreaterOrEqual(validationErrors.Count, 1, "Expected at least 1 error");
            Assert.NotNull(validationErrors.FirstOrDefault(e => e.PropertyName.Contains(nameof(SentryTestWindow.Options.Dsn))));
        }

        [Test]
        public void ValidateDsn_Empty_CreatesNoError()
        {
            LogAssert.ignoreFailingMessages = true; // mandatory

            // arrange
            var validationErrors = new List<ValidationError>();

            // act

            // Do the 'act' phase inside 'using', not outside. There is no window 'outside'.
            using (var window = SentryTestWindow.Open())
            {
                window.OnValidationError += error => validationErrors.Add(error);
                window.Options.Dsn = "";
            }

            // assert
            Assert.AreEqual(0, validationErrors.Count);
        }

        [Test]
        public void ValidateDsn_CorrectFormat_CreatesNoError()
        {
            LogAssert.ignoreFailingMessages = true; // mandatory

            // arrange
            var validationErrors = new List<ValidationError>();

            // act

            // Do the 'act' phase inside 'using', not outside. There is no window 'outside'.
            using (var window = SentryTestWindow.Open())
            {
                window.OnValidationError += error => validationErrors.Add(error);

                Uri.TryCreate("https://sentryTest.io", UriKind.Absolute, out Uri testUri);
                window.Options.Dsn = testUri.ToString();
            }

            // assert
            Assert.AreEqual(0, validationErrors.Count);
        }
    }
}
