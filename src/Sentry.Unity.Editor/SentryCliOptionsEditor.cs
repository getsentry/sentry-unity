using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Sentry.Unity.Editor
{
    [CustomEditor(typeof(SentryCliOptions))]
    public class SentryCliOptionsEditor : UnityEditor.Editor
    {
        private ReorderableList? _symbolList;

        private void OnEnable()
        {
            _symbolList = new ReorderableList(null, typeof( string ))
            {
                drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Additional Symbol Paths"),
                drawElementCallback = (rect, elementIdx, _, _) =>
                {
                    rect = EditorGUI.PrefixLabel(rect, new GUIContent($"Element {elementIdx}"));

                    var element = _symbolList!.list[elementIdx];
                    var symbolList = EditorGUI.TextField(rect, (string)element);
                    _symbolList.list[elementIdx] = symbolList;
                }
            };
        }

        public override void OnInspectorGUI()
        {
            if (target is not SentryCliOptions cliOptions)
            {
                return;
            }

            EditorGUI.BeginDisabledGroup(true);

            EditorGUILayout.Toggle("Enable Symbols Upload", cliOptions.UploadSymbols);
            EditorGUILayout.Toggle("Enable Dev Symbols Upload", cliOptions.UploadDevelopmentSymbols);
            EditorGUILayout.TextField("Auth-Token", cliOptions.Auth);
            EditorGUILayout.TextField("Org-Slug", cliOptions.Organization);
            EditorGUILayout.TextField("Project Name", cliOptions.Project);

            _symbolList!.list = cliOptions.AdditionalSymbolPaths;
            _symbolList.DoLayoutList();

            EditorGUI.EndDisabledGroup();
        }
    }
}
