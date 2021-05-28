using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public void LoadBugFarmScene() => SceneManager.LoadScene("1_BugFarmScene");

    public void LoadTransitionScene() => SceneManager.LoadScene("2_TransitionScene");
}
