using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Events;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Sentry.Unity.Editor
{
    public class ButtonInstrumentation : IProcessSceneWithReport
    {
        public int callbackOrder { get; } = 0;

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            var gameObjects = scene.GetRootGameObjects();
            if (gameObjects is { Length: > 0 })
            {
                var sentryScene = gameObjects[0].AddComponent<SentryScene>();

                foreach (var gameObject in gameObjects)
                {
                    var buttons = gameObject.GetComponentsInChildren<Button>();
                    foreach (var button in buttons)
                    {
                        var unityAction = new UnityAction<string>(sentryScene.AddButtonBreadcrumb);
                        UnityEventTools.AddStringPersistentListener(button.onClick, unityAction, button.gameObject.name);
                    }
                }
            }
        }
    }
}
