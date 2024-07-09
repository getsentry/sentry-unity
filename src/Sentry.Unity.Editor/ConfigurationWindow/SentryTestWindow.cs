using System;
using System.IO;
using UnityEditor;

namespace Sentry.Unity.Editor.ConfigurationWindow;

public sealed class SentryTestWindow : SentryWindow, IDisposable
{
    public SentryTestWindow()
    {
        // static
        SentryWindow.SentryOptionsAssetName = Path.GetRandomFileName();
    }

    public static SentryTestWindow Open()
        => (SentryTestWindow)GetWindow(typeof(SentryTestWindow));

    public void Dispose()
    {
        Close(); // calls 'OnLostFocus' implicitly
        AssetDatabase.DeleteAsset(ScriptableSentryUnityOptions.GetConfigPath(SentryOptionsAssetName));
    }
}
