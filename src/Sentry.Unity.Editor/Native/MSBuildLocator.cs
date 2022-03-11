using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Sentry.Extensibility;
using UnityEditor.PackageManager;

namespace Sentry.Unity.Editor.Native
{
    internal static class MSBuildLocator
    {
        public static void SetMSBuildPath(SentryEditorOptions editorOptions, IDiagnosticLogger? logger)
        {
            if (!File.Exists(editorOptions.VSWherePath))
            {
                logger?.LogDebug("Failed to find 'VSWhere' at '{0}'. Trying to locate.", editorOptions.VSWherePath);
                SetVSWherePath(editorOptions, logger);
            }

            logger?.LogDebug("Using 'VSWhere' at '{0}' to locate MSBuild.", editorOptions.VSWherePath);

            var vsWhereOutput = "";
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = editorOptions.VSWherePath,
                    Arguments = "-latest -requires Microsoft.Component.MSBuild -find MSBuild\\**\\Bin\\MSBuild.exe",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.OutputDataReceived += (sender, args) => vsWhereOutput += args.Data;
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();

            logger?.LogDebug("VSWhere returned with: '{0}'", vsWhereOutput);

            if (!File.Exists(vsWhereOutput))
            {
                throw new FileNotFoundException($"Failed to locate 'MSBuild'.");
            }

            editorOptions.MSBuildPath = vsWhereOutput;
        }

        internal static void SetVSWherePath(SentryEditorOptions editorOptions, IDiagnosticLogger? logger)
        {
            logger?.LogDebug("Requesting packages from Client.");

            var packageListRequest = Client.List(true);
            while (!packageListRequest.IsCompleted)
            {
                // TODO: timeout - can't use Task.Run because it has to be on the main thread
            }

            if (packageListRequest.Status == StatusCode.Success)
            {
                logger?.LogDebug("Successfully retrieved installed packages.");

                var visualstudioPackage = packageListRequest.Result.FirstOrDefault(p => p.name == "com.unity.ide.visualstudio");
                if (visualstudioPackage != null)
                {
                    logger?.LogDebug("Located com.unity.ide.visualstudio at '{0}'", visualstudioPackage.resolvedPath);

                    var vsWherePath = Path.Combine(visualstudioPackage.resolvedPath, "Editor", "VSWhere", "vswhere.exe");
                    if (File.Exists(vsWherePath))
                    {
                        logger?.LogDebug("Setting 'VSWhere' to '{0}'", vsWherePath);
                        editorOptions.VSWherePath = vsWherePath;
                    }
                    else
                    {
                        throw new FileNotFoundException($"Failed to locate 'VSWhere' at {vsWherePath}");
                    }
                }
                else
                {
                    throw new Exception("Failed to locate the 'com.unity.ide.visualstudio' package.");
                }
            }
            else
            {
                throw new Exception("Failed to retrieve installed packages.");
            }
        }
    }
}
