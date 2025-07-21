using UnityEngine;
using UnityEditor;

namespace Sentry.Unity
{
    [CustomEditor(typeof(SentryUserFeedback))]
    public class SentryUserFeedbackEditor : UnityEditor.Editor
    {
        private SerializedProperty _openFeedbackButton;
        private SerializedProperty _feedbackForm;
        private SerializedProperty _sendFeedbackButton;
        private SerializedProperty _name;
        private SerializedProperty _email;
        private SerializedProperty _description;
        private SerializedProperty _addScreenshot;

        private void OnEnable()
        {
            _feedbackForm = serializedObject.FindProperty("_feedbackForm");

            _openFeedbackButton = serializedObject.FindProperty("_openFeedbackButton");
            _sendFeedbackButton = serializedObject.FindProperty("_sendFeedbackButton");

            _name = serializedObject.FindProperty("_name");
            _email = serializedObject.FindProperty("_email");
            _description = serializedObject.FindProperty("_description");
            _addScreenshot = serializedObject.FindProperty("_addScreenshot");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Form", EditorStyles.boldLabel);

            DrawPropertyField(_feedbackForm, "Feedback Form", true);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Buttons", EditorStyles.boldLabel);

            DrawPropertyField(_openFeedbackButton, "Open Feedback Form Button", true);
            DrawPropertyField(_sendFeedbackButton, "Send Feedback Button", true);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Input Fields", EditorStyles.boldLabel);

            DrawPropertyField(_name, "Name Input Field", false);
            DrawPropertyField(_email, "Email Input Field", false);
            DrawPropertyField(_description, "Description Input Field", true);
            DrawPropertyField(_addScreenshot, "Add Screenshot Toggle", false);

            serializedObject.ApplyModifiedProperties();
        }

        private static void DrawPropertyField(SerializedProperty property, string displayName, bool isRequired)
        {
            if (property.objectReferenceValue)
            {
                EditorGUILayout.PropertyField(property, new GUIContent(displayName));
            }
            else
            {
                GUI.backgroundColor = isRequired ? Color.red : Color.yellow;
                EditorGUILayout.PropertyField(property, new GUIContent(displayName));
                GUI.backgroundColor = Color.white;
            }
        }
    }
}
