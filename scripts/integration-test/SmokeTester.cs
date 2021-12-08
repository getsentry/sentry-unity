using System.IO;
using UnityEngine;

public class SmokeTester : MonoBehaviour
{
    public void Start()
    {
        SmokeTest();
    }

    public static void SmokeTest()
    {
        // On Android we'll grep logcat for this string instead of relying on exit code:
        Debug.Log("SMOKE TEST: PASS");

        // Quit does not work on MacOS so we flag with a new file
        Application.Quit(200);
    }
}
