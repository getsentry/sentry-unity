using System.IO;
using System.Text;
using System.Xml;
using UnityEditor;
using UnityEditor.Android;
using UnityEngine;

namespace Sentry.Unity.Editor.Android
{
    // https://github.com/getsentry/sentry-java/blob/db4dfc92f202b1cefc48d019fdabe24d487db923/sentry-android-core/src/main/java/io/sentry/android/core/ManifestMetadataReader.java#L66-L187
    public class AndroidManifestConfiguration : IPostGenerateGradleAndroidProject
    {
        public void OnPostGenerateGradleAndroidProject(string basePath)
        {
            if (!(AssetDatabase.LoadAssetAtPath<UnitySentryOptions>(SentryWindows.SentryOptionsAssetPath) is UnitySentryOptions options))
            {
                Debug.LogError("Sentry will be disabled!\nSentryOptions asset not found. Did you configure it on Component/Sentry?");
                return;
            }

            options.Logger?.Log(SentryLevel.Debug,
                "AndroidManifestConfiguration.OnPostGenerateGradleAndroidProject at: {0}", args: basePath);

            var manifestPath = GetManifestPath(basePath);
            if (!File.Exists(manifestPath))
            {
                options.Logger?.Log(SentryLevel.Fatal, "Manifest not found at: {0}", args: manifestPath);
                return;
            }
            var androidManifest = new AndroidManifest(manifestPath);

            androidManifest.SetDsn(options.Dsn);

            // Since logcat is only an editor thing, disregarding options.DebugOnlyInEditor
            androidManifest.SetDebug(options.Debug);
            androidManifest.SetLevel(options.DiagnosticsLevel);

            // TODO: All SentryOptions and create specific Android options

            _ = androidManifest.Save();
        }

        public int callbackOrder => 1;

        private string _manifestFilePath;

        private string GetManifestPath(string basePath)
        {
            if (string.IsNullOrEmpty(_manifestFilePath))
            {
                var pathBuilder = new StringBuilder(basePath);
                _ = pathBuilder.Append(Path.DirectorySeparatorChar).Append("src");
                _ = pathBuilder.Append(Path.DirectorySeparatorChar).Append("main");
                _ = pathBuilder.Append(Path.DirectorySeparatorChar).Append("AndroidManifest.xml");
                _manifestFilePath = pathBuilder.ToString();
            }
            return _manifestFilePath;
        }
    }


    internal class AndroidXmlDocument : XmlDocument
    {
        private readonly string _mPath;
        protected readonly XmlNamespaceManager nsMgr;
        public readonly string AndroidXmlNamespace = "http://schemas.android.com/apk/res/android";
        public AndroidXmlDocument(string path)
        {
            _mPath = path;
            using (var reader = new XmlTextReader(_mPath))
            {
                _ = reader.Read();
                Load(reader);
            }
            nsMgr = new XmlNamespaceManager(NameTable);
            nsMgr.AddNamespace("android", AndroidXmlNamespace);
        }

        public string Save() => SaveAs(_mPath);

        public string SaveAs(string path)
        {
            using (var writer = new XmlTextWriter(path, new UTF8Encoding(false)))
            {
                writer.Formatting = Formatting.Indented;
                Save(writer);
            }
            return path;
        }
    }

    internal class AndroidManifest : AndroidXmlDocument
    {
        private readonly XmlElement _applicationElement;

        public AndroidManifest(string path) : base(path)
            => _applicationElement = SelectSingleNode("/manifest/application") as XmlElement;

        private XmlAttribute CreateAndroidAttribute(string key, string value)
        {
            var attr = CreateAttribute("android", key, AndroidXmlNamespace);
            attr.Value = value;
            return attr;
        }

        private XmlNode GetActivityWithLaunchIntent() =>
            SelectSingleNode("/manifest/application/activity[intent-filter/action/@android:name='android.intent.action.MAIN' and " +
                             "intent-filter/category/@android:name='android.intent.category.LAUNCHER']", nsMgr);

        internal void SetDsn(string dsn)
        {
            var dsnElement = _applicationElement.OwnerDocument.CreateElement("meta-data");
            _ = dsnElement.Attributes.Append(CreateAndroidAttribute("name", "io.sentry.dsn"));
            _ = dsnElement.Attributes.Append(CreateAndroidAttribute("value", dsn));

            _ = _applicationElement.AppendChild(dsnElement);
        }

        internal void SetDebug(bool debug)
        {
            var debugElement = _applicationElement.OwnerDocument.CreateElement("meta-data");
            _ = debugElement.Attributes.Append(CreateAndroidAttribute("name", "io.sentry.debug"));
            _ = debugElement.Attributes.Append(CreateAndroidAttribute("value", debug ? "true" : "false"));
            _ = _applicationElement.AppendChild(debugElement);
        }

        internal void SetLevel(SentryLevel level)
        {
            // https://github.com/getsentry/sentry-java/blob/db4dfc92f202b1cefc48d019fdabe24d487db923/sentry/src/main/java/io/sentry/SentryLevel.java#L4-L9
            var javaLevel = level switch
            {
                SentryLevel.Debug => "DEBUG",
                SentryLevel.Error => "ERROR",
                SentryLevel.Fatal => "FATAL",
                SentryLevel.Info => "INFO",
                SentryLevel.Warning => "WARNING",
                _ => null
            };
            if (javaLevel is { } value)
            {
                var debugElement = _applicationElement.OwnerDocument.CreateElement("meta-data");
                _ = debugElement.Attributes.Append(CreateAndroidAttribute("name", "io.sentry.debug.level"));
                _ = debugElement.Attributes.Append(CreateAndroidAttribute("value", value));
                _ = _applicationElement.AppendChild(debugElement);
            }
        }
    }
}
