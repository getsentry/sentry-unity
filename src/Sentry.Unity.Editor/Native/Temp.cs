using UnityEditor;

namespace Sentry.Unity.Editor.Native
{
    public static class Temp
    {
        [MenuItem("Tools/ClickMe")]
        public static void ClickMe()
        {
            var options = new SentryUnityOptions() {Debug = true, DiagnosticLevel = SentryLevel.Debug,};
            options.DiagnosticLogger = new UnityLogger(options);
            SentryWindowsPlayer.Build(options, "");
        }
    }
}
