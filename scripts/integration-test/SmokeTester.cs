using System;
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

        // Test passed: Exit Code 200 to avoid false positive from a graceful exit unrelated to this test run
        // Create a file to write to.
        using (StreamWriter sw = File.CreateText("test.txt"))
        {
            sw.WriteLine("200");
        }

        // Quit does not work on MacOS so we flag with a new file
        Application.Quit(200);
    }
}
