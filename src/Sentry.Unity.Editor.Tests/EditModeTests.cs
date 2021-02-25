using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.TestTools;

namespace Sentry.Unity.Editor.Tests
{
    public sealed class EditModeTests
    {
        [UnityTest]
        public IEnumerator OptionsReleaseField_LengthExceeds_CreatesError()
        {
            EditorHelpers.SetupScene("BugFarmScene");

            // arrange
            using var window = SentryWindow.OpenSentryWindow();

            var validationErrors = new List<ValidationError>();
            window.OnValidationError += error => validationErrors.Add(error);

            // act
            window.Options.Dsn = "qwerty";
            yield return null; // mandatory, waiting for Unity to properly redraw and update inner states

            // assert
            Assert.AreEqual(1, validationErrors.Count);
        }

        [UnityTest]
        public IEnumerator OptionsDsnField_WrongFormat_CreatesError()
        {
            EditorHelpers.SetupScene("BugFarmScene");

            // arrange
            using var window = SentryWindow.OpenSentryWindow();

            var validationErrors = new List<ValidationError>();
            window.OnValidationError += error => validationErrors.Add(error);

            // act
            window.Options.Dsn = "qwertyuiopa";
            yield return null; // mandatory, waiting for Unity to properly redraw and update inner states

            // assert
            Assert.AreEqual(1, validationErrors.Count);
        }
    }

    internal static class EditorHelpers
    {
        // Similar to
        public static void SetupScene(string sceneName)
        {
            LogAssert.ignoreFailingMessages = true;

            var sceneFilePath = GetSceneFilePath(sceneName);
            Assert.NotNull(sceneFilePath);

            // Editor counterpart of 'SceneManager.LoadScene'
            EditorSceneManager.OpenScene(sceneFilePath, OpenSceneMode.Single);

            // TODO: creates new ScriptableObject so we don't rely on an existing one which is 'SentryOptions'
            SentryWindow.SentryOptionsAssetName = "TestSentryOptions";
        }

        /// <summary>
        /// Finds a scene from editor build settings: File -> Build Settings -> Scenes In Build
        /// </summary>
        private static string? GetSceneFilePath(string sceneName)
        {
            foreach (var scene in EditorBuildSettings.scenes)
            {
                if (scene.path.Contains(sceneName))
                {
                    return scene.path;
                }
            }

            return null;
        }
    }
}
