using System;
using System.IO;
using UnityEditor;

namespace Sentry.Unity.Editor
{
    public sealed class SentryTestWindow : SentryWindow, IDisposable
    {
        protected override string SentryOptionsAssetName { get; } = Path.GetRandomFileName();

        public static SentryTestWindow Open()
            => (SentryTestWindow)GetWindow(typeof(SentryTestWindow));

        public void Dispose()
        {
            Close(); // calls 'OnLostFocus' implicitly
            // File.Delete(SentryUnityOptions.GetConfigPath(SentryOptionsAssetName));
            AssetDatabase.Refresh();
        }
    }
}
