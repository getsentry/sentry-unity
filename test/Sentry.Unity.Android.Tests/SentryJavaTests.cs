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

    [Test]
    public void HandleJniThreadAttachment_MainThreadNotAttached_DoesNotAttach()
    {
        // Arrange
        _androidJni.SetAttached(false);

        // Act
        _sut.HandleJniThreadAttachment(isMainThread: true);

        // Assert
        Assert.IsFalse(_androidJni.AttachCalled);
    }

    [Test]
    public void HandleJniThreadAttachment_NonMainThreadNotAttached_Attaches()
    {
        // Arrange
        _androidJni.SetAttached(false);

        // Act
        _sut.HandleJniThreadAttachment(isMainThread: false);

        // Assert
        Assert.IsTrue(_androidJni.AttachCalled);
    }

    [Test]
    public void HandleJniThreadAttachment_NonMainThreadAlreadyAttached_DoesNotAttach()
    {
        // Arrange
        _androidJni.SetAttached(true);

        // Act
        _sut.HandleJniThreadAttachment(isMainThread: false);

        // Assert
        Assert.IsFalse(_androidJni.AttachCalled);
    }

    [Test]
    public void HandleJniThreadDetachment_MainThread_DoesNotDetach()
    {
        // Act
        _sut.HandleJniThreadDetachment(isMainThread: true);

        // Assert
        Assert.IsFalse(_androidJni.DetachCalled);
    }

    [Test]
    public void HandleJniThreadDetachment_NonMainThreadSdkAttached_Detaches()
    {
        // Arrange
        _androidJni.SetAttached(false);
        _sut.HandleJniThreadAttachment(isMainThread: false);
        _androidJni.Reset();

        // Act
        _sut.HandleJniThreadDetachment(isMainThread: false);

        // Assert
        Assert.IsTrue(_androidJni.DetachCalled);
    }

    [Test]
    public void HandleJniThreadDetachment_NonMainThreadSdkDidNotAttach_DoesNotDetach()
    {
        // Arrange
        _androidJni.SetAttached(true);

        // Act
        _sut.HandleJniThreadDetachment(isMainThread: false);

        // Assert
        Assert.IsFalse(_androidJni.DetachCalled);
    }

    [Test]
    public void HandleJniThreadAttachment_AttachDetachCycle_WorksCorrectly()
    {
        // Arrange
        _androidJni.SetAttached(false);

        // Act & Assert
        _sut.HandleJniThreadAttachment(isMainThread: false);
        Assert.IsTrue(_androidJni.AttachCalled, "Should attach on non-main thread when not attached");

        // Reset flags for detach phase
        _androidJni.Reset();

        // Act & Assert
        _sut.HandleJniThreadDetachment(isMainThread: false);
        Assert.IsTrue(_androidJni.DetachCalled, "Should detach on non-main thread when SDK attached");
    }

    internal class TestAndroidJNI : IAndroidJNI
    {
        private bool _isAttached;

        public bool AttachCalled { get; private set; }
        public bool DetachCalled { get; private set; }

        public void AttachCurrentThread()
        {
            AttachCalled = true;
            _isAttached = true;
        }

        public void DetachCurrentThread()
        {
            DetachCalled = true;
            _isAttached = false;
        }

        public int GetVersion() => _isAttached ? 1 : 0;

        // Test helper methods
        public void SetAttached(bool attached) => _isAttached = attached;

        public void Reset()
        {
            AttachCalled = false;
            DetachCalled = false;
        }
    }
}
