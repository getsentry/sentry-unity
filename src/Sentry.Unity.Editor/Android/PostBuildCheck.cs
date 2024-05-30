using System;
using System.IO;
using Sentry.Extensibility;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Sentry.Unity.Editor.Android
{
    public class PostBuildCheck : IPostprocessBuildWithReport
    {
        public int callbackOrder { get; } = 117;

        public void OnPostprocessBuild(BuildReport report)
        {
            var checker = new UploadTaskChecker(SentryScriptableObject.ConfiguredBuildTimeOptions);
            checker.CheckUploadTaskResult();
        }
    }

    internal class UploadTaskChecker
    {
        private readonly SentryUnityOptions? _options;
        private readonly SentryCliOptions? _sentryCliOptions;
        private readonly IDiagnosticLogger _logger;

        internal UploadTaskChecker(Func<(SentryUnityOptions?, SentryCliOptions?)> getOptions)
        {
            (_options, _sentryCliOptions) = getOptions();
            _logger = _options?.DiagnosticLogger ?? new UnityLogger(_options ?? new SentryUnityOptions());
        }

        internal void CheckUploadTaskResult()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                _logger.LogDebug("Target platform is not Android. Will not validate the upload task.");
                return;
            }

            if (EditorUserBuildSettings.exportAsGoogleAndroidProject)
            {
                _logger.LogDebug("The task is not executed during export. Will not validate the upload task.");
                return;
            }

            // ReSharper disable once Unity.NoNullPropagation
            if (!(_sentryCliOptions?.UploadSymbols ?? false))
            {
                _logger.LogDebug("Symbol Upload is disabled. Will not validate the upload task.");
                return;
            }

            var projectDir = Directory.GetParent(Application.dataPath);
            if (projectDir == null)
            {
                _logger.LogDebug("Could not find Unity Project path. Will not check upload symbols task result.");
                return;
            }

            var unityProjectPath = projectDir.FullName;
            var symbolUploadLogPath = Path.Combine(unityProjectPath, "Logs", DebugSymbolUpload.SymbolUploadLogName);
            if (HasError(symbolUploadLogPath, out var symbolError, out var symbolLog))
            {
                _logger.LogWarning($"Symbol upload task error: {symbolError}");
                _logger.LogWarning("Symbol upload log file content:");
                LogFileContent(symbolLog);
                File.WriteAllText(symbolUploadLogPath, symbolLog); // Clean up the log file
            }

            var mappingUploadLogPath = Path.Combine(unityProjectPath, "Logs", DebugSymbolUpload.MappingUploadLogName);
            if (HasError(mappingUploadLogPath, out var mappingError, out var mappingLog))
            {
                _logger.LogWarning($"Mapping upload task error: {mappingError}");
                _logger.LogWarning("Mapping upload log file content:");
                LogFileContent(mappingLog);
                File.WriteAllText(mappingUploadLogPath, mappingLog); // Clean up the log file
            }
        }

        private bool HasError(string filePath, out string error, out string fileContent)
        {
            fileContent = error = string.Empty;
            const string errorMarker = "===ERROR===";
            if (!File.Exists(filePath))
            {
                return false;
            }

            var text = File.ReadAllText(filePath);
            if (!text.Contains(errorMarker))
            {
                return false;
            }

            var index = text.IndexOf(errorMarker, StringComparison.InvariantCulture);
            if (index < 0)
            {
                return false;
            }

            fileContent = text.Substring(0, index);
            error = text.Substring(index + errorMarker.Length);
            return !string.IsNullOrEmpty(error);
        }

        private void LogFileContent(string fileContent)
        {
            const int maxLogLength = 8192;
            if (fileContent.Length < maxLogLength)
            {
                Debug.LogWarning(fileContent);
                return;
            }

            for (var i = 0; i < fileContent.Length; i += maxLogLength)
            {
                var chunkLength = maxLogLength + i > fileContent.Length ? fileContent.Length - i : maxLogLength;
                Debug.LogWarning(fileContent.Substring(i, chunkLength));
            }
        }
    }
}
