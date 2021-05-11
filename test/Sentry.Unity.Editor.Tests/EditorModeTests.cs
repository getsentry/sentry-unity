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

            var linkXmlPath = $"{Application.dataPath}/Resources/{SentryUnityOptions.ConfigRootFolder}/link.xml";
            Assert.IsTrue(File.Exists(linkXmlPath));
        }
    }
}
