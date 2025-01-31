using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Sentry.Protocol;
using Sentry.Unity.Integrations;

namespace Sentry.Unity.Tests;

public class UnityErrorLogExceptionTests
{
    [Test]
    public void ToSentryException_MarkedAsHandled()
    {
        var sentryException = new UnityErrorLogException("", "", new SentryUnityOptions()).ToSentryException();

        Assert.IsTrue(sentryException.Mechanism?.Handled);
    }

    [TestCaseSource(nameof(ParsingTestCases))]
    public void ToSentryException_ParsingTestCases(
        string logString,
        string logStackTrace,
        SentryException sentryException)
    {
        var actual = new UnityErrorLogException(logString, logStackTrace, new SentryUnityOptions()).ToSentryException();

        AssertEqual(sentryException, actual);
    }

    private static readonly object[] ParsingTestCases =
    [
        // An example log message + stacktrace from within the Editor
        new object[]
        {
            "Debug.LogError() called",
            """
            UnityEngine.DebugLogHandler:LogFormat (UnityEngine.LogType,UnityEngine.Object,string,object[])
            Sentry.Unity.Integrations.UnityLogHandlerIntegration:LogFormat (UnityEngine.LogType,UnityEngine.Object,string,object[]) (at /Users/bitfox/Workspace/sentry-unity/src/Sentry.Unity/Integrations/UnityLogHandlerIntegration.cs:89)
            UnityEngine.Debug:LogError (object)
            BugFarmButtons:LogError () (at Assets/Scripts/BugFarmButtons.cs:85)
            UnityEngine.EventSystems.EventSystem:Update () (at ./Library/PackageCache/com.unity.ugui/Runtime/UGUI/EventSystem/EventSystem.cs:530)
            """,
            new SentryException
            {
                Value = "Debug.LogError() called",
                Type = UnityErrorLogException.ExceptionType,
                Stacktrace = new SentryStackTrace
                {
                    Frames = new List<SentryStackFrame>
                    {
                        new()
                        {
                            Function = "UnityEngine.EventSystems.EventSystem:Update ()",
                            AbsolutePath = "./Library/PackageCache/com.unity.ugui/Runtime/UGUI/EventSystem/EventSystem.cs",
                            LineNumber = 530,
                            FileName = "EventSystem.cs",
                            InApp = false
                        },
                        new()
                        {
                            Function = "BugFarmButtons:LogError ()",
                            AbsolutePath = "Assets/Scripts/BugFarmButtons.cs",
                            LineNumber = 85,
                            FileName = "BugFarmButtons.cs",
                            InApp = true
                        },
                        new()
                        {
                            Function = "UnityEngine.Debug:LogError (object)",
                            AbsolutePath = null,
                            LineNumber = null,
                            FileName = null,
                            InApp = false
                        },
                        new()
                        {
                            Function = "Sentry.Unity.Integrations.UnityLogHandlerIntegration:LogFormat (UnityEngine.LogType,UnityEngine.Object,string,object[])",
                            AbsolutePath = "/Users/bitfox/Workspace/sentry-unity/src/Sentry.Unity/Integrations/UnityLogHandlerIntegration.cs",
                            LineNumber = 89,
                            FileName = "UnityLogHandlerIntegration.cs",
                            InApp = false
                        },
                        new()
                        {
                            Function = "UnityEngine.DebugLogHandler:LogFormat (UnityEngine.LogType,UnityEngine.Object,string,object[])",
                            AbsolutePath = null,
                            LineNumber = null,
                            FileName = null,
                            InApp = false
                        }
                    }
                },
                Mechanism = new Mechanism
                {
                    Handled = true,
                    Type = "unity.log"
                }
            }
        },
        // An example log message + stacktrace from a IL2CPP release build
        new object[]
        {
            "LogError from within the StackTraceSample",
            """
            UnityEngine.DebugLogHandler:Internal_Log(LogType, LogOption, String, Object)
            BugFarmButtons:StackTraceExampleB()
            BugFarmButtons:StackTraceExampleA()
            UnityEngine.Events.UnityEvent:Invoke()
            UnityEngine.EventSystems.ExecuteEvents:Execute(GameObject, BaseEventData, EventFunction`1)
            UnityEngine.EventSystems.StandaloneInputModule:ReleaseMouse(PointerEventData, GameObject)
            UnityEngine.EventSystems.StandaloneInputModule:ProcessMouseEvent(Int32)
            UnityEngine.EventSystems.StandaloneInputModule:Process()

            """,
            new SentryException
            {
                Value = "LogError from within the StackTraceSample",
                Type = UnityErrorLogException.ExceptionType,
                Stacktrace = new SentryStackTrace
                {
                    Frames = new List<SentryStackFrame>
                    {
                        new()
                        {
                            Function = "UnityEngine.EventSystems.StandaloneInputModule:Process()",
                            AbsolutePath = null,
                            LineNumber = null,
                            FileName = null,
                            InApp = false
                        },
                        new()
                        {
                            Function = "UnityEngine.EventSystems.StandaloneInputModule:ProcessMouseEvent(Int32)",
                            AbsolutePath = null,
                            LineNumber = null,
                            FileName = null,
                            InApp = false
                        },
                        new()
                        {
                            Function = "UnityEngine.EventSystems.StandaloneInputModule:ReleaseMouse(PointerEventData, GameObject)",
                            AbsolutePath = null,
                            LineNumber = null,
                            FileName = null,
                            InApp = false
                        },
                        new()
                        {
                            Function = "UnityEngine.EventSystems.ExecuteEvents:Execute(GameObject, BaseEventData, EventFunction`1)",
                            AbsolutePath = null,
                            LineNumber = null,
                            FileName = null,
                            InApp = false
                        },
                        new()
                        {
                            Function = "UnityEngine.Events.UnityEvent:Invoke()",
                            AbsolutePath = null,
                            LineNumber = null,
                            FileName = null,
                            InApp = false
                        },
                        new()
                        {
                            Function = "BugFarmButtons:StackTraceExampleA()",
                            AbsolutePath = null,
                            LineNumber = null,
                            FileName = null,
                            InApp = true
                        },
                        new()
                        {
                            Function = "BugFarmButtons:StackTraceExampleB()",
                            AbsolutePath = null,
                            LineNumber = null,
                            FileName = null,
                            InApp = true
                        },
                        new()
                        {
                            Function = "UnityEngine.DebugLogHandler:Internal_Log(LogType, LogOption, String, Object)",
                            AbsolutePath = null,
                            LineNumber = null,
                            FileName = null,
                            InApp = false
                        },
                    }
                },
                Mechanism = new Mechanism
                {
                    Handled = true,
                    Type = "unity.log"
                }
            }
        }
    ];

    private static void AssertEqual(SentryException expected, SentryException actual)
    {
        Assert.AreEqual(expected.Value, actual.Value);
        Assert.AreEqual(expected.ThreadId, actual.ThreadId);
        Assert.AreEqual(expected.Module, actual.Module);
        Assert.AreEqual(expected.Type, actual.Type);
        if (expected.Stacktrace is not null)
        {
            Assert.AreEqual(expected.Stacktrace.Frames.Count, actual.Stacktrace!.Frames.Count);
            for (var i = 0; i < expected.Stacktrace.Frames.Count; i++)
            {
                Assert.AreEqual(expected.Stacktrace.Frames[i].Function, actual.Stacktrace.Frames[i].Function);
                Assert.AreEqual(expected.Stacktrace.Frames[i].Module, actual.Stacktrace.Frames[i].Module);
                Assert.AreEqual(expected.Stacktrace.Frames[i].Package, actual.Stacktrace.Frames[i].Package);
                Assert.AreEqual(expected.Stacktrace.Frames[i].Platform, actual.Stacktrace.Frames[i].Platform);
                Assert.AreEqual(expected.Stacktrace.Frames[i].AbsolutePath, actual.Stacktrace.Frames[i].AbsolutePath);
                Assert.AreEqual(expected.Stacktrace.Frames[i].ColumnNumber, actual.Stacktrace.Frames[i].ColumnNumber);
                Assert.AreEqual(expected.Stacktrace.Frames[i].FileName, actual.Stacktrace.Frames[i].FileName);
                Assert.AreEqual(expected.Stacktrace.Frames[i].ImageAddress, actual.Stacktrace.Frames[i].ImageAddress);
                Assert.AreEqual(expected.Stacktrace.Frames[i].InApp, actual.Stacktrace.Frames[i].InApp);
                Assert.AreEqual(expected.Stacktrace.Frames[i].InstructionAddress, actual.Stacktrace.Frames[i].InstructionAddress);
                Assert.AreEqual(expected.Stacktrace.Frames[i].LineNumber, actual.Stacktrace.Frames[i].LineNumber);
                Assert.AreEqual(expected.Stacktrace.Frames[i].PostContext, actual.Stacktrace.Frames[i].PostContext);
                Assert.AreEqual(expected.Stacktrace.Frames[i].PreContext, actual.Stacktrace.Frames[i].PreContext);
                Assert.AreEqual(expected.Stacktrace.Frames[i].SymbolAddress, actual.Stacktrace.Frames[i].SymbolAddress);
            }
        }
        else
        {
            Assert.Null(actual.Stacktrace);
        }
        if (expected.Mechanism is not null)
        {
            Assert.AreEqual(expected.Mechanism.Description, actual.Mechanism!.Description);
            Assert.AreEqual(expected.Mechanism.Handled, actual.Mechanism.Handled);
            Assert.AreEqual(expected.Mechanism.Type, actual.Mechanism.Type);
            Assert.AreEqual(expected.Mechanism.HelpLink, actual.Mechanism.HelpLink);
            Assert.AreEqual(expected.Mechanism.Data, actual.Mechanism.Data);
            Assert.True(expected.Mechanism.Data.Keys.SequenceEqual(actual.Mechanism.Data.Keys));
            Assert.True(expected.Mechanism.Data.Values.SequenceEqual(actual.Mechanism.Data.Values));
            Assert.True(expected.Mechanism.Meta.Keys.SequenceEqual(actual.Mechanism.Meta.Keys));
            Assert.True(expected.Mechanism.Meta.Values.SequenceEqual(actual.Mechanism.Meta.Values));
        }
        else
        {
            Assert.Null(actual.Mechanism);
        }
    }
}
