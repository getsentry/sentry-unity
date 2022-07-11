using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Sentry.Extensibility;
using Sentry.Unity.Integrations;
using UnityEditor;

namespace Sentry.Unity.Editor.Android
{
    internal class ProguardSetup
    {
        private readonly IDiagnosticLogger _logger;
        private readonly string _gradleProjectPath;
        private readonly string _gradleScriptPath;
        public const string RuleFileName = "proguard-sentry-unity.pro";

        public ProguardSetup(IDiagnosticLogger logger, string gradleProjectPath)
        {
            _logger = logger;
            _gradleProjectPath = Path.Combine(gradleProjectPath, "unityLibrary");
            _gradleScriptPath = Path.Combine(_gradleProjectPath, "build.gradle");
        }

        public void RemoveFromGradleProject()
        {
            _logger.LogDebug("Removing Proguard rules from the gradle project.");
            var gradle = LoadGradleScript();
            if (!gradle.Contains(RuleFileName))
            {
                _logger.LogDebug("No reference to the Proguard ruleset {0} in {1}.", RuleFileName, _gradleScriptPath);
            }
            else
            {
                var gradleNew = Regex.Replace(gradle, @"(\s+consumerProguardFiles .*), *'" + RuleFileName + "'", "$1");
                if (gradle.Length == gradleNew.Length)
                {
                    throw new Exception($"Couldn't remove Proguard rule {RuleFileName} from {_gradleScriptPath}.");
                }
                File.WriteAllText(_gradleScriptPath, gradleNew);
            }

            var ruleFile = Path.Combine(_gradleProjectPath, RuleFileName);
            if (!File.Exists(ruleFile))
            {
                _logger.LogDebug("No Proguard ruleset file at {0} - nothing to remove.", ruleFile);
            }
            else
            {
                File.Delete(ruleFile);
            }
        }

        public void AddToGradleProject()
        {
            _logger.LogInfo("Adding Proguard rules to the gradle project.");
            var gradle = LoadGradleScript();

            var ruleFile = Path.Combine(_gradleProjectPath, RuleFileName);
            _logger.LogDebug("Writing proguard rule file to {0}.", ruleFile);
            File.Copy(Path.GetFullPath($"Packages/{SentryPackageInfo.GetName()}/Plugins/Android/{RuleFileName}"), ruleFile, true);

            if (gradle.Contains(RuleFileName))
            {
                _logger.LogDebug($"Proguard rule {RuleFileName} has already been added to {_gradleScriptPath} `consumerProguardFiles` in a previous build.");
            }
            else
            {
                var gradleNew = Regex.Replace(gradle, @"(\s+consumerProguardFiles .*)", "$1, '" + RuleFileName + "'");
                if (gradle.Length == gradleNew.Length)
                {
                    throw new Exception($"Couldn't add Proguard rule {RuleFileName} to {_gradleScriptPath} - no `consumerProguardFiles` found.");
                }
                File.WriteAllText(_gradleScriptPath, gradleNew);
            }
        }

        private string LoadGradleScript()
        {
            if (!File.Exists(_gradleScriptPath))
            {
                throw new FileNotFoundException($"Failed to find the gradle config.", _gradleScriptPath);
            }
            return File.ReadAllText(_gradleScriptPath);
        }
    }
}
