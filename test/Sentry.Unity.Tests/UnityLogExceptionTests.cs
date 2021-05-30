using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Sentry.Protocol;

namespace Sentry.Unity.Tests
{
    public class UnityLogExceptionTests
    {
        [TestCaseSource(nameof(ParsingTestCases))]
        public void ToSentryException_ParsingTestCases(
            string logString,
            string logStackTrace,
            SentryException sentryException)
        {
            var actual = new UnityLogException(logString, logStackTrace).ToSentryException();

            AssertEqual(sentryException, actual);
        }

        private static readonly object[] ParsingTestCases =
        {
            new object[] {
                "NullReferenceException: Object reference not set to an instance of an object",
                @"BugFarm.ThrowNull () (at Assets/Scripts/BugFarm.cs:33)
UnityEngine.Events.InvokableCall.Invoke () (at /Users/bokken/buildslave/unity/build/Runtime/Export/UnityEvent/UnityEvent.cs:166)
UnityEngine.Events.UnityEvent.Invoke () (at /Users/bokken/buildslave/unity/build/Runtime/Export/UnityEvent/UnityEvent/UnityEvent_0.cs:58)
UnityEngine.UI.Button.Press () (at /Applications/Unity/Hub/Editor/2019.4.21f1/Unity.app/Contents/Resources/PackageManager/BuiltInPackages/com.unity.ugui/Runtime/UI/Core/Button.cs:68)
UnityEngine.UI.Button.OnPointerClick (UnityEngine.EventSystems.PointerEventData eventData) (at /Applications/Unity/Hub/Editor/2019.4.21f1/Unity.app/Contents/Resources/PackageManager/BuiltInPackages/com.unity.ugui/Runtime/UI/Core/Button.cs:110)
UnityEngine.EventSystems.ExecuteEvents.Execute (UnityEngine.EventSystems.IPointerClickHandler handler, UnityEngine.EventSystems.BaseEventData eventData) (at /Applications/Unity/Hub/Editor/2019.4.21f1/Unity.app/Contents/Resources/PackageManager/BuiltInPackages/com.unity.ugui/Runtime/EventSystem/ExecuteEvents.cs:50)
UnityEngine.EventSystems.ExecuteEvents.Execute[T] (UnityEngine.GameObject target, UnityEngine.EventSystems.BaseEventData eventData, UnityEngine.EventSystems.ExecuteEvents+EventFunction`1[T1] functor) (at /Applications/Unity/Hub/Editor/2019.4.21f1/Unity.app/Contents/Resources/PackageManager/BuiltInPackages/com.unity.ugui/Runtime/EventSystem/ExecuteEvents.cs:261)
UnityEngine.EventSystems.EventSystem:Update() (at /Applications/Unity/Hub/Editor/2019.4.21f1/Unity.app/Contents/Resources/PackageManager/BuiltInPackages/com.unity.ugui/Runtime/EventSystem/EventSystem.cs:377)",
                new SentryException
                {
                    Value = "Object reference not set to an instance of an object",
                    Type = "NullReferenceException",
                    Stacktrace = new SentryStackTrace
                    {
                        Frames = new List<SentryStackFrame>
                        {
                            new()
                            {
                                Function = "UnityEngine.EventSystems.EventSystem:Update()",
                                AbsolutePath = "/Applications/Unity/Hub/Editor/2019.4.21f1/Unity.app/Contents/Resources/PackageManager/BuiltInPackages/com.unity.ugui/Runtime/EventSystem/EventSystem.cs",
                                LineNumber = 377,
                                FileName = "EventSystem.cs",
                                InApp = false
                            },
                            new()
                            {
                                Function = "UnityEngine.EventSystems.ExecuteEvents.Execute[T] (UnityEngine.GameObject target, UnityEngine.EventSystems.BaseEventData eventData, UnityEngine.EventSystems.ExecuteEvents+EventFunction`1[T1] functor)",
                                AbsolutePath = "/Applications/Unity/Hub/Editor/2019.4.21f1/Unity.app/Contents/Resources/PackageManager/BuiltInPackages/com.unity.ugui/Runtime/EventSystem/ExecuteEvents.cs",
                                LineNumber = 261,
                                FileName = "ExecuteEvents.cs",
                                InApp = false
                            },
                            new()
                            {
                                Function = "UnityEngine.EventSystems.ExecuteEvents.Execute (UnityEngine.EventSystems.IPointerClickHandler handler, UnityEngine.EventSystems.BaseEventData eventData)",
                                AbsolutePath = "/Applications/Unity/Hub/Editor/2019.4.21f1/Unity.app/Contents/Resources/PackageManager/BuiltInPackages/com.unity.ugui/Runtime/EventSystem/ExecuteEvents.cs",
                                LineNumber = 50,
                                FileName = "ExecuteEvents.cs",
                                InApp = false
                            },
                            new()
                            {
                                Function = "UnityEngine.UI.Button.OnPointerClick (UnityEngine.EventSystems.PointerEventData eventData)",
                                AbsolutePath = "/Applications/Unity/Hub/Editor/2019.4.21f1/Unity.app/Contents/Resources/PackageManager/BuiltInPackages/com.unity.ugui/Runtime/UI/Core/Button.cs",
                                LineNumber = 110,
                                FileName = "Button.cs",
                                InApp = false
                            },
                            new()
                            {
                                Function = "UnityEngine.UI.Button.Press ()",
                                AbsolutePath = "/Applications/Unity/Hub/Editor/2019.4.21f1/Unity.app/Contents/Resources/PackageManager/BuiltInPackages/com.unity.ugui/Runtime/UI/Core/Button.cs",
                                LineNumber = 68,
                                FileName = "Button.cs",
                                InApp = false
                            },
                            new()
                            {
                                Function = "UnityEngine.Events.UnityEvent.Invoke ()",
                                AbsolutePath = "/Users/bokken/buildslave/unity/build/Runtime/Export/UnityEvent/UnityEvent/UnityEvent_0.cs",
                                LineNumber = 58,
                                FileName = "UnityEvent_0.cs",
                                InApp = false
                            },
                            new()
                            {
                                Function = "UnityEngine.Events.InvokableCall.Invoke ()",
                                AbsolutePath = "/Users/bokken/buildslave/unity/build/Runtime/Export/UnityEvent/UnityEvent.cs",
                                LineNumber = 166,
                                FileName = "UnityEvent.cs",
                                InApp = false
                            },
                            new()
                            {
                                Function = "BugFarm.ThrowNull ()",
                                AbsolutePath = "Assets/Scripts/BugFarm.cs",
                                LineNumber = 33,
                                FileName = "BugFarm.cs",
                                InApp = true
                            },
                        }
                    }
                } },
            new object[] {
                @"AssertionException: Assertion failure. Values are not equal.
Expected: False == True",
                @"UnityEngine.Assertions.Assert.Fail (System.String message, System.String userMessage) (at /Users/bokken/buildslave/unity/build/Runtime/Export/Assertions/Assert/AssertBase.cs:29)
UnityEngine.Assertions.Assert.AreEqual[T] (T expected, T actual, System.String message, System.Collections.Generic.IEqualityComparer`1[T] comparer) (at /Users/bokken/buildslave/unity/build/Runtime/Export/Assertions/Assert/AssertGeneric.cs:31)
UnityEngine.Assertions.Assert.AreEqual[T] (T expected, T actual, System.String message) (at /Users/bokken/buildslave/unity/build/Runtime/Export/Assertions/Assert/AssertGeneric.cs:19)
UnityEngine.Assertions.Assert.AreEqual[T] (T expected, T actual) (at /Users/bokken/buildslave/unity/build/Runtime/Export/Assertions/Assert/AssertGeneric.cs:13)
BugFarm.AssertFalse () (at Assets/Scripts/BugFarm.cs:28)
UnityEngine.Events.InvokableCall.Invoke () (at /Users/bokken/buildslave/unity/build/Runtime/Export/UnityEvent/UnityEvent.cs:166)
UnityEngine.Events.UnityEvent.Invoke () (at /Users/bokken/buildslave/unity/build/Runtime/Export/UnityEvent/UnityEvent/UnityEvent_0.cs:58)
UnityEngine.UI.Button.Press () (at /Applications/Unity/Hub/Editor/2019.4.21f1/Unity.app/Contents/Resources/PackageManager/BuiltInPackages/com.unity.ugui/Runtime/UI/Core/Button.cs:68)
UnityEngine.UI.Button.OnPointerClick (UnityEngine.EventSystems.PointerEventData eventData) (at /Applications/Unity/Hub/Editor/2019.4.21f1/Unity.app/Contents/Resources/PackageManager/BuiltInPackages/com.unity.ugui/Runtime/UI/Core/Button.cs:110)
UnityEngine.EventSystems.ExecuteEvents.Execute (UnityEngine.EventSystems.IPointerClickHandler handler, UnityEngine.EventSystems.BaseEventData eventData) (at /Applications/Unity/Hub/Editor/2019.4.21f1/Unity.app/Contents/Resources/PackageManager/BuiltInPackages/com.unity.ugui/Runtime/EventSystem/ExecuteEvents.cs:50)
UnityEngine.EventSystems.ExecuteEvents.Execute[T] (UnityEngine.GameObject target, UnityEngine.EventSystems.BaseEventData eventData, UnityEngine.EventSystems.ExecuteEvents+EventFunction`1[T1] functor) (at /Applications/Unity/Hub/Editor/2019.4.21f1/Unity.app/Contents/Resources/PackageManager/BuiltInPackages/com.unity.ugui/Runtime/EventSystem/ExecuteEvents.cs:261)
UnityEngine.EventSystems.EventSystem:Update() (at /Applications/Unity/Hub/Editor/2019.4.21f1/Unity.app/Contents/Resources/PackageManager/BuiltInPackages/com.unity.ugui/Runtime/EventSystem/EventSystem.cs:377)",
                new SentryException
                {
                    Value = @"Assertion failure. Values are not equal.
Expected: False == True",
                    Type = "AssertionException",
                    Stacktrace = new SentryStackTrace
                    {
                        Frames = new List<SentryStackFrame>
                        {
                            new()
                            {
                                Function = "UnityEngine.EventSystems.EventSystem:Update()",
                                AbsolutePath = "/Applications/Unity/Hub/Editor/2019.4.21f1/Unity.app/Contents/Resources/PackageManager/BuiltInPackages/com.unity.ugui/Runtime/EventSystem/EventSystem.cs",
                                LineNumber = 377,
                                FileName = "EventSystem.cs",
                                InApp = false
                            },
                            new()
                            {
                                Function = "UnityEngine.EventSystems.ExecuteEvents.Execute[T] (UnityEngine.GameObject target, UnityEngine.EventSystems.BaseEventData eventData, UnityEngine.EventSystems.ExecuteEvents+EventFunction`1[T1] functor)",
                                AbsolutePath = "/Applications/Unity/Hub/Editor/2019.4.21f1/Unity.app/Contents/Resources/PackageManager/BuiltInPackages/com.unity.ugui/Runtime/EventSystem/ExecuteEvents.cs",
                                LineNumber = 261,
                                FileName = "ExecuteEvents.cs",
                                InApp = false
                            },
                            new()
                            {
                                Function = "UnityEngine.EventSystems.ExecuteEvents.Execute (UnityEngine.EventSystems.IPointerClickHandler handler, UnityEngine.EventSystems.BaseEventData eventData)",
                                AbsolutePath = "/Applications/Unity/Hub/Editor/2019.4.21f1/Unity.app/Contents/Resources/PackageManager/BuiltInPackages/com.unity.ugui/Runtime/EventSystem/ExecuteEvents.cs",
                                LineNumber = 50,
                                FileName = "ExecuteEvents.cs",
                                InApp = false
                            },
                            new()
                            {
                                Function = "UnityEngine.UI.Button.OnPointerClick (UnityEngine.EventSystems.PointerEventData eventData)",
                                AbsolutePath = "/Applications/Unity/Hub/Editor/2019.4.21f1/Unity.app/Contents/Resources/PackageManager/BuiltInPackages/com.unity.ugui/Runtime/UI/Core/Button.cs",
                                LineNumber = 110,
                                FileName = "Button.cs",
                                InApp = false
                            },
                            new()
                            {
                                Function = "UnityEngine.UI.Button.Press ()",
                                AbsolutePath = "/Applications/Unity/Hub/Editor/2019.4.21f1/Unity.app/Contents/Resources/PackageManager/BuiltInPackages/com.unity.ugui/Runtime/UI/Core/Button.cs",
                                LineNumber = 68,
                                FileName = "Button.cs",
                                InApp = false
                            },
                            new()
                            {
                                Function = "UnityEngine.Events.UnityEvent.Invoke ()",
                                AbsolutePath = "/Users/bokken/buildslave/unity/build/Runtime/Export/UnityEvent/UnityEvent/UnityEvent_0.cs",
                                LineNumber = 58,
                                FileName = "UnityEvent_0.cs",
                                InApp = false
                            },
                            new()
                            {
                                Function = "UnityEngine.Events.InvokableCall.Invoke ()",
                                AbsolutePath = "/Users/bokken/buildslave/unity/build/Runtime/Export/UnityEvent/UnityEvent.cs",
                                LineNumber = 166,
                                FileName = "UnityEvent.cs",
                                InApp = false
                            },
                            new()
                            {
                                Function = "BugFarm.AssertFalse ()",
                                AbsolutePath = "Assets/Scripts/BugFarm.cs",
                                LineNumber = 28,
                                FileName = "BugFarm.cs",
                                InApp = true
                            },
                            new()
                            {
                                Function = "UnityEngine.Assertions.Assert.AreEqual[T] (T expected, T actual)",
                                AbsolutePath = "/Users/bokken/buildslave/unity/build/Runtime/Export/Assertions/Assert/AssertGeneric.cs",
                                LineNumber = 13,
                                FileName = "AssertGeneric.cs",
                                InApp = false
                            },
                            new()
                            {
                                Function = "UnityEngine.Assertions.Assert.AreEqual[T] (T expected, T actual, System.String message)",
                                AbsolutePath = "/Users/bokken/buildslave/unity/build/Runtime/Export/Assertions/Assert/AssertGeneric.cs",
                                LineNumber = 19,
                                FileName = "AssertGeneric.cs",
                                InApp = false
                            },
                            new()
                            {
                                Function = "UnityEngine.Assertions.Assert.AreEqual[T] (T expected, T actual, System.String message, System.Collections.Generic.IEqualityComparer`1[T] comparer)",
                                AbsolutePath = "/Users/bokken/buildslave/unity/build/Runtime/Export/Assertions/Assert/AssertGeneric.cs",
                                LineNumber = 31,
                                FileName = "AssertGeneric.cs",
                                InApp = false
                            },
                            new()
                            {
                                Function = "UnityEngine.Assertions.Assert.Fail (System.String message, System.String userMessage)",
                                AbsolutePath = "/Users/bokken/buildslave/unity/build/Runtime/Export/Assertions/Assert/AssertBase.cs",
                                LineNumber = 29,
                                FileName = "AssertBase.cs",
                                InApp = false
                            },
                        }
                    }
                } },
            // TODO: Current parsing logic isn't able to parse this (editor only stack trace)
//             new object[] {
//                 "🕷️🕷️🕷️ Spider message 🕷️🕷️🕷️🕷️",
//                 @"#0 GetStacktrace(int)
// #1 DebugStringToFile(DebugStringToFileData const&)
// #2 DebugLogHandler_CUSTOM_Internal_Log(LogType, LogOption, ScriptingBackendNativeStringPtrOpaque*, ScriptingBackendNativeObjectPtrOpaque*)
// #3 (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log (UnityEngine.LogType,UnityEngine.LogOption,string,UnityEngine.Object)",
//                 new SentryException
//                 {
//                     Value = "🕷️🕷️🕷️ Spider message 🕷️🕷️🕷️🕷️",
//                     Stacktrace = new SentryStackTrace
//                     {
//                         Frames = new List<SentryStackFrame>
//                         {
//                             new()
//                             {
//                                 Function = "UnityEngine.DebugLogHandler:Internal_Log (UnityEngine.LogType,UnityEngine.LogOption,string,UnityEngine.Object)",
//                                 InApp = false
//                             },
//                             new()
//                             {
//                                 Function = "DebugLogHandler_CUSTOM_Internal_Log(LogType, LogOption, ScriptingBackendNativeStringPtrOpaque*, ScriptingBackendNativeObjectPtrOpaque*)",
//                             },
//                             new()
//                             {
//                                 Function = "DebugStringToFile(DebugStringToFileData const&)",
//                                 InApp = false
//                             },
//                             new()
//                             {
//                                 Function = "GetStacktrace(int)",
//                             },
//                         }
//                     }
//                 } },
        };

        private static void AssertEqual(SentryException expected, SentryException actual)
        {
            Assert.AreEqual(expected.Value, actual.Value);
            Assert.AreEqual(expected.ThreadId, actual.ThreadId);
            Assert.AreEqual(expected.Module, actual.Module);
            Assert.AreEqual(expected.Type, actual.Type);
            if (expected.Stacktrace is not null)
            {
                if (expected.Stacktrace.Frames is not null)
                {
                    Assert.AreEqual(expected.Stacktrace.Frames.Count, actual.Stacktrace!.Frames!.Count);
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
                        Assert.AreEqual(expected.Stacktrace.Frames[i].InstructionOffset, actual.Stacktrace.Frames[i].InstructionOffset);
                        Assert.AreEqual(expected.Stacktrace.Frames[i].LineNumber, actual.Stacktrace.Frames[i].LineNumber);
                        Assert.AreEqual(expected.Stacktrace.Frames[i].PostContext, actual.Stacktrace.Frames[i].PostContext);
                        Assert.AreEqual(expected.Stacktrace.Frames[i].PreContext, actual.Stacktrace.Frames[i].PreContext);
                        Assert.AreEqual(expected.Stacktrace.Frames[i].SymbolAddress, actual.Stacktrace.Frames[i].SymbolAddress);
                    }
                }
                else
                {
                    Assert.Null(actual.Stacktrace!.Frames);
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
                if (expected.Mechanism.Data is not null)
                {
                    Assert.True(expected.Mechanism.Data.Keys.SequenceEqual(actual.Mechanism.Data.Keys));
                    Assert.True(expected.Mechanism.Data.Values.SequenceEqual(actual.Mechanism.Data.Values));
                }
                else
                {
                    Assert.Null(actual.Mechanism.Meta);
                }
                if (expected.Mechanism.Meta is not null)
                {
                    Assert.True(expected.Mechanism.Meta.Keys.SequenceEqual(actual.Mechanism.Meta.Keys));
                    Assert.True(expected.Mechanism.Meta.Values.SequenceEqual(actual.Mechanism.Meta.Values));
                }
                else
                {
                    Assert.Null(actual.Mechanism.Meta);
                }
            }
            else
            {
                Assert.Null(actual.Mechanism);
            }
        }
    }
}
