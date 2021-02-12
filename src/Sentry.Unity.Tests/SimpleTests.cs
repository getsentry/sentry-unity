using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

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

        [UnityTest]
        public IEnumerator UnityCoroutine_CreatedGameObjectWithDelay_Found()
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "test cube";
            yield return new WaitForSeconds(2);
            Assert.IsTrue(GameObject.Find(cube.name));
        }
    }
}
