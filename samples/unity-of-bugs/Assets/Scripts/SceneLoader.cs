using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public void LoadTransitionScene() => SceneManager.LoadScene("TransitionScene");

    public void LoadBugFarmScene() => SceneManager.LoadScene("BugFarmScene");
}
