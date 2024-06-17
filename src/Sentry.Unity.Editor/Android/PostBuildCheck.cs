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
            CheckUploadSymbolLogs(unityProjectPath);
            CheckUploadMappingLogs(unityProjectPath);
        }

        private void CheckUploadSymbolLogs(string unityProjectPath)
        {
            var symbolUploadLogPath = Path.Combine(unityProjectPath, "Logs", DebugSymbolUpload.SymbolUploadLogName);
            var hasSymbolError = HasError(symbolUploadLogPath, out var symbolError, out var symbolLog);
            if (hasSymbolError)
            {
                _logger.LogWarning($"Symbol upload task error: {symbolError}");
            }

            LogFileContent("Symbol upload log file content:", symbolLog, hasSymbolError);
            File.WriteAllText(symbolUploadLogPath, symbolLog); // Clean up the log file
        }

        private void CheckUploadMappingLogs(string unityProjectPath)
        {
            if (!AndroidUtils.ShouldUploadMapping())
            {
                _logger.LogDebug("Minification is disabled. Will not check upload mapping task result.");
                return;
            }

            var mappingUploadLogPath = Path.Combine(unityProjectPath, "Logs", DebugSymbolUpload.MappingUploadLogName);
            var hasMappingError = HasError(mappingUploadLogPath, out var mappingError, out var mappingLog);
            if (hasMappingError)
            {
                _logger.LogWarning($"Mapping upload task error: {mappingError}");
            }

            LogFileContent("Mapping upload log file content:", mappingLog, hasMappingError);
            File.WriteAllText(mappingUploadLogPath, mappingLog); // Clean up the log file
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
                fileContent = text;
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

        private void LogFileContent(string title, string fileContent, bool hasError)
        {
            var logFunction = new Action<string>(message =>
            {
                if (hasError)
                {
                    Debug.LogWarning(message);
                }
                else
                {
                    Debug.Log(message);
                }
            });

            logFunction(title);

            const int maxLogLength = 8192;
            if (fileContent.Length < maxLogLength)
            {
                logFunction(fileContent);
                return;
            }

            for (var i = 0; i < fileContent.Length; i += maxLogLength)
            {
                var chunkLength = maxLogLength + i > fileContent.Length ? fileContent.Length - i : maxLogLength;
                logFunction(fileContent.Substring(i, chunkLength));
            }
        }
    }
}
