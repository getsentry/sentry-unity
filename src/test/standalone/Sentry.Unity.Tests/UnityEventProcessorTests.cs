using NUnit.Framework;

namespace Sentry.Unity.Tests
{
    public class UnityEventProcessorTests
    {
        [Test]
        public void Process_NullEvent_ReturnsNull()
        {
            var sut = new UnityEventProcessor();
            Assert.Null(sut.Process(null!));
        }
    }
}
