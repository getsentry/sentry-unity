using UnityEditor;

namespace Sentry.Unity.Editor;

[CustomEditor(typeof(SentryCliOptions))]
public class SentryCliOptionsEditor : UnityEditor.Editor
{
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

        EditorGUILayout.LabelField("Options Configuration", EditorStyles.boldLabel);
        EditorGUILayout.ObjectField("Runtime Configuration", cliOptions.CliOptionsConfiguration,
            typeof(SentryCliOptionsConfiguration), false);

        EditorGUI.EndDisabledGroup();
    }
}
