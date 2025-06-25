using NUnit.Framework;
using Sentry.Unity.Tests.SharedClasses;

namespace Sentry.Unity.Android.Tests;

public class SentryJavaTests
{
    private TestLogger _logger = null!;
    private TestAndroidJNI _androidJni = null!;
    private SentryJava _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _logger = new TestLogger();
        _androidJni = new TestAndroidJNI();
        _sut = new SentryJava(_logger, _androidJni);
    }

<<<<<<< Updated upstream
    [Test]
    [TestCase(true, false, Description = "main thread = should attach")]
    [TestCase(false, true, Description = "non main thread = should not attach")]
    public void HandleJniThreadAttachment_AttachesIfMainThread(bool isMainThread, bool shouldAttach)
    {
        // Act
        _sut.HandleJniThreadAttachment(isMainThread);

        // Assert
        Assert.AreEqual(shouldAttach, _androidJni.AttachCalled);
    }

    [Test]
    [TestCase(true, false, Description = "main thread = should detach")]
    [TestCase(false, true, Description = "non main thread = should not detach")]
    public void HandleJniThreadDetachment_DetachesIfMainThread(bool isMainThread, bool shouldAttach)
    {
        // Act
        _sut.HandleJniThreadDetachment(isMainThread);

        // Assert
        Assert.AreEqual(shouldAttach, _androidJni.DetachCalled);
    }

=======
>>>>>>> Stashed changes
    internal class TestAndroidJNI : IAndroidJNI
    {
        public bool AttachCalled { get; private set; }
        public bool DetachCalled { get; private set; }

        public void AttachCurrentThread() => AttachCalled = true;

        public void DetachCurrentThread() => DetachCalled = true;
    }
}
