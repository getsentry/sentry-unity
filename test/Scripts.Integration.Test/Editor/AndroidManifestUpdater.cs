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
        var replacement = "<application android:usesCleartextTraffic=\"true\"";
        var text = File.ReadAllText(manifestPath);
        if (text.Contains(replacement))
        {
            Debug.Log($"AndroidManifestUpdater: Already contains the configuration, nothing to do.");
        }
        else
        {
            var newText = text.Replace("<application", replacement);
            if (text.Equals(newText))
            {
                Debug.LogError($"AndroidManifestUpdater: Failed to update the <application> tag");
            }
            else
            {
                File.WriteAllText(manifestPath, newText);
            }
        }
    }
}
