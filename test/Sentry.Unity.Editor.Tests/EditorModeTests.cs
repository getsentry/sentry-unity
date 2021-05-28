using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
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
    }
}
