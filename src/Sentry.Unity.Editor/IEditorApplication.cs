using UnityEditor;

namespace Sentry.Unity.Editor
{
    internal interface IEditorApplication
    {
        string ApplicationContentsPath { get; }
    }

    internal sealed class EditorApplicationAdapter : IEditorApplication
    {
        public static readonly EditorApplicationAdapter Instance = new();

        public string ApplicationContentsPath => EditorApplication.applicationContentsPath;
    }
}
