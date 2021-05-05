using System;
using System.Collections;
using System.Threading;
using UnityEngine;

namespace Sentry.Unity
{
    public class SentryBehavior : MonoBehaviour
    {
        private int goodFrames;
        private int slowFrames;
        private int frozFrames;

        private float slowThreshold = 0.02f;
        private float frozThreshold = 1.0f;

        private float lastTime;

        private void Start()
        {
            frozThreshold = Time.maximumDeltaTime;
        }

        private void Update()
        {
            MeasureDeltaTime();
            CheckFrame();

            if (Input.GetKeyDown(KeyCode.S))
            {
                var sleepDuration = Time.maximumDeltaTime + 0.1f;
                Debug.Log($"<color=red>=========== Sleep for: {sleepDuration}ms ===========</color>");
                Thread.Sleep((int)(sleepDuration * 1000));
            }
        }

        private void MeasureDeltaTime()
        {
            var measuredDeltaTime = Time.realtimeSinceStartup - lastTime;
            lastTime = Time.realtimeSinceStartup;
            Debug.Log($"Measured delta time: {measuredDeltaTime}");
        }

        private void CheckFrame()
        {
            if (Time.deltaTime >= frozThreshold)
            {
                frozFrames++;
            }
            else if (Time.deltaTime > slowThreshold)
            {
                slowFrames++;
            }
            else
            {
                goodFrames++;
            }

            Debug.Log($"Good: {goodFrames} | Slow: {slowFrames} | Frozen: {frozFrames}");
        }


        // TODO: Flush events. See note on OnApplicationQuit
        //private void OnApplicationPause() =>
        // TODO: Flush events, see note on OnApplicationQuit
        // private void OnApplicationFocus { if (!focusStatus) Flush events! }
    }
}
