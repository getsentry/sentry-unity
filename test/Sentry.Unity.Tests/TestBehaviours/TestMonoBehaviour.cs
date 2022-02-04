using System;
using UnityEngine;

namespace Sentry.Unity.Tests.TestBehaviours
{
    /*
     * Behaviour we have access to from Tests project.
     */
    internal sealed class TestMonoBehaviour : MonoBehaviour
    {
        public void TestException()
            => throw new Exception("This is an exception");

        public void DebugLogError()
            => Debug.LogError("error");

        public void DebugLogException()
            => Debug.LogException(new Exception("Unity log exception"));
    }
}
