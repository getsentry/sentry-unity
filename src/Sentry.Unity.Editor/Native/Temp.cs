using UnityEditor;
using UnityEngine;

namespace Sentry.Unity.Editor.Native
{
    public static class Temp
    {
        [MenuItem("Tools/ClickMe")]
        public static void ClickMe()
        {
            var options = new SentryUnityOptions() { Debug = true, DiagnosticLevel = SentryLevel.Debug, };
            options.DiagnosticLogger = new UnityLogger(options);

            var editorOptions = SentryEditorOptions.LoadEditorOptions();
            editorOptions.VSWherePath = "";
            editorOptions.MSBuildPath = "";

            MSBuildLocator.SetMSBuildPath(editorOptions, options.DiagnosticLogger);

            var windowsPlayerBuilder = SentryWindowsPlayer.Create(options.DiagnosticLogger);
            windowsPlayerBuilder.AddNativeOptions();
            windowsPlayerBuilder.AddSentryToMain();
            windowsPlayerBuilder.Build(editorOptions.MSBuildPath, "executablePath");
        }
    }
}
