using System;
using System.IO;
using System.Text;
using System.Xml;
using Sentry.Extensibility;
using UnityEditor;
using UnityEditor.Android;
using UnityEngine;

namespace Sentry.Unity.Editor.Android
{
    // https://github.com/getsentry/sentry-java/blob/d3764bfc97eed22564a1e23ba96fa73ad2685498/sentry-android-core/src/main/java/io/sentry/android/core/ManifestMetadataReader.java#L83-L217
    public class AndroidManifestConfiguration : IPostGenerateGradleAndroidProject
    {
        private readonly Func<SentryUnityOptions?> _getOptions;
        private readonly IUnityLoggerInterceptor? _interceptor;

        // Lower levels are called first.
        public int callbackOrder => 1;

        public AndroidManifestConfiguration()
            : this(() => ScriptableSentryUnityOptions.LoadSentryUnityOptions(BuildPipeline.isBuildingPlayer))
        {
        }

        // Testing
        internal AndroidManifestConfiguration(
            Func<SentryUnityOptions?> getOptions,
            IUnityLoggerInterceptor? interceptor = null)
        {
            _getOptions = getOptions;
            _interceptor = interceptor;
        }

        public void OnPostGenerateGradleAndroidProject(string basePath)
        {
            var manifestPath = GetManifestPath(basePath);
            if (!File.Exists(manifestPath))
            {
                throw new FileNotFoundException("Can't configure native Android SDK nor set auto-init:false.", manifestPath);
            }

            var disableAutoInit = false;
            var options = _getOptions();
            if (options is null)
            {
                var logger = new UnityLogger(new SentryOptions(), _interceptor);
                logger.LogWarning("Couldn't load SentryOptions. Can't configure native Android SDK.");
                disableAutoInit = true;
            }
            else if (!options.Validate())
            {
                options.DiagnosticLogger?.LogWarning("Failed to validate Sentry Options. Android native support will not be configured.");
                disableAutoInit = true;
            }
            else if (!options.AndroidNativeSupportEnabled)
            {
                options.DiagnosticLogger?.LogDebug("Android Native support disabled via options.");
                disableAutoInit = true;
            }

            var androidManifest = new AndroidManifest(manifestPath);

            if (disableAutoInit)
            {
                androidManifest.DisableSentryAndSave();
                return;
            }

            options!.DiagnosticLogger?.LogDebug("Configuring Sentry options on AndroidManifest: {0}", basePath);

            options.DiagnosticLogger?.LogDebug("Setting DSN: {0}", options.Dsn);
            androidManifest.SetDsn(options.Dsn!);
            if (!options.DebugOnlyInEditor)
            {
                options.DiagnosticLogger?.LogDebug("Setting Debug: {0}", options.Debug);
                androidManifest.SetDebug(options.Debug);
            }
            else
            {
                options.DiagnosticLogger?.LogDebug("Not setting debug flag because DebugOnlyInEditor is true");
            }

            if (options.Release is not null)
            {
                options.DiagnosticLogger?.LogDebug("Setting Release: {0}", options.Release);
                androidManifest.SetRelease(options.Release);
            }
            if (options.Environment is not null)
            {
                options.DiagnosticLogger?.LogDebug("Setting Environment: {0}", options.Environment);
                androidManifest.SetEnvironment(options.Environment);
            }

            options.DiagnosticLogger?.LogDebug("Setting DiagnosticLevel: {0}", options.DiagnosticLevel);
            androidManifest.SetLevel(options.DiagnosticLevel);

            if (options.SampleRate.HasValue)
            {
                options.DiagnosticLogger?.LogDebug("Setting SampleRate: {0}", options.SampleRate);
                androidManifest.SetSampleRate(options.SampleRate.Value);
            }

            // TODO: Missing on AndroidManifest
            // options.DiagnosticLogger?.LogDebug("Setting MaxBreadcrumbs: {0}", options.MaxBreadcrumbs);
            // androidManifest.SetMaxBreadcrumbs(options.MaxBreadcrumbs);
            // options.DiagnosticLogger?.LogDebug("Setting MaxCacheItems: {0}", options.MaxCacheItems);
            // androidManifest.SetMaxCacheItems(options.MaxCacheItems);
            // options.DiagnosticLogger?.LogDebug("Setting SendDefaultPii: {0}", options.SendDefaultPii);
            // // androidManifest.SetSendDefaultPii(options.SendDefaultPii);

            // Disabling the native in favor of the C# layer for now
            androidManifest.SetAutoSessionTracking(false);
            // TODO: We need an opt-out for this:
            androidManifest.SetNdkScopeSync(true);

            // TODO: All SentryOptions and create specific Android options

            _ = androidManifest.Save();

            AppendSymbolUploadToGradleProject(basePath, options);
        }

        internal void AppendSymbolUploadToGradleProject(string basePath, SentryUnityOptions options)
        {
            var cliOptions = SentryCliOptions.LoadCliOptions();
            if (!cliOptions.UploadSymbols || EditorUserBuildSettings.development)
            {
                return;
            }

            var gradleProjectPath = Directory.GetParent(basePath);
            var unityProjectPath = Directory.GetParent(Application.dataPath);
            var symbolsPath = Path.Combine(unityProjectPath.FullName, "Temp", "StagingArea", "symbols");
            // TODO: remove? - during the first build there are no symbols
            if (!Directory.Exists(symbolsPath))
            {
                options.DiagnosticLogger?.LogWarning($"Could not find symbols at path: {symbolsPath}");
            }

            // TODO: Fix dev package pathing
            var sentryCliPath = Path.GetFullPath(Path.Combine("Packages", "io.sentry.unity.dev", "Editor", "sentry-cli", GetSentryCli()));
            if (!File.Exists(sentryCliPath))
            {
                options.DiagnosticLogger?.LogError($"Could not find sentry-cli at path: {sentryCliPath}");
                return;
            }

            // TODO: set permission for sentry-cli

            using var streamWriter = File.AppendText(Path.Combine(gradleProjectPath.FullName, "build.gradle"));
            streamWriter.Write($@"
gradle.taskGraph.whenReady {{
    gradle.taskGraph.allTasks[-1].doLast {{
        println 'Uploading symbols to Sentry'
        exec {{
            executable = ""{sentryCliPath}""
            args = [""--auth-token"", ""{cliOptions.Auth}"", ""upload-dif"", ""--org"", ""{cliOptions.Organization}"", ""--project"", ""{cliOptions.Project}"", ""{symbolsPath}""]
        }}
    }}
}}");
        }

        internal static string GetSentryCli()
        {
            return Application.platform switch
            {
                RuntimePlatform.WindowsEditor => "",
                RuntimePlatform.OSXEditor => "sentry-cli-Darwin-universal",
                RuntimePlatform.LinuxEditor => "",
                _ => string.Empty
            };
        }

        internal static string GetManifestPath(string basePath) =>
            new StringBuilder(basePath)
                .Append(Path.DirectorySeparatorChar)
                .Append("src")
                .Append(Path.DirectorySeparatorChar)
                .Append("main")
                .Append(Path.DirectorySeparatorChar)
                .Append("AndroidManifest.xml")
                .ToString();
    }

    internal class AndroidXmlDocument : XmlDocument
    {
        private readonly string _path;
        protected const string AndroidXmlNamespace = "http://schemas.android.com/apk/res/android";

        protected AndroidXmlDocument(string path)
        {
            _path = path;
            using (var reader = new XmlTextReader(_path))
            {
                _ = reader.Read();
                Load(reader);
            }
            var nsManager = new XmlNamespaceManager(NameTable);
            nsManager.AddNamespace("android", AndroidXmlNamespace);
        }

        public string Save() => SaveAs(_path);

        private string SaveAs(string path)
        {
            using var writer = new XmlTextWriter(path, new UTF8Encoding(false)) { Formatting = Formatting.Indented };
            Save(writer);
            return path;
        }
    }

    internal class AndroidManifest : AndroidXmlDocument
    {
        private readonly XmlElement _applicationElement;

        public AndroidManifest(string path) : base(path) =>
            _applicationElement = (XmlElement)SelectSingleNode("/manifest/application");

        // Without this we get:
        // Unable to get provider io.sentry.android.core.SentryInitProvider: java.lang.IllegalArgumentException: DSN is required. Use empty string to disable SDK.
        internal void DisableSentryAndSave()
        {
            SetMetaData("io.sentry.auto-init", "false");
            _ = Save();
        }

        internal void SetDsn(string dsn) => SetMetaData("io.sentry.dsn", dsn);
        internal void SetSampleRate(float sampleRate) => SetMetaData("io.sentry.sample-rate", sampleRate.ToString());
        internal void SetRelease(string release) => SetMetaData("io.sentry.release", release);
        internal void SetEnvironment(string environment) => SetMetaData("io.sentry.environment", environment);
        internal void SetAutoSessionTracking(bool enableAutoSessionTracking)
            => SetMetaData("io.sentry.auto-session-tracking.enable", enableAutoSessionTracking.ToString());
        internal void SetNdkScopeSync(bool enableNdkScopeSync)
            => SetMetaData("io.sentry.ndk.scope-sync.enable", enableNdkScopeSync.ToString());
        internal void SetDebug(bool debug) => SetMetaData("io.sentry.debug", debug ? "true" : "false");

        // https://github.com/getsentry/sentry-java/blob/db4dfc92f202b1cefc48d019fdabe24d487db923/sentry/src/main/java/io/sentry/SentryLevel.java#L4-L9
        internal void SetLevel(SentryLevel level) =>
            SetMetaData("io.sentry.debug.level", level switch
            {
                SentryLevel.Debug => "debug",
                SentryLevel.Error => "error",
                SentryLevel.Fatal => "fatal",
                SentryLevel.Info => "info",
                SentryLevel.Warning => "warning",
                _ => "debug"
            });

        private void SetMetaData(string key, string value)
        {
            var element = _applicationElement.AppendChild(_applicationElement.OwnerDocument
                .CreateElement("meta-data"));
            _ = element.Attributes.Append(CreateAndroidAttribute("name", key));
            _ = element.Attributes.Append(CreateAndroidAttribute("value", value));
        }

        private XmlAttribute CreateAndroidAttribute(string key, string value)
        {
            var attr = CreateAttribute("android", key, AndroidXmlNamespace);
            attr.Value = value;
            return attr;
        }
    }
}
