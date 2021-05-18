using UnityEngine;
using UnityEngine.SceneManagement;

namespace Assets.Scripts
{
    public sealed class SceneLoaderBehaviour : MonoBehaviour
    {
        public void LoadTransitionScene() => SceneManager.LoadScene("TransitionScene");

        public void LoadBugFarmScene() => SceneManager.LoadScene("BugFarmScene");
    }
}
