using UnityEditor;

namespace Sentry.Unity.Editor
{
    internal interface IEditorApplication
    {
        string ApplicationContentsPath { get; }
        EditorApplication.CallbackFunction Update { get; set; }
    }

    internal sealed class EditorApplicationAdapter : IEditorApplication
    {
        public static readonly EditorApplicationAdapter Instance = new();

        public string ApplicationContentsPath => EditorApplication.applicationContentsPath;

        public EditorApplication.CallbackFunction Update
        {
            get => EditorApplication.update;
            set => EditorApplication.update = value;
        }
    }
}
