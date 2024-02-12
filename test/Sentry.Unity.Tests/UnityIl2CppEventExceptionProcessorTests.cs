using NUnit.Framework;

namespace Sentry.Unity.Tests
{
    public class UnityIl2CppEventProcessorTests
    {
        [Test]
        [TestCase(null, null)]
        [TestCase("f30fef22-d93e-7f60-0000-000000000000", "f30fef22d93e7f60")]
        [TestCase("0c9249e5-e223-8bd5-0000-000000000000", "0c9249e5e2238bd5")]
        [TestCase("6f42afa0-45c8-86e6-2372-a02513d55560", "6f42afa045c886e62372a02513d55560")]
        [TestCase("ff25f952-44c9-4c78-b54f-f8403f185b50-b9a5f714", "ff25f95244c94c78b54ff8403f185b50b9a5f714")]
        [TestCase("94552647-48dc-4fe4-ba75-7ccd3c43c44d-917f8072", "9455264748dc4fe4ba757ccd3c43c44d917f8072")]
        public void NormalizeUuid_ReturnValueMatchesExpected(string input, string expected)
        {
            var actual = UnityIl2CppEventProcessor.NormalizeUuid(input);

            Assert.AreEqual(actual, expected);
        }
    }
}
