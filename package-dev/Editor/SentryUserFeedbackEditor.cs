using UnityEngine;
using UnityEditor;

namespace Sentry.Unity
{
    [CustomEditor(typeof(SentryUserFeedbackMonoBehaviour))]
    public class SentryUserFeedbackEditor : UnityEditor.Editor
    {
        private SerializedProperty _openFeedbackButton;
        private SerializedProperty _feedbackForm;
        private SerializedProperty _sendBugReportButton;
        private SerializedProperty _cancelButton;
        private SerializedProperty _name;
        private SerializedProperty _email;
        private SerializedProperty _message;
        private SerializedProperty _addScreenshot;

        private void OnEnable()
        {
            _openFeedbackButton = serializedObject.FindProperty("_openFeedbackButton");
            _feedbackForm = serializedObject.FindProperty("_feedbackForm");
            _sendBugReportButton = serializedObject.FindProperty("_sendBugReportButton");
            _cancelButton = serializedObject.FindProperty("_cancelButton");
            _name = serializedObject.FindProperty("_name");
            _email = serializedObject.FindProperty("_email");
            _message = serializedObject.FindProperty("_message");
            _addScreenshot = serializedObject.FindProperty("_addScreenshot");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Form", EditorStyles.boldLabel);

            DrawPropertyField(_feedbackForm, "Feedback Form");

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Buttons", EditorStyles.boldLabel);

            DrawPropertyField(_openFeedbackButton, "Open Feedback Button");
            DrawPropertyField(_sendBugReportButton, "Send Bug Report Button");
            DrawPropertyField(_cancelButton, "Cancel Button");

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Input Fields", EditorStyles.boldLabel);

            DrawPropertyField(_name, "Name Input Field");
            DrawPropertyField(_email, "Email Input Field");
            DrawPropertyField(_message, "Message Input Field");
            DrawPropertyField(_addScreenshot, "Add Screenshot Toggle");

            serializedObject.ApplyModifiedProperties();
        }

        private static void DrawPropertyField(SerializedProperty property, string displayName)
        {
            if (property.objectReferenceValue)
            {
                EditorGUILayout.PropertyField(property, new GUIContent(displayName));
            }
            else
            {
                GUI.backgroundColor = Color.red;
                EditorGUILayout.PropertyField(property, new GUIContent(displayName));
                GUI.backgroundColor = Color.white;
            }
        }
    }
}
