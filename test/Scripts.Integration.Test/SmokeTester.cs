using System.IO;
using UnityEngine;

public class SmokeTester : MonoBehaviour
{
    public void Start() => PlayerTest();

    public static void PlayerTest()
    {
        Debug.Log("PLAYER RUN");
        Application.Quit(200);
    }
}
