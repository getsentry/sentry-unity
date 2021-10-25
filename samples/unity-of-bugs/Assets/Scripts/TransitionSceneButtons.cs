using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TransitionSceneButtons : MonoBehaviour
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void ThrowNull() => throw null;

    public void LoadBugfarm() => SceneManager.LoadScene("1_Bugfarm");

    public void LoadMobileNativeSupport() => SceneManager.LoadScene("2_MobileNativeSupport");
}
