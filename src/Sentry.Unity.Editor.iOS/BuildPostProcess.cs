using System;
using Sentry.Extensibility;
using UnityEditor;
using UnityEditor.Callbacks;

namespace Sentry.Unity.Editor.iOS
{
    public static class BuildPostProcess
    {
        [PostProcessBuild(1)]
        public static void OnPostProcessBuild(BuildTarget target, string pathToProject)
        {
            if (target != BuildTarget.iOS)
            {
                return;
            }

            var options = ScriptableSentryUnityOptions.LoadSentryUnityOptions(BuildPipeline.isBuildingPlayer);

            try
            {
                using var sentryXcodeProject = SentryXcodeProject.Open(pathToProject);

                if (PlayerSettings.iOS.sdkVersion == iOSSdkVersion.DeviceSDK)
                {
                    sentryXcodeProject.AddSentryDeviceFramework();
                }
                else
                {
                    sentryXcodeProject.AddSentrySimulatorFramework();
                }

                if (options?.Validate() != true)
                {
                    new UnityLogger(new SentryOptions()).LogWarning("Failed to validate Sentry Options. Native support disabled.");
                    return;
                }

                if (!options.IosNativeSupportEnabled)
                {
                    options.DiagnosticLogger?.LogDebug("iOS Native support disabled through the options.");
                    return;
                }

                sentryXcodeProject.AddNativeOptions(options);
                sentryXcodeProject.AddSentryToMain(options);
            }
            catch (Exception e)
            {
                options?.DiagnosticLogger?.LogError("Failed to add the Sentry framework to the generated Xcode project", e);
            }
        }
    }
}
