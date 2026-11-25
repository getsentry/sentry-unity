using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Sentry.Unity.Samples
{
    /// <summary>The scene samples: loading a small extra scene in single, additive and async mode.</summary>
    public static class SceneSamples
    {
        public static readonly SampleCategory Category = new SampleCategory("Scenes",
            new SampleEntry("scenes/load-single", "Load a scene",
                "SceneManager.LoadScene. The load arrives as a transaction.",
                () => LoadExtraScene(LoadSceneMode.Single)),
            new SampleEntry("scenes/load-additive", "Load a scene additively",
                "Opens a scene on top of this one. Close unloads it.",
                () => LoadExtraScene(LoadSceneMode.Additive)),
            new SampleEntry("scenes/load-async", "Load a scene asynchronously",
                "SceneManager.LoadSceneAsync. The load arrives as a transaction.",
                LoadExtraSceneAsync));

        // Each returns once the scene is loaded and the frame after it, when the SDK finishes the scene's transaction.
        private static IEnumerator LoadExtraScene(LoadSceneMode mode)
        {
            ExtraScene.LoadedAdditively = mode == LoadSceneMode.Additive;
            SceneManager.LoadScene(ExtraScene.Name, mode);
            while (!SceneManager.GetSceneByName(ExtraScene.Name).isLoaded)
            {
                yield return null;
            }

            yield return null;
            SampleRunner.Report($"Loaded {ExtraScene.Name} ({mode})");
        }

        private static IEnumerator LoadExtraSceneAsync()
        {
            ExtraScene.LoadedAdditively = false;
            yield return SceneManager.LoadSceneAsync(ExtraScene.Name);
            yield return null;
            SampleRunner.Report($"Loaded {ExtraScene.Name} asynchronously");
        }
    }
}
