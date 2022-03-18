using UnityEditor.Android;
using System.IO;
using Debug = UnityEngine.Debug;

public class AndroidManifestUpdater : IPostGenerateGradleAndroidProject
{
    public int callbackOrder => 2;

    public void OnPostGenerateGradleAndroidProject(string basePath)
    {
        var manifestName = "AndroidManifest.xml";
        var manifestPath = Path.Combine(basePath, "src", "main", manifestName);
        if (!File.Exists(manifestPath))
        {
            throw new FileNotFoundException("AndroidManifest not found in " + manifestPath);
        }

        Debug.Log($"AndroidManifestUpdater: Updating {manifestName}");
        var text = File.ReadAllText(manifestPath);
        var newText = text.Replace("<application", "<application android:usesCleartextTraffic=\"true\"");
        if (text.Equals(newText))
        {
            Debug.LogError($"AndroidManifestUpdater: Failed find the <application> tag");
        }
        else
        {
            File.WriteAllText(manifestPath, newText);
        }
    }
}
