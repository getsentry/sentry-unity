using System.IO;
using System.Text;
using System.Xml;
using UnityEditor.Android;
using UnityEngine;

namespace Sentry.Unity.Editor.Android
{
    public class AndroidManifestConfiguration : IPostGenerateGradleAndroidProject
    {
        public void OnPostGenerateGradleAndroidProject(string basePath)
        {
            Debug.Log("AndroidManifestConfiguration.OnPostGenerateGradleAndroidProject at path " + basePath);

            var manifestPath = GetManifestPath(basePath);
            if (File.Exists(manifestPath))
            {
                //throw new InvalidOperationException($"Manifest not found at: {manifestPath}.");
            }
            var androidManifest = new AndroidManifest(manifestPath);

            // TODO: Just opt out from auto init
            // Disable SDK to be enabled via C# with options
            androidManifest.SetDsn("https://94677106febe46b88b9b9ae5efd18a00@o447951.ingest.sentry.io/5439417");

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

            var debugElement = _applicationElement.OwnerDocument.CreateElement("meta-data");
            _ = debugElement.Attributes.Append(CreateAndroidAttribute("name", "io.sentry.debug"));
            _ = debugElement.Attributes.Append(CreateAndroidAttribute("value", "true"));

            _ = _applicationElement.AppendChild(debugElement);
        }
    }
}
