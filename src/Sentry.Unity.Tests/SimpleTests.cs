using NUnit.Framework;
using UnityEngine;

namespace Sentry.Unity.Tests
{
    public sealed class SimpleTests
    {

        [Test]
        public void SentryUnity_Object_NotNull()
        {
            var unitySentryOptions = ScriptableObject.CreateInstance<UnitySentryOptions>();
            Assert.NotNull(unitySentryOptions);
        }
    }
}
