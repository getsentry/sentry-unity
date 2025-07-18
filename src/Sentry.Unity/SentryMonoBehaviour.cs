using System;
using System.Collections;
using Sentry.Extensibility;
using Sentry.Internal;
using Sentry.Unity.Integrations;
using UnityEngine;

namespace Sentry.Unity;

internal interface ISentryMonoBehaviour
{
    event Action? ApplicationResuming;
    public void CaptureScreenshotForEvent(SentryUnityOptions options, SentryId eventId);
}

/// <summary>
/// Singleton and DontDestroyOnLoad setup.
/// </summary>
[AddComponentMenu("")] // Hides it from being added as a component in the inspector
public partial class SentryMonoBehaviour : MonoBehaviour, ISentryMonoBehaviour
{
    private static SentryMonoBehaviour? _instance;
    public static SentryMonoBehaviour Instance
    {
        get
        {
            // Unity overrides `==` operator in MonoBehaviours
            if (_instance == null)
            {
                // HideAndDontSave excludes the gameObject from the scene meaning it does not get destroyed on loading/unloading
                var sentryGameObject = new GameObject("SentryMonoBehaviour") { hideFlags = HideFlags.HideAndDontSave };
                _instance = sentryGameObject.AddComponent<SentryMonoBehaviour>();
            }

            return _instance;
        }
    }
}

/// <summary>
/// A MonoBehaviour used to provide access to helper methods used during Performance Auto Instrumentation
/// </summary>
public partial class SentryMonoBehaviour
{
    public void StartAwakeSpan(MonoBehaviour monoBehaviour) =>
        SentrySdk.GetSpan()?.StartChild("awake", $"{monoBehaviour.gameObject.name}.{monoBehaviour.GetType().Name}");

    public void FinishAwakeSpan() => SentrySdk.GetSpan()?.Finish(SpanStatus.Ok);
}

/// <summary>
///  A MonoBehavior used to forward application focus events to subscribers.
/// </summary>
public partial class SentryMonoBehaviour
{
    /// <summary>
    /// Hook to receive an event when the application gains focus.
    /// </summary>
    public event Action? ApplicationResuming;

    /// <summary>
    /// Hook to receive an event when the application loses focus.
    /// </summary>
    public event Action? ApplicationPausing;

    // Keeping internal track of running state because OnApplicationPause and OnApplicationFocus get called during startup and would fire false resume events
    internal bool _isRunning = true;

    private IApplication? _application;
    internal IApplication Application
    {
        get
        {
            _application ??= ApplicationAdapter.Instance;
            return _application;
        }
        set => _application = value;
    }

    /// <summary>
    /// Updates the SDK's internal pause status
    /// </summary>
    public void UpdatePauseStatus(bool paused)
    {
        if (paused && _isRunning)
        {
            _isRunning = false;
            ApplicationPausing?.Invoke();
        }
        else if (!paused && !_isRunning)
        {
            _isRunning = true;
            ApplicationResuming?.Invoke();
        }
    }

    /// <summary>
    /// To receive Pause events.
    /// </summary>
    internal void OnApplicationPause(bool pauseStatus) => UpdatePauseStatus(pauseStatus);

    /// <summary>
    /// To receive Focus events.
    /// </summary>
    /// <param name="hasFocus"></param>
    internal void OnApplicationFocus(bool hasFocus) => UpdatePauseStatus(!hasFocus);

    // The GameObject has to destroy itself since it was created with HideFlags.HideAndDontSave
    private void OnApplicationQuit() => Destroy(gameObject);

    private void Awake()
    {
        // This prevents object from being destroyed when unloading the scene since using HideFlags.HideAndDontSave
        // doesn't guarantee its persistence on all platforms i.e. WebGL
        // (see https://github.com/getsentry/sentry-unity/issues/1678 for more details)
        DontDestroyOnLoad(gameObject);
    }
}

/// <summary>
/// A MonoBehaviour that captures screenshots
/// </summary>
public partial class SentryMonoBehaviour : ISentryMonoBehaviour
{
    private bool _isCapturingScreenshot;
    internal Func<SentryUnityOptions, byte[]> ScreenshotCaptureFunction = SentryScreenshot.Capture;
    internal Action<SentryId, SentryAttachment> AttachmentCaptureFunction = (eventId, attachment) =>
        ((Hub)Sentry.SentrySdk.CurrentHub).CaptureAttachment(eventId, attachment);

    public void CaptureScreenshotForEvent(SentryUnityOptions options, SentryId eventId)
    {
        // Only ever capture one screenshot per frame
        if (!_isCapturingScreenshot)
        {
            _isCapturingScreenshot = true;
            StartCoroutine(CaptureScreenshot(options, eventId));
        }
    }

    private IEnumerator CaptureScreenshot(SentryUnityOptions options, SentryId eventId)
    {
        options.LogDebug("Screenshot capture triggered. Waiting for End of Frame.");

        yield return new WaitForEndOfFrame();

        try
        {
            var screenshotBytes = ScreenshotCaptureFunction(options);
            var attachment = new SentryAttachment(
                    AttachmentType.Default,
                    new ByteAttachmentContent(screenshotBytes),
                    "screenshot.jpg",
                    "image/jpeg");

            options.LogDebug("Screenshot captured for event {0}", eventId);

            AttachmentCaptureFunction(eventId, attachment);
        }
        catch (Exception e)
        {
            options.LogError(e, "Failed to capture screenshot.");
        }
        finally
        {
            _isCapturingScreenshot = false;
        }
    }
}
