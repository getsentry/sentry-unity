using System;
using System.Collections;
using System.Runtime.CompilerServices;
using NUnit.Framework;
using Sentry.Unity.Tests.SharedClasses;
using Sentry.Unity.Tests.TestBehaviours;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Sentry.Unity.Tests;

public sealed class IntegrationTests
{
    private TestHttpClientHandler _testHttpClientHandler = null!; // Set in Setup
    private readonly TimeSpan _eventReceiveTimeout = TimeSpan.FromSeconds(1);

    private string _eventMessage = null!; // Set in setup
    private string _identifyingEventValueAttribute = null!; // Set in setup

    [SetUp]
    public void SetUp()
    {
        _testHttpClientHandler = new TestHttpClientHandler("SetupTestHttpClientHandler");
        _eventMessage = Guid.NewGuid() + " Test Event";
        _identifyingEventValueAttribute = CreateAttribute("value", _eventMessage);
    }

    [TearDown]
    public void TearDown()
    {
        if (SentrySdk.IsEnabled)
        {
            SentryUnity.Close();
        }
    }

    [UnityTest]
    public IEnumerator ThrowException_EventContainingMessageGetsCaptured()
    {
        yield return SetupSceneCoroutine("1_BugFarm");

        using var _ = InitSentrySdk();
        var testBehaviour = new GameObject("TestHolder").AddComponent<TestMonoBehaviour>();

        // We don't want to call testBehaviour.TestException(); because it won't go via Sentry infra.
        // We don't have it in tests, but in scenes.
        testBehaviour.gameObject.SendMessage(nameof(testBehaviour.ThrowException), _eventMessage);

        var triggeredEvent = _testHttpClientHandler.GetEvent(_identifyingEventValueAttribute, _eventReceiveTimeout);
        Assert.That(triggeredEvent, Does.Contain(_identifyingEventValueAttribute));
    }

    [UnityTest]
    public IEnumerator ThrowException_EventIncludesApplicationProductNameAtVersionAsRelease()
    {
        yield return SetupSceneCoroutine("1_BugFarm");

        var expectedAttribute = CreateAttribute("release", Application.productName + "@" + Application.version);
        using var _ = InitSentrySdk();
        var testBehaviour = new GameObject("TestHolder").AddComponent<TestMonoBehaviour>();

        testBehaviour.gameObject.SendMessage(nameof(testBehaviour.ThrowException), _eventMessage);

        var triggeredEvent = _testHttpClientHandler.GetEvent(_identifyingEventValueAttribute, _eventReceiveTimeout);
        Assert.That(triggeredEvent, Does.Contain(_identifyingEventValueAttribute)); // sanity check
        Assert.That(triggeredEvent, Does.Contain(expectedAttribute));
    }

    [UnityTest]
    public IEnumerator ThrowException_CustomReleaseSet_EventIncludesCustomRelease()
    {
        yield return SetupSceneCoroutine("1_BugFarm");

        var customRelease = "CustomRelease";
        var expectedAttribute = CreateAttribute("release", customRelease);
        using var _ = InitSentrySdk(o =>
        {
            o.Release = customRelease;
        });
        var testBehaviour = new GameObject("TestHolder").AddComponent<TestMonoBehaviour>();

        testBehaviour.gameObject.SendMessage(nameof(testBehaviour.ThrowException), _eventMessage);

        var triggeredEvent = _testHttpClientHandler.GetEvent(_identifyingEventValueAttribute, _eventReceiveTimeout);
        Assert.That(triggeredEvent, Does.Contain(_identifyingEventValueAttribute)); // sanity check
        Assert.That(triggeredEvent, Does.Contain(expectedAttribute));
    }

    [UnityTest]
    public IEnumerator ThrowException_ProductNameWhitespace_EventIncludesApplicationVersionAsRelease()
    {
        yield return SetupSceneCoroutine("1_BugFarm");

        var originalProductName = PlayerSettings.productName;
        PlayerSettings.productName = " ";
        var expectedAttribute = CreateAttribute("release", Application.version);
        using var _ = InitSentrySdk();
        var testBehaviour = new GameObject("TestHolder").AddComponent<TestMonoBehaviour>();

        testBehaviour.gameObject.SendMessage(nameof(testBehaviour.ThrowException), _eventMessage);

        var triggeredEvent = _testHttpClientHandler.GetEvent(_identifyingEventValueAttribute, _eventReceiveTimeout);
        Assert.That(triggeredEvent, Does.Contain(_identifyingEventValueAttribute)); // sanity check
        Assert.That(triggeredEvent, Does.Contain(expectedAttribute));

        PlayerSettings.productName = originalProductName;
    }

    [UnityTest]
    public IEnumerator ThrowException_ProductNameEmpty_EventIncludesApplicationVersionAsRelease()
    {
        yield return SetupSceneCoroutine("1_BugFarm");

        var originalProductName = PlayerSettings.productName;
        PlayerSettings.productName = null;
        var expectedAttribute = CreateAttribute("release", Application.version);
        using var _ = InitSentrySdk();
        var testBehaviour = new GameObject("TestHolder").AddComponent<TestMonoBehaviour>();

        testBehaviour.gameObject.SendMessage(nameof(testBehaviour.ThrowException), _eventMessage);

        var triggeredEvent = _testHttpClientHandler.GetEvent(_identifyingEventValueAttribute, _eventReceiveTimeout);
        Assert.That(triggeredEvent, Does.Contain(_identifyingEventValueAttribute)); // sanity check
        Assert.That(triggeredEvent, Does.Contain(expectedAttribute));

        PlayerSettings.productName = originalProductName;
    }

    [UnityTest]
    public IEnumerator ThrowException_EditorTest_EventIncludesEditorAsEnvironment()
    {
        yield return SetupSceneCoroutine("1_BugFarm");

        var expectedAttribute = CreateAttribute("environment", "editor");
        using var _ = InitSentrySdk();
        var testBehaviour = new GameObject("TestHolder").AddComponent<TestMonoBehaviour>();

        testBehaviour.gameObject.SendMessage(nameof(testBehaviour.ThrowException), _eventMessage);

        var triggeredEvent = _testHttpClientHandler.GetEvent(_identifyingEventValueAttribute, _eventReceiveTimeout);
        Assert.That(triggeredEvent, Does.Contain(_identifyingEventValueAttribute)); // sanity check
        Assert.That(triggeredEvent, Does.Contain(expectedAttribute));
    }

    [UnityTest]
    public IEnumerator ThrowException_SendDefaultPiiIsTrue_EventIncludesEnvironmentUserNameAsUserName()
    {
        yield return SetupSceneCoroutine("1_BugFarm");

        var expectedAttribute = CreateAttribute("username", Environment.UserName);
        using var _ = InitSentrySdk(o =>
        {
            o.SendDefaultPii = true;
            o.IsEnvironmentUser = true;
        });
        var testBehaviour = new GameObject("TestHolder").AddComponent<TestMonoBehaviour>();

        testBehaviour.gameObject.SendMessage(nameof(testBehaviour.ThrowException), _eventMessage);

        var triggeredEvent = _testHttpClientHandler.GetEvent(_identifyingEventValueAttribute, _eventReceiveTimeout);
        Assert.That(triggeredEvent, Does.Contain(_identifyingEventValueAttribute)); // sanity check
        Assert.That(triggeredEvent, Does.Contain(expectedAttribute));
    }

    [UnityTest]
    public IEnumerator ThrowException_SendDefaultPiiIsFalse_EventDoesNotIncludeEnvironmentUserNameAsUserName()
    {
        yield return SetupSceneCoroutine("1_BugFarm");

        var expectedAttribute = CreateAttribute("username", Environment.UserName);
        using var _ = InitSentrySdk();
        var testBehaviour = new GameObject("TestHolder").AddComponent<TestMonoBehaviour>();

        testBehaviour.gameObject.SendMessage(nameof(testBehaviour.ThrowException), _eventMessage);

        var triggeredEvent = _testHttpClientHandler.GetEvent(_identifyingEventValueAttribute, _eventReceiveTimeout);
        Assert.That(triggeredEvent, Does.Contain(_identifyingEventValueAttribute)); // sanity check
        Assert.That(triggeredEvent, Does.Not.Contain(expectedAttribute));
    }

    [UnityTest]
    public IEnumerator BugFarmScene_MultipleSentryInit_SendEventForTheLatest()
    {
        yield return SetupSceneCoroutine("1_BugFarm");

        var firstHttpClientHandler = new TestHttpClientHandler("NotSupposedToBeCalled_TestHttpClientHandler");
        using var firstDisposable = InitSentrySdk(o =>
        {
            o.Dsn = "http://publickey@localhost:8000/12345";
            o.CreateHttpMessageHandler = () => firstHttpClientHandler;
        });

        using var secondDisposable = InitSentrySdk(); // uses the default test DSN

        var testBehaviour = new GameObject("TestHolder").AddComponent<TestMonoBehaviour>();
        testBehaviour.gameObject.SendMessage(nameof(testBehaviour.ThrowException), _eventMessage);

        // Sanity check
        Assert.AreEqual(string.Empty, firstHttpClientHandler.GetEvent(_identifyingEventValueAttribute, _eventReceiveTimeout));

        Assert.AreNotEqual(string.Empty, _testHttpClientHandler.GetEvent(_identifyingEventValueAttribute, _eventReceiveTimeout));
    }

    [UnityTest]
    public IEnumerator DebugLogException_IsMarkedUnhandled()
    {
        yield return SetupSceneCoroutine("1_BugFarm");

        var expectedMechanism = "\"mechanism\":{\"type\":\"Unity.LogException\",\"handled\":false}}]}";
        using var _ = InitSentrySdk();
        var testBehaviour = new GameObject("TestHolder").AddComponent<TestMonoBehaviour>();

        testBehaviour.gameObject.SendMessage(nameof(testBehaviour.DebugLogException), _eventMessage);

        var triggeredEvent = _testHttpClientHandler.GetEvent(_identifyingEventValueAttribute, _eventReceiveTimeout);
        Assert.That(triggeredEvent, Does.Contain(_identifyingEventValueAttribute)); // sanity check
        Assert.That(triggeredEvent, Does.Contain(expectedMechanism));
    }

    [UnityTest]
    public IEnumerator DebugLogError_OnMainThread_IsCapturedAndIsMainThreadIsTrue()
    {
        Assert.Inconclusive("Flaky"); // Ignoring because of flakiness.

        yield return SetupSceneCoroutine("1_BugFarm");

        _identifyingEventValueAttribute = CreateAttribute("message", _eventMessage); // DebugLogError gets captured as a message
        var expectedAttribute = CreateAttribute("unity.is_main_thread", "true");

        using var _ = InitSentrySdk();
        var testBehaviour = new GameObject("TestHolder").AddComponent<TestMonoBehaviour>();

        testBehaviour.gameObject.SendMessage(nameof(testBehaviour.DebugLogError), _eventMessage);

        var triggeredEvent = _testHttpClientHandler.GetEvent(_identifyingEventValueAttribute, _eventReceiveTimeout);
        Assert.That(triggeredEvent, Does.Contain(_identifyingEventValueAttribute));
        Assert.That(triggeredEvent, Does.Contain(expectedAttribute));
    }

    [UnityTest]
    public IEnumerator DebugLogError_InTask_IsCapturedAndIsMainThreadIsFalse()
    {
        Assert.Inconclusive("Flaky"); // Ignoring because of flakiness.

        yield return SetupSceneCoroutine("1_BugFarm");

        _identifyingEventValueAttribute = CreateAttribute("message", _eventMessage); // DebugLogError gets captured as a message
        var expectedAttribute = CreateAttribute("unity.is_main_thread", "false");

        using var _ = InitSentrySdk();
        var testBehaviour = new GameObject("TestHolder").AddComponent<TestMonoBehaviour>();

        testBehaviour.gameObject.SendMessage(nameof(testBehaviour.DebugLogErrorInTask), _eventMessage);

        var triggeredEvent = _testHttpClientHandler.GetEvent(_identifyingEventValueAttribute, _eventReceiveTimeout);
        Assert.That(triggeredEvent, Does.Contain(_identifyingEventValueAttribute));
        Assert.That(triggeredEvent, Does.Contain(expectedAttribute));
    }

    [UnityTest]
    public IEnumerator DebugLogException_OnMainThread_IsCapturedAndIsMainThreadIsTrue()
    {
        yield return SetupSceneCoroutine("1_BugFarm");

        var expectedAttribute = CreateAttribute("unity.is_main_thread", "true");

        using var _ = InitSentrySdk();
        var testBehaviour = new GameObject("TestHolder").AddComponent<TestMonoBehaviour>();

        testBehaviour.gameObject.SendMessage(nameof(testBehaviour.DebugLogException), _eventMessage);

        var triggeredEvent = _testHttpClientHandler.GetEvent(_identifyingEventValueAttribute, _eventReceiveTimeout);
        Assert.That(triggeredEvent, Does.Contain(_identifyingEventValueAttribute));
        Assert.That(triggeredEvent, Does.Contain(expectedAttribute));
    }

    [UnityTest]
    public IEnumerator DebugLogException_InTask_IsCapturedAndIsMainThreadIsFalse()
    {
        if (TestEnvironment.IsGitHubActions)
        {
            Assert.Inconclusive("Flaky"); // Ignoring because of flakiness
        }

        yield return SetupSceneCoroutine("1_BugFarm");

        var expectedAttribute = CreateAttribute("unity.is_main_thread", "false");

        using var _ = InitSentrySdk();
        var testBehaviour = new GameObject("TestHolder").AddComponent<TestMonoBehaviour>();

        testBehaviour.gameObject.SendMessage(nameof(testBehaviour.DebugLogExceptionInTask), _eventMessage);

        var triggeredEvent = _testHttpClientHandler.GetEvent(_identifyingEventValueAttribute, _eventReceiveTimeout);
        Assert.That(triggeredEvent, Does.Contain(_identifyingEventValueAttribute));
        Assert.That(triggeredEvent, Does.Contain(expectedAttribute));
    }

    [UnityTest]
    public IEnumerator Init_OptionsAreDefaulted()
    {
        yield return null;

        var expectedOptions = new SentryUnityOptions
        {
            Dsn = string.Empty // The SentrySDK tries to resolve the DSN from the environment when it's null
        };

        SentryUnityOptions? actualOptions = null;
        using var _ = InitSentrySdk(o =>
        {
            o.Dsn = string.Empty; // InitSentrySDK already sets a test dsn
            actualOptions = o;
        });

        Assert.NotNull(actualOptions);
        ScriptableSentryUnityOptionsTests.AssertOptions(expectedOptions, actualOptions!);
    }

    private static string CreateAttribute(string name, string value) => $"\"{name}\":\"{value}\"";

    internal static IEnumerator SetupSceneCoroutine(string sceneName, [CallerMemberName] string callerName = "")
    {
        Debug.Log($"=== Running: '{callerName}' ===\n");

        // don't fail test if exception is thrown via 'SendMessage', we want to continue
        LogAssert.ignoreFailingMessages = true;

        // load scene with initialized Sentry, SceneManager.LoadSceneAsync(sceneName);
        SceneManager.LoadScene(sceneName);

        // skip a frame for a Unity to properly load a scene
        yield return null;
    }

    internal IDisposable InitSentrySdk(Action<SentryUnityOptions>? configure = null)
    {
        SentryUnity.Init(options =>
        {
            options.Dsn = "https://e9ee299dbf554dfd930bc5f3c90d5d4b@o447951.ingest.sentry.io/4504604988538880";
            options.CreateHttpMessageHandler = () => _testHttpClientHandler;

            configure?.Invoke(options);
        });

        return new SentryDisposable();
    }

    private sealed class SentryDisposable : IDisposable
    {
        public void Dispose() => SentrySdk.Close();
    }
}
