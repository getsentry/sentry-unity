/*
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Xml;
using UnityEditor;
using UnityEditor.Android;
using UnityEngine;

namespace Sentry.Unity.Editor
{
    // https://github.com/getsentry/sentry-java/blob/db4dfc92f202b1cefc48d019fdabe24d487db923/sentry-android-core/src/main/java/io/sentry/android/core/ManifestMetadataReader.java#L66-L187
    public class AndroidManifestConfiguration : IPostGenerateGradleAndroidProject
    {
        private const string SentryOptionsAssetPath = "Assets/Resources/Sentry/SentryOptions.asset";

        public void OnPostGenerateGradleAndroidProject(string basePath)
        {
            if (!ShouldInit(basePath, out var result))
            {
                return;
            }

            var (androidManifest, options) = result.Value;

            options.Logger?.Log(SentryLevel.Debug,
                "Configuring Sentry Android SDK on build.gradle at: {0}", args: basePath);

            var gradleBuildPath = Path.Combine(basePath, "build.gradle");
            var gradleConfigContent = File.ReadAllText(gradleBuildPath);
            // TODO: Have a opt-out to installing the SDK here
            if (!gradleConfigContent.Contains("'io.sentry:sentry-android:"))
            {
                // TODO: Fragile, regex something like: ^dependencies +\{(\n|$){
                var deps = "dependencies {";
                var lastIndex = gradleConfigContent.LastIndexOf(deps, StringComparison.Ordinal);
                if (lastIndex > 0)
                {
                    gradleConfigContent = gradleConfigContent.Insert(lastIndex + deps.Length,
                        // TODO: Configurable version
                        $"{Environment.NewLine}    implementation 'io.sentry:sentry-android:4.0.0-alpha.1'{Environment.NewLine}");
                    File.WriteAllText(gradleBuildPath, gradleConfigContent);
                }
                options.Logger?.Log(SentryLevel.Debug,
                    "Sentry Android SDK already in the gradle build file.");
            }
            else
            {
                options.Logger?.Log(SentryLevel.Debug,
                    "Sentry Android SDK already in the gradle build file.");
            }

            options.Logger?.Log(SentryLevel.Debug,
                "Configuring Sentry options on AndroidManifest: {0}", args: basePath);

            options.Logger?.Log(SentryLevel.Debug, "Setting DSN: {0}", args: options.Dsn);

            if (options.Dsn is not null)
            {
                androidManifest.SetDsn(options.Dsn);
            }

            // Since logcat is only an editor thing, disregarding options.DebugOnlyInEditor
            options.Logger?.Log(SentryLevel.Debug, "Setting Debug: {0}", args: options.Debug);
            androidManifest.SetDebug(options.Debug);
            options.Logger?.Log(SentryLevel.Debug, "Setting DiagnosticsLevel: {0}", args: options.DiagnosticsLevel);
            androidManifest.SetLevel(options.DiagnosticsLevel);

            // TODO: All SentryOptions and create specific Android options

            _ = androidManifest.Save();
        }

        private static bool ShouldInit(
            string basePath,
            [NotNullWhen(true)]
            out (AndroidManifest androidManifest, UnitySentryOptions options)? result)
        {
            result = null;
            var manifestPath = GetManifestPath(basePath);
            if (!File.Exists(manifestPath))
            {
                Debug.LogError($"Manifest not found at {manifestPath}");
                return false;
            }

            var androidManifest = new AndroidManifest(manifestPath);

            if (!(AssetDatabase.LoadAssetAtPath<UnitySentryOptions>(SentryOptionsAssetPath) is { } options))
            {
                Debug.LogError(
                    "SentryOptions asset not found. Sentry will be disabled! Did you configure it on Component/Sentry?");
                androidManifest.DisableSentryAndSave();
                return false;
            }

            if (!options.Enabled)
            {
                options.Logger?.Log(SentryLevel.Debug, "Sentry Disabled In Options. Disabling sentry-android auto-init.");
                androidManifest.DisableSentryAndSave();
                return false;
            }

            if (string.IsNullOrWhiteSpace(options.Dsn))
            {
                options.Logger?.Log(SentryLevel.Warning, "No Sentry DSN configured. Sentry will be disabled.");
                // Otherwise sentry-android attempts to init and logs out:
                // Unable to get provider io.sentry.android.core.SentryInitProvider: java.lang.IllegalArgumentException: DSN is required. Use empty string to disable SDK.
                androidManifest.DisableSentryAndSave();
                return false;
            }

            result = (androidManifest, options);
            return true;
        }

        public int callbackOrder => 1;

        private static string GetManifestPath(string basePath) =>
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

        internal void DisableSentryAndSave()
        {
            SetMetaData("io.sentry.auto-init", "false");
            _ = Save();
        }

        internal void SetDsn(string dsn)  => SetMetaData("io.sentry.dsn", dsn);

        internal void SetDebug(bool debug) => SetMetaData("io.sentry.debug", debug ? "true" : "false");

        // https://github.com/getsentry/sentry-java/blob/db4dfc92f202b1cefc48d019fdabe24d487db923/sentry/src/main/java/io/sentry/SentryLevel.java#L4-L9
        internal void SetLevel(SentryLevel level) =>
            SetMetaData("io.sentry.debug.level", level switch
            {
                SentryLevel.Debug => "DEBUG",
                SentryLevel.Error => "ERROR",
                SentryLevel.Fatal => "FATAL",
                SentryLevel.Info => "INFO",
                SentryLevel.Warning => "WARNING",
                _ => "DEBUG"
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
*/
