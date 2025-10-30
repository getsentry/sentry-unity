using System.Linq;
using NUnit.Framework;
using Sentry.Unity.Integrations;

namespace Sentry.Unity.Tests;

public class UnityLogEventFactoryTests
{
    private const string SampleMessage = "Debug.LogError() called";
    private const string SampleStackTrace = """
        UnityEngine.DebugLogHandler:LogFormat (UnityEngine.LogType,UnityEngine.Object,string,object[])
        BugFarmButtons:LogError () (at Assets/Scripts/BugFarmButtons.cs:85)
        """;

    [Test]
    public void CreateMessageEvent_ValidStackTrace_CreatesMessageEventWithThreads()
    {
        var evt = UnityLogEventFactory.CreateMessageEvent(
            SampleMessage, SampleStackTrace, SentryLevel.Error, new SentryUnityOptions());

        Assert.NotNull(evt.Message);
        Assert.AreEqual(SampleMessage, evt.Message!.Message);
        Assert.AreEqual(SentryLevel.Error, evt.Level);
        Assert.NotNull(evt.SentryThreads);
        Assert.AreEqual(1, evt.SentryThreads.Count());
    }

    [Test]
    public void CreateMessageEvent_ValidStackTrace_ThreadHasStackTrace()
    {
        var evt = UnityLogEventFactory.CreateMessageEvent(
            SampleMessage, SampleStackTrace, SentryLevel.Error, new SentryUnityOptions());

        var thread = evt.SentryThreads!.First();
        Assert.False(thread.Crashed);
        Assert.True(thread.Current);
        Assert.NotNull(thread.Stacktrace);
        Assert.NotNull(thread.Stacktrace!.Frames);
        Assert.AreEqual(2, thread.Stacktrace.Frames.Count);
    }

    [Test]
    public void CreateMessageEvent_ValidStackTrace_FramesAreReversed()
    {
        var evt = UnityLogEventFactory.CreateMessageEvent(
            SampleMessage, SampleStackTrace, SentryLevel.Error, new SentryUnityOptions());

        var frames = evt.SentryThreads!.First().Stacktrace!.Frames;
        // After reversing, the last frame in the Unity stacktrace should be first
        Assert.AreEqual("BugFarmButtons:LogError ()", frames[0].Function);
        Assert.AreEqual("UnityEngine.DebugLogHandler:LogFormat (UnityEngine.LogType,UnityEngine.Object,string,object[])", frames[1].Function);
    }

    [Test]
    public void CreateExceptionEvent_ValidStackTrace_CreatesExceptionEvent()
    {
        var evt = UnityLogEventFactory.CreateExceptionEvent(
            SampleMessage, SampleStackTrace, new SentryUnityOptions());

        Assert.AreEqual(SentryLevel.Error, evt.Level);
        Assert.NotNull(evt.SentryExceptions);
        Assert.AreEqual(1, evt.SentryExceptions.Count());
    }

    [Test]
    public void CreateExceptionEvent_ValidStackTrace_ExceptionHasExpectedProperties()
    {
        var evt = UnityLogEventFactory.CreateExceptionEvent(
            SampleMessage, SampleStackTrace, new SentryUnityOptions());

        var exception = evt.SentryExceptions!.First();
        Assert.AreEqual(SampleMessage, exception.Value);
        Assert.AreEqual("LogError", exception.Type);
        Assert.NotNull(exception.Mechanism);
        Assert.True(exception.Mechanism!.Handled);
        Assert.AreEqual("unity.log", exception.Mechanism.Type);
    }

    [Test]
    public void CreateExceptionEvent_ValidStackTrace_ExceptionHasStackTrace()
    {
        var evt = UnityLogEventFactory.CreateExceptionEvent(
            SampleMessage, SampleStackTrace, new SentryUnityOptions());

        var exception = evt.SentryExceptions!.First();
        Assert.NotNull(exception.Stacktrace);
        Assert.NotNull(exception.Stacktrace!.Frames);
        Assert.AreEqual(2, exception.Stacktrace.Frames.Count);
    }

    [Test]
    public void CreateExceptionEvent_ValidStackTrace_FramesAreReversed()
    {
        var evt = UnityLogEventFactory.CreateExceptionEvent(
            SampleMessage, SampleStackTrace, new SentryUnityOptions());

        var frames = evt.SentryExceptions!.First().Stacktrace!.Frames;
        // After reversing, the last frame in the Unity stacktrace should be first
        Assert.AreEqual("BugFarmButtons:LogError ()", frames[0].Function);
        Assert.AreEqual("UnityEngine.DebugLogHandler:LogFormat (UnityEngine.LogType,UnityEngine.Object,string,object[])", frames[1].Function);
    }
}
