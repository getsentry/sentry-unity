using System;
using NUnit.Framework;

namespace Sentry.Unity.Android.Tests
{
    public class AndroidJavaScopeObserverTests
    {
        private class Fixture
        {
            public SentryUnityOptions Options { get; set; } = new();
            public AndroidJavaScopeObserver GetSut() => new(Options);
        }

        private Fixture _fixture = new();

        [SetUp]
        public void SetUp() => _fixture = new Fixture();

    }
}
