using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneButtons : MonoBehaviour
{
    public void LoadBugFarm() => SceneManager.LoadScene("1_BugFarm");
    public void LoadNativeSupport() => SceneManager.LoadScene("2_NativeSupport");
    public void LoadAdditionalSamples() => SceneManager.LoadScene("3_AdditionalSamples");
    public void LoadThreadedSamples() => SceneManager.LoadScene("4_ThreadedSamples");

    public void CloseGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
