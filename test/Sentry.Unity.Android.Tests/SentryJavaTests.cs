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

    internal class TestAndroidJNI : IAndroidJNI
    {
        public bool AttachCalled { get; private set; }
        public bool DetachCalled { get; private set; }

        public void AttachCurrentThread() => AttachCalled = true;

        public void DetachCurrentThread() => DetachCalled = true;
    }
}
