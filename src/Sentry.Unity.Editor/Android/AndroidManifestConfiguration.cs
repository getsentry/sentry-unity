using System;
using System.Collections.Generic;
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
        private readonly SentryUnityOptions? _options;
        private readonly SentryCliOptions? _sentryCliOptions;
        private readonly IUnityLoggerInterceptor? _interceptor;

        private readonly bool _isDevelopmentBuild;
        private readonly ScriptingImplementation _scriptingImplementation;

        // Lower levels are called first.
        public int callbackOrder => 1;

        public AndroidManifestConfiguration()
            : this(
                () => SentryScriptableObject.Load<ScriptableSentryUnityOptions>(ScriptableSentryUnityOptions.GetConfigPath())
                    ?.ToSentryUnityOptions(BuildPipeline.isBuildingPlayer),
                () => SentryScriptableObject.Load<SentryCliOptions>(SentryCliOptions.GetConfigPath()),
                isDevelopmentBuild: EditorUserBuildSettings.development,
                scriptingImplementation: PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android))
        {
        }

        // Testing
        internal AndroidManifestConfiguration(
            Func<SentryUnityOptions?> getOptions,
            Func<SentryCliOptions?> getSentryCliOptions,
            IUnityLoggerInterceptor? interceptor = null,
            bool isDevelopmentBuild = false,
            ScriptingImplementation scriptingImplementation = ScriptingImplementation.IL2CPP)
        {
            _options = getOptions();
            _sentryCliOptions = getSentryCliOptions();

            _interceptor = interceptor;

            _isDevelopmentBuild = isDevelopmentBuild;
            _scriptingImplementation = scriptingImplementation;
        }

        public void OnPostGenerateGradleAndroidProject(string basePath)
        {
            ModifyManifest(basePath);

            var unityProjectPath = Directory.GetParent(Application.dataPath).FullName;
            var gradleProjectPath = Directory.GetParent(basePath).FullName;
            SetupSymbolsUpload(unityProjectPath, gradleProjectPath);
        }

        internal void ModifyManifest(string basePath)
        {
            var manifestPath = GetManifestPath(basePath);
            if (!File.Exists(manifestPath))
            {
                throw new FileNotFoundException("Can't configure native Android SDK nor set auto-init:false.",
                    manifestPath);
            }

            var disableAutoInit = false;
            var logger = _options?.DiagnosticLogger ?? new UnityLogger(_options ?? new SentryUnityOptions(), _interceptor);

            if (_options is null)
            {
                logger.LogWarning("Android Native support disabled. " +
                                  "Sentry has not been configured. You can do that through the editor: Tools -> Sentry");
                disableAutoInit = true;
            }
            else if (!_options.IsValid())
            {
                logger.LogDebug("Native support disabled.");
                disableAutoInit = true;
            }
            else if (!_options.AndroidNativeSupportEnabled)
            {
                logger.LogDebug("Android Native support disabled through the options.");
                disableAutoInit = true;
            }

            var androidManifest = new AndroidManifest(manifestPath, _options?.DiagnosticLogger);
            androidManifest.RemovePreviousConfigurations();
            androidManifest.AddDisclaimerComment();

            if (disableAutoInit)
            {
                androidManifest.DisableSentryAndSave();
                return;
            }

            logger.LogDebug("Configuring Sentry options on AndroidManifest: {0}", basePath);
            androidManifest.SetSDK("sentry.java.android.unity");
            logger.LogDebug("Setting DSN: {0}", _options!.Dsn);
            androidManifest.SetDsn(_options.Dsn!);
            if (_options.Debug)
            {
                logger.LogDebug("Setting Debug: {0}", _options.Debug);
                androidManifest.SetDebug(_options.Debug);
            }

            if (_options.Release is not null)
            {
                logger.LogDebug("Setting Release: {0}", _options.Release);
                androidManifest.SetRelease(_options.Release);
            }

            if (_options.Environment is not null)
            {
                logger.LogDebug("Setting Environment: {0}", _options.Environment);
                androidManifest.SetEnvironment(_options.Environment);
            }

            logger.LogDebug("Setting DiagnosticLevel: {0}", _options.DiagnosticLevel);
            androidManifest.SetLevel(_options.DiagnosticLevel);

            if (_options.SampleRate.HasValue)
            {
                logger.LogDebug("Setting SampleRate: {0}", _options.SampleRate);
                androidManifest.SetSampleRate(_options.SampleRate.Value);
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
        }

        internal void SetupSymbolsUpload(string unityProjectPath, string gradleProjectPath)
        {
            var disableSymbolsUpload = false;
            var logger = _options?.DiagnosticLogger ?? new UnityLogger(new SentryUnityOptions());
            var symbolsUpload = new DebugSymbolUpload(logger, unityProjectPath, gradleProjectPath,
                EditorUserBuildSettings.exportAsGoogleAndroidProject);

            if (_options is null || !_options.Enabled || !_options.AndroidNativeSupportEnabled)
            {
                disableSymbolsUpload = true;
            }

            if (_sentryCliOptions is null)
            {
                logger.LogWarning("Failed to load sentry-cli options.");
                disableSymbolsUpload = true;
            }
            else if (!_sentryCliOptions.IsValid(logger, _isDevelopmentBuild))
            {
                disableSymbolsUpload = true;
            }
            else if (_scriptingImplementation != ScriptingImplementation.IL2CPP)
            {
                logger.LogDebug("Automated symbols upload requires the IL2CPP scripting backend.");
                disableSymbolsUpload = true;
            }

            if (disableSymbolsUpload)
            {
                symbolsUpload.RemoveUploadFromGradleFile();
                return;
            }

            try
            {
                logger.LogInfo("Adding automated debug symbols upload to the gradle project.");

                var sentryCliPath = SentryCli.SetupSentryCli();
                SentryCli.CreateSentryProperties(gradleProjectPath, _sentryCliOptions!, _options!);

                if (EditorUserBuildSettings.exportAsGoogleAndroidProject)
                {
                    symbolsUpload.TryCopySymbolsToGradleProject();
                }

                symbolsUpload.AppendUploadToGradleFile(sentryCliPath);
            }
            catch (Exception e)
            {
                logger.LogError("Failed to add the automatic symbols upload to the gradle project", e);
            }
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
        protected const string AndroidNsPrefix = "android";

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
        private const string SentryPrefix = "io.sentry";
        private const string Disclaimer = "GENERATED BY SENTRY. Changes to the Sentry options will be lost!";

        private readonly XmlElement _applicationElement;
        private readonly IDiagnosticLogger? _logger;

        public AndroidManifest(string path, IDiagnosticLogger? logger) : base(path)
        {
            _applicationElement = (XmlElement)SelectSingleNode("/manifest/application");
            _logger = logger;
        }

        internal void RemovePreviousConfigurations()
        {
            var nodesToRemove = new List<XmlNode>();

            foreach (XmlNode node in _applicationElement.ChildNodes)
            {
                if (node.NodeType == XmlNodeType.Comment && node.Value.Equals(Disclaimer))
                {
                    nodesToRemove.Add(node);
                    continue;
                }

                if (node.Name.Equals("meta-data"))
                {
                    foreach (XmlAttribute attr in node.Attributes)
                    {
                        if (attr.Prefix.Equals(AndroidNsPrefix) && attr.LocalName.Equals("name") &&
                            attr.Value.StartsWith(SentryPrefix))
                        {
                            _logger?.LogDebug("Removing AndroidManifest meta-data '{0}'", attr.Value);
                            nodesToRemove.Add(node);
                            break;
                        }
                    }
                }
            }

            foreach (var node in nodesToRemove)
            {
                _ = node.ParentNode.RemoveChild(node);
            }
        }

        public void AddDisclaimerComment() =>
            _applicationElement.AppendChild(_applicationElement.OwnerDocument.CreateComment(Disclaimer));

        // Without this we get:
        // Unable to get provider io.sentry.android.core.SentryInitProvider: java.lang.IllegalArgumentException: DSN is required. Use empty string to disable SDK.
        internal void DisableSentryAndSave()
        {
            SetMetaData($"{SentryPrefix}.auto-init", "false");
            _ = Save();
        }

        internal void SetDsn(string dsn) => SetMetaData($"{SentryPrefix}.dsn", dsn);

        internal void SetSampleRate(float sampleRate) =>
            SetMetaData($"{SentryPrefix}.sample-rate", sampleRate.ToString());

        internal void SetRelease(string release) => SetMetaData($"{SentryPrefix}.release", release);
        internal void SetEnvironment(string environment) => SetMetaData($"{SentryPrefix}.environment", environment);

        internal void SetSDK(string name) => SetMetaData($"{SentryPrefix}.sdk.name", name);

        internal void SetAutoSessionTracking(bool enableAutoSessionTracking)
            => SetMetaData($"{SentryPrefix}.auto-session-tracking.enable", enableAutoSessionTracking.ToString());

        internal void SetNdkScopeSync(bool enableNdkScopeSync)
            => SetMetaData($"{SentryPrefix}.ndk.scope-sync.enable", enableNdkScopeSync.ToString());

        internal void SetDebug(bool debug) => SetMetaData($"{SentryPrefix}.debug", debug ? "true" : "false");

        // https://github.com/getsentry/sentry-java/blob/db4dfc92f202b1cefc48d019fdabe24d487db923/sentry/src/main/java/io/sentry/SentryLevel.java#L4-L9
        internal void SetLevel(SentryLevel level) =>
            SetMetaData($"{SentryPrefix}.debug.level", level switch
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
            var attr = CreateAttribute(AndroidNsPrefix, key, AndroidXmlNamespace);
            attr.Value = value;
            return attr;
        }
    }
}
