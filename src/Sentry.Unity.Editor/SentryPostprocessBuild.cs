using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Sentry.Unity.Editor
{
    public class SentryPostprocessBuild : IPostprocessBuildWithReport
    {
        public int callbackOrder { get; }

        public void OnPostprocessBuild(BuildReport report)
        {
            // RemoveLinkXml();
        }

        internal void RemoveLinkXml()
        {
            var linkXmlRootFolder = $"Assets/{SentryPreprocessBuild.LinkXmlFolder}";
            if (AssetDatabase.IsValidFolder(linkXmlRootFolder))
            {
                AssetDatabase.DeleteAsset(linkXmlRootFolder);
            }
        }
    }
}
