using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Sentry.Unity.Editor.Tests
{
    public sealed class EditorModeTests
    {
        [UnitySetUp]
        public IEnumerator InitializeOptions()
        {
            // Due to an issue, Sentry doesn't always load UnitySentryOptions, which
            // results in tests not running on clean clone or on CI.
            // https://github.com/getsentry/sentry-unity/issues/77
            //
            // This hack sets the options manually if that happens.
            // Since this skips a layer of testing, this is not desirable long term
            // and we should find a proper way to solve this.
            if (!SentryInitialization.IsInit)
            {
                var options = new UnitySentryOptions
                {
                    Dsn = "https://94677106febe46b88b9b9ae5efd18a00@o447951.ingest.sentry.io/5439417",
                    Enabled = true
                };

                SentryInitialization.Init(options);

                Debug.LogWarning("Sentry has not been initialized prior to running tests. Using manual configuration.");
            }

            yield break;
        }

        [Test]
        public void OptionsDsnField_WrongFormat_CreatesError()
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
            Assert.AreEqual(1, validationErrors.Count);
            Assert.NotNull(validationErrors.SingleOrDefault(e => e.PropertyName.Contains(nameof(SentryTestWindow.Options.Dsn))));
        }

        // This test method has a side effect of creating 'link.xml' if file doesn't exits.
        [Test]
        public void SentryTestWindow_OpenAndLinkXmlCopied_Successful()
        {
            LogAssert.ignoreFailingMessages = true; // mandatory

            // Open & Close window to trigger 'link.xml' logic
            SentryTestWindow.Open().Dispose();

            var linkXmlPath = $"{Application.dataPath}/Resources/{UnitySentryOptions.ConfigRootFolder}/link.xml";
            Assert.IsTrue(File.Exists(linkXmlPath));
        }
    }
}
