using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Sentry.Protocol;
using Sentry.Unity.Integrations;

namespace Sentry.Unity.Tests;

public class UnityStackTraceParserTests
{
    [TestCaseSource(nameof(ParsingTestCases))]
    public void Parse_VariousStackTraceFormats_ParsesCorrectly(
        string logStackTrace,
        List<SentryStackFrame> expectedFrames)
    {
        var actual = UnityStackTraceParser.Parse(logStackTrace, new SentryUnityOptions());

        Assert.AreEqual(expectedFrames.Count, actual.Count);
        for (var i = 0; i < expectedFrames.Count; i++)
        {
            AssertFrameEqual(expectedFrames[i], actual[i]);
        }
    }

    private static readonly object[] ParsingTestCases =
    [
        // An example log message + stacktrace from within the Editor
        new object[]
        {
            """
            UnityEngine.DebugLogHandler:LogFormat (UnityEngine.LogType,UnityEngine.Object,string,object[])
            Sentry.Unity.Integrations.UnityLogHandlerIntegration:LogFormat (UnityEngine.LogType,UnityEngine.Object,string,object[]) (at /Users/bitfox/Workspace/sentry-unity/src/Sentry.Unity/Integrations/UnityLogHandlerIntegration.cs:89)
            UnityEngine.Debug:LogError (object)
            BugFarmButtons:LogError () (at Assets/Scripts/BugFarmButtons.cs:85)
            UnityEngine.EventSystems.EventSystem:Update () (at ./Library/PackageCache/com.unity.ugui/Runtime/UGUI/EventSystem/EventSystem.cs:530)
            """,
            new List<SentryStackFrame>
            {
                new()
                {
                    Function = "UnityEngine.DebugLogHandler:LogFormat (UnityEngine.LogType,UnityEngine.Object,string,object[])",
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
                    Function = "UnityEngine.Debug:LogError (object)",
                    AbsolutePath = null,
                    LineNumber = null,
                    FileName = null,
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
                    Function = "UnityEngine.EventSystems.EventSystem:Update ()",
                    AbsolutePath = "./Library/PackageCache/com.unity.ugui/Runtime/UGUI/EventSystem/EventSystem.cs",
                    LineNumber = 530,
                    FileName = "EventSystem.cs",
                    InApp = false
                }
            }
        },
        // An example log message + stacktrace from a IL2CPP release build
        new object[]
        {
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
            new List<SentryStackFrame>
            {
                new()
                {
                    Function = "UnityEngine.DebugLogHandler:Internal_Log(LogType, LogOption, String, Object)",
                    AbsolutePath = null,
                    LineNumber = null,
                    FileName = null,
                    InApp = false
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
                    Function = "BugFarmButtons:StackTraceExampleA()",
                    AbsolutePath = null,
                    LineNumber = null,
                    FileName = null,
                    InApp = true
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
                    Function = "UnityEngine.EventSystems.ExecuteEvents:Execute(GameObject, BaseEventData, EventFunction`1)",
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
                    Function = "UnityEngine.EventSystems.StandaloneInputModule:ProcessMouseEvent(Int32)",
                    AbsolutePath = null,
                    LineNumber = null,
                    FileName = null,
                    InApp = false
                },
                new()
                {
                    Function = "UnityEngine.EventSystems.StandaloneInputModule:Process()",
                    AbsolutePath = null,
                    LineNumber = null,
                    FileName = null,
                    InApp = false
                }
            }
        }
    ];

    private static void AssertFrameEqual(SentryStackFrame expected, SentryStackFrame actual)
    {
        Assert.AreEqual(expected.Function, actual.Function);
        Assert.AreEqual(expected.Module, actual.Module);
        Assert.AreEqual(expected.Package, actual.Package);
        Assert.AreEqual(expected.Platform, actual.Platform);
        Assert.AreEqual(expected.AbsolutePath, actual.AbsolutePath);
        Assert.AreEqual(expected.ColumnNumber, actual.ColumnNumber);
        Assert.AreEqual(expected.FileName, actual.FileName);
        Assert.AreEqual(expected.ImageAddress, actual.ImageAddress);
        Assert.AreEqual(expected.InApp, actual.InApp);
        Assert.AreEqual(expected.InstructionAddress, actual.InstructionAddress);
        Assert.AreEqual(expected.LineNumber, actual.LineNumber);
        Assert.AreEqual(expected.PostContext, actual.PostContext);
        Assert.AreEqual(expected.PreContext, actual.PreContext);
        Assert.AreEqual(expected.SymbolAddress, actual.SymbolAddress);
    }
}
