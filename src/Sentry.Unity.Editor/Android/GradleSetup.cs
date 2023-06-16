using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sentry.Extensibility;
using Sentry.Unity.Integrations;
using UnityEditor.Build;

namespace Sentry.Unity.Editor.Android
{
    internal class GradleSetup
    {
        private readonly IDiagnosticLogger _logger;

        public const string LocalRepository = @"maven { url ""${project(':unityLibrary').projectDir}/android-sdk-repository"" }";
        public const string RepositoryScopeName = "repositories";
        public static readonly string SdkDependencies = "implementation ('io.sentry:sentry-android:+') {{ exclude group: 'androidx.core' exclude group: 'androidx.lifecycle' }}";
        public const string DependencyScopeName = "dependencies";
        public const string MavenCentralWithoutFilter = "mavenCentral()";
        public const string MavenCentralWithFilter = "mavenCentral { content { excludeGroupByRegex \"io\\\\.sentry.*\" } }";
        public static readonly List<string> ScopesToSkip = new() { "buildscript", "pluginManagement" };

        private readonly string _rootGradle;
        private readonly string _settingsGradle;
        private readonly string _unityLibraryGradle;

        public GradleSetup(IDiagnosticLogger logger, string gradleProjectPath)
        {
            _logger = logger;
            _rootGradle = Path.Combine(gradleProjectPath, "build.gradle");
            _settingsGradle = Path.Combine(gradleProjectPath, "settings.gradle");
            _unityLibraryGradle = Path.Combine(gradleProjectPath, "unityLibrary", "build.gradle");
        }

        public void UpdateGradleProject(IApplication? application = null)
        {
            _logger.LogInfo("Adding Sentry to the gradle project.");

            // Starting with 2022.3.0f1 the root build.gradle updated to use the "new" way of importing plugins via `id`
            // Instead, dependency repositories get handled in the `settings.gradle` at the root
            if (SentryUnityVersion.IsNewerOrEqualThan("2022.3", application))
            {
                _logger.LogDebug("Updating the gradle file at '{0}'", _settingsGradle);
                var gradleContent = LoadGradleScript(_settingsGradle);
                gradleContent = InsertIntoScope(gradleContent, RepositoryScopeName, LocalRepository);
                gradleContent = SetMavenCentralFilter(gradleContent);
                File.WriteAllText(_settingsGradle, gradleContent);

                _logger.LogDebug("Updating the gradle file at '{0}'", _unityLibraryGradle);
                var unityLibraryGradleContent = LoadGradleScript(_unityLibraryGradle);
                unityLibraryGradleContent = InsertIntoScope(unityLibraryGradleContent, DependencyScopeName, SdkDependencies);
                File.WriteAllText(_unityLibraryGradle, unityLibraryGradleContent);
            }
            else
            {
                _logger.LogDebug("Updating the gradle file at '{0}'", _rootGradle);
                var gradleContent = LoadGradleScript(_rootGradle);
                gradleContent = InsertIntoScope(gradleContent, RepositoryScopeName, LocalRepository);
                File.WriteAllText(_rootGradle, gradleContent);

                _logger.LogDebug("Updating the gradle file at '{0}'", _unityLibraryGradle);
                var unityLibraryGradleContent = LoadGradleScript(_unityLibraryGradle);
                unityLibraryGradleContent = InsertIntoScope(unityLibraryGradleContent, DependencyScopeName, SdkDependencies);
                unityLibraryGradleContent = SetMavenCentralFilter(unityLibraryGradleContent);
                File.WriteAllText(_unityLibraryGradle, unityLibraryGradleContent);
            }
        }

        public void ClearGradleProject(IApplication? application = null)
        {
            _logger.LogInfo("Removing Sentry from the gradle project.");

            // Starting with 2022.3.0f1 the root build.gradle updated to use the "new" way of importing plugins via `id`
            // Instead, dependency repositories get handled in the `settings.gradle` at the root
            if (SentryUnityVersion.IsNewerOrEqualThan("2022.3", application))
            {
                _logger.LogDebug("Removing modifications from '{0}'", _settingsGradle);
                var gradleContent = LoadGradleScript(_settingsGradle);
                gradleContent = RemoveFromGradleContent(gradleContent, LocalRepository);
                gradleContent = gradleContent.Replace(MavenCentralWithFilter, MavenCentralWithoutFilter);
                File.WriteAllText(_settingsGradle, gradleContent);

                _logger.LogDebug("Removing modifications from the 'build.gradle' file at {0}", _unityLibraryGradle);
                var unityLibraryGradleContent = LoadGradleScript(_unityLibraryGradle);
                unityLibraryGradleContent = RemoveFromGradleContent(unityLibraryGradleContent, SdkDependencies);
                File.WriteAllText(_unityLibraryGradle, unityLibraryGradleContent);
            }
            else
            {
                _logger.LogDebug("Removing modifications from '{0}'", _rootGradle);
                var gradleContent = LoadGradleScript(_rootGradle);
                gradleContent = RemoveFromGradleContent(gradleContent, LocalRepository);
                File.WriteAllText(_rootGradle, gradleContent);

                _logger.LogDebug("Removing modifications from the 'build.gradle' file at {0}", _unityLibraryGradle);
                var unityLibraryGradleContent = LoadGradleScript(_unityLibraryGradle);
                unityLibraryGradleContent = RemoveFromGradleContent(unityLibraryGradleContent, SdkDependencies);
                unityLibraryGradleContent = unityLibraryGradleContent.Replace(MavenCentralWithFilter, MavenCentralWithoutFilter);
                File.WriteAllText(_unityLibraryGradle, unityLibraryGradleContent);
            }
        }

        internal string InsertIntoScope(string gradleContent, string scope, string insertion)
        {
            if (gradleContent.Contains(insertion))
            {
                _logger.LogDebug("The gradle file has already been updated. Skipping.");
                return gradleContent;
            }

            var lines = gradleContent.Split('\n');
            var scopeStart = FindBeginningOfScope(lines, scope);
            if (scopeStart == -1)
            {
                throw new BuildFailedException($"Failed to find scope '{scope}'.");
            }

            var modifiedLines = new List<string>(lines);

            var lineToInsert = string.Empty;
            var whiteSpaceCount = lines[scopeStart].IndexOf(scope, StringComparison.Ordinal);
            for (var i = 0; i < whiteSpaceCount; i++)
            {
                lineToInsert += " ";
            }

            lineToInsert += "    " + insertion;
            var lineOffset = lines[scopeStart].Contains("{") ? 1 : 2; // to make sure we're inside the scope
            modifiedLines.Insert(scopeStart + lineOffset, lineToInsert);

            return string.Join("\n", modifiedLines.ToArray());
        }

        internal string SetMavenCentralFilter(string gradleContent)
        {
            if (gradleContent.Contains(MavenCentralWithFilter))
            {
                _logger.LogDebug("The MavenFilter has already been set. Skipping.");
                return gradleContent;
            }

            var lines = gradleContent.Split('\n');
            var scopeStart = FindBeginningOfScope(lines, RepositoryScopeName);
            if (scopeStart == -1)
            {
                throw new BuildFailedException($"Failed to find scope '{RepositoryScopeName}'.");
            }

            for (var i = scopeStart; i < lines.Length; i++)
            {
                if (lines[i].Contains(MavenCentralWithoutFilter))
                {
                    lines[i] = lines[i].Replace(MavenCentralWithoutFilter, MavenCentralWithFilter);
                    break;
                }
            }

            return string.Join("\n", lines.ToArray());
        }

        private int FindBeginningOfScope(string[] lines, string scope)
        {
            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                // There are potentially multiple, nested scopes. We cannot add ourselves to the ones listed in `ScopesToSkip`
                if (ScopesToSkip.Any(line.Contains))
                {
                    var startIndex = i;

                    // In case the '{' is on the next line
                    if (!line.Contains("{") && lines[startIndex + 1].Contains("{"))
                    {
                        startIndex += 1;
                    }

                    i = FindClosingBracket(lines, startIndex);
                }
                else if (lines[i].Contains(scope))
                {
                    return i;
                }
            }

            return -1;
        }

        private static int FindClosingBracket(string[] lines, int startIndex)
        {
            var openBrackets = 0;

            for (var i = startIndex + 1; i < lines.Length; i++)
            {
                var line = lines[i];
                if (line.Contains("{"))
                {
                    openBrackets++;
                }

                if (line.Contains("}"))
                {
                    if (openBrackets == 0)
                    {
                        return i;
                    }

                    openBrackets--;
                }
            }

            throw new BuildFailedException("Failed to find the closing bracket.");
        }

        private static string RemoveFromGradleContent(string gradleContent, string toRemove)
            => gradleContent.Contains(toRemove) ? gradleContent.Replace(toRemove, "") : gradleContent;

        internal static string LoadGradleScript(string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Failed to find the gradle config.", path);
            }
            return File.ReadAllText(path);
        }
    }
}
