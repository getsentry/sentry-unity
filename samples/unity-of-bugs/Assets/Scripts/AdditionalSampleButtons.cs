using System;
using System.Threading;
using Sentry;
using Sentry.Unity;
using Unity.Burst;
using Unity.Jobs;
using UnityEngine;

public class AdditionalSampleButtons : MonoBehaviour
{
    public void SetUser()
    {
        // Setting the user on the scope makes sure the user is set on the context of all future events
        SentrySdk.ConfigureScope(s =>
        {
            s.User = new SentryUser
            {
                Email = "ant@farm.bug",
                Username = "ant",
                Id = "ant-id"
            };
        });
        Debug.Log("User set: ant");
    }

    private class PlayerCharacter
    {
        public string Name { get; set; }
        public int Age { get; set; }
        public string AttackType { get; set; }
    }

    public void CaptureMessageWithContext()
    {
        // The context is freely customizable and allows you to add data specific to your game.
        // The SDKs capture methods provide an optional scope that is only getting applied for that one specific event
        SentrySdk.CaptureMessage("Capturing with player character context.", scope =>
        {
            scope.Contexts["character"] = new PlayerCharacter
            {
                Name = "Mighty Fighter",
                Age = 19,
                AttackType = "melee"
            };
        });
    }

    public void ApplicationNotResponding()
    {
        Debug.Log("Running Thread.Sleep() on the UI thread to trigger an ANR event.");
        Thread.Sleep(6 * 1000); // ANR detection currently defaults to 5 seconds
        Debug.Log("Thread.Sleep() finished.");
    }

    public void Assert() => UnityEngine.Assertions.Assert.IsTrue(false);


    [BurstCompile]
    private struct BuggyBurstJob : IJob
    {
        public void Execute()
        {
            Debug.LogError("Bursting with bugs! 💥");
        }
    }

    public void StartBuggyBurstJob()
    {
        Debug.Log("Starting Burst job filled with bugs! 💥");
        var job = new BuggyBurstJob();
        var handle = job.Schedule();
        handle.Complete();
    }
}
