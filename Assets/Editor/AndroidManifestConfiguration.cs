using System.IO;
using System.Text;
using System.Xml;
using UnityEditor.Android;

public class AndroidManifestConfiguration : IPostGenerateGradleAndroidProject
{
    public void OnPostGenerateGradleAndroidProject(string basePath)
    {
        var androidManifest = new AndroidManifest(GetManifestPath(basePath));

        androidManifest.SetDsn("https://07c6e51144e642a7910ef095334d3063@o19635.ingest.sentry.io/70811");

        androidManifest.Save();
    }

    public int callbackOrder => 1;

    private string _manifestFilePath;

    private string GetManifestPath(string basePath)
    {
        if (string.IsNullOrEmpty(_manifestFilePath))
        {
            var pathBuilder = new StringBuilder(basePath);
            pathBuilder.Append(Path.DirectorySeparatorChar).Append("src");
            pathBuilder.Append(Path.DirectorySeparatorChar).Append("main");
            pathBuilder.Append(Path.DirectorySeparatorChar).Append("AndroidManifest.xml");
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
            reader.Read();
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
        dsnElement.Attributes.Append(CreateAndroidAttribute("name", "io.sentry.dsn"));
        dsnElement.Attributes.Append(CreateAndroidAttribute("value", dsn));

        _applicationElement.AppendChild(dsnElement);

        var debugElement = _applicationElement.OwnerDocument.CreateElement("meta-data");
        debugElement.Attributes.Append(CreateAndroidAttribute("name", "io.sentry.debug"));
        debugElement.Attributes.Append(CreateAndroidAttribute("value", "true"));

        _applicationElement.AppendChild(debugElement);
    }
}
