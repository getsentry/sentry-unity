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

        // Test passed: Exit Code 200 to avoid false positive from a graceful exit unrelated to this test run
        Application.Quit(200);
    }
}
