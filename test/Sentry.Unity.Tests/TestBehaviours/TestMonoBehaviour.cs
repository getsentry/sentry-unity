using System;
using System.Threading.Tasks;
using UnityEngine;

namespace Sentry.Unity.Tests.TestBehaviours
{
    /*
     * Behaviour we have access to from Tests project.
     */
    internal sealed class TestMonoBehaviour : MonoBehaviour
    {
        public void ThrowException(string message) => throw new Exception(message);
        public void DebugLogError(string message) => Debug.LogError(message);
        public void DebugLogErrorInTask(string message) => Task.Run(() => DebugLogError(message));
        public void DebugLogException(string message) => Debug.LogException(new Exception(message));
        public void DebugLogExceptionInTask(string message) => Task.Run(() => DebugLogException(message));
    }
}
