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

        public void DebugLogError(string? message = null)
            => Debug.LogError(message ?? "error");
    }
}
