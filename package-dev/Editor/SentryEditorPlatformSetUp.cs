using UnityEditor;
using UnityEngine;
using System;
using Sentry.Unity;
using Sentry.Unity.NativeUtils;

namespace Sentry.Unity.Editor
{
    internal static class SentryEditorPlatformSetUp
    {
        /// <summary>
        /// Called by Unity during domain reloads in the editor and before builds.
        /// This ensures that platform services are available during build-time processing.
        /// </summary>
        [InitializeOnLoadMethod]
        private static void SetUpUnityInfoInEditor()
        {
            try
            {
                _ = SentryPlatformServices.UnityInfo;
            }
            catch (InvalidOperationException)
            {
                SentryPlatformServices.UnityInfo = new SentryUnityInfo();
            }
        }
    }
}
