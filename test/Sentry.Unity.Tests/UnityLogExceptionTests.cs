using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Sentry.Protocol;
using UnityEngine;

namespace Sentry.Unity.Tests
{
    public class UnityLogExceptionTests
    {
        [Test]
        public void ToSentryException_MarkedAsUnhandled()
        {
            var converter = new UnityLogExceptionConverter(new SentryOptions());
            var ule = new UnityLogException();
            var sentryException = converter.ToSentryException(ule);

            Assert.IsFalse(sentryException.Mechanism?.Handled);
        }

        [TestCaseSource(nameof(ParsingTestCases))]
        public void ToSentryException_ParsingTestCases(
            string logString,
            string logStackTrace,
            LogType logType,
            StackTraceLogType stackTraceLogType,
            SentryException sentryException)
        {
            var converter = new UnityLogExceptionConverter(new SentryOptions());
            var ule = new UnityLogException(logString, logStackTrace, logType, stackTraceLogType);
            var actual = converter.ToSentryException(ule);

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
                LogType.Exception,
                StackTraceLogType.ScriptOnly,
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
                    },
                    Mechanism = new Mechanism
                    {
                        Handled = false,
                        Type = "unity.log"
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
Class.Method () (at /Applications/Test/Fake.cs)
UnityEngine.EventSystems.EventSystem:Update() (at /Applications/Unity/Hub/Editor/2019.4.21f1/Unity.app/Contents/Resources/PackageManager/BuiltInPackages/com.unity.ugui/Runtime/EventSystem/EventSystem.cs:377)",
                LogType.Assert,
                StackTraceLogType.ScriptOnly,
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
                                Function = "Class.Method ()",
                                AbsolutePath = "/Applications/Test/Fake.cs",
                                // Missing line number
                                LineNumber = null,
                                FileName = "Fake.cs",
                                InApp = true
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
                    },
                    Mechanism = new Mechanism
                    {
                        Handled = false,
                        Type = "unity.log"
                    }
                } },
            new object[] {
                "some error",
                @" #0 GetStacktrace(int)
 #1 DebugStringToFile(DebugStringToFileData const&)
 #2 DebugLogHandler_CUSTOM_Internal_Log(LogType, LogOption, ScriptingBackendNativeStringPtrOpaque*, ScriptingBackendNativeObjectPtrOpaque*)
 #3  (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log (UnityEngine.LogType,UnityEngine.LogOption,string,UnityEngine.Object)
 #4  (Mono JIT Code) [LightLoop.cs:1612] UnityEngine.Rendering.HighDefinition.HDRenderPipeline:PrepareGPULightdata (UnityEngine.Rendering.CommandBuffer,UnityEngine.Rendering.HighDefinition.HDCamera,UnityEngine.Rendering.CullingResults)
 #5  (Mono JIT Code) [LightLoop.cs:1878] UnityEngine.Rendering.HighDefinition.HDRenderPipeline:PrepareLightsForGPU (UnityEngine.Rendering.CommandBuffer,UnityEngine.Rendering.HighDefinition.HDCamera,UnityEngine.Rendering.CullingResults,UnityEngine.Rendering.HighDefinition.HDProbeCullingResults,UnityEngine.Rendering.HighDefinition.LocalVolumetricFogList,UnityEngine.Rendering.HighDefinition.DebugDisplaySettings,UnityEngine.Rendering.HighDefinition.AOVRequestData)
 #6  (Mono JIT Code) [HDRenderPipeline.cs:2046] UnityEngine.Rendering.HighDefinition.HDRenderPipeline:ExecuteRenderRequest (UnityEngine.Rendering.HighDefinition.HDRenderPipeline/RenderRequest,UnityEngine.Rendering.ScriptableRenderContext,UnityEngine.Rendering.CommandBuffer,UnityEngine.Rendering.HighDefinition.AOVRequestData)
 #7  (Mono JIT Code) [HDRenderPipeline.cs:1856] UnityEngine.Rendering.HighDefinition.HDRenderPipeline:Render (UnityEngine.Rendering.ScriptableRenderContext,System.Collections.Generic.List`1<UnityEngine.Camera>)
 #8  (Mono JIT Code) [RenderPipeline.cs:52] UnityEngine.Rendering.RenderPipeline:InternalRender (UnityEngine.Rendering.ScriptableRenderContext,System.Collections.Generic.List`1<UnityEngine.Camera>)
 #9  (Mono JIT Code) [RenderPipelineManager.cs:115] UnityEngine.Rendering.RenderPipelineManager:DoRenderLoop_Internal (UnityEngine.Rendering.RenderPipelineAsset,intptr,System.Collections.Generic.List`1<UnityEngine.Camera/RenderRequest>,Unity.Collections.LowLevel.Unsafe.AtomicSafetyHandle)
 #10  (Mono JIT Code) (wrapper runtime-invoke) <Module>:runtime_invoke_void_object_intptr_object_AtomicSafetyHandle (object,intptr,intptr,intptr)
 #11 mono_jit_runtime_invoke
 #12 do_runtime_invoke
 #13 mono_runtime_invoke
 #14 scripting_method_invoke(ScriptingMethodPtr, ScriptingObjectPtr, ScriptingArguments&, ScriptingExceptionPtr*, bool)
 #15 ScriptingInvocation::Invoke(ScriptingExceptionPtr*, bool)
 #16 ScriptableRenderContext::ExtractAndExecuteRenderPipeline(dynamic_array<Camera*, 0ul> const&, void (*)(SceneNode const*, AABB const*, IndexList&, SceneCullingParameters const*), void*, ScriptingObjectPtr)
 #17 Camera::ExecuteCustomRenderPipeline(Camera::EditorDrawingMode, DrawGridParameters const*, Camera::RenderFlag)
 #18 Camera::RenderEditorCamera(Camera::EditorDrawingMode, DrawGridParameters const*, CullResults*, Camera::RenderFlag, int, bool, bool, core::hash_set<GameObject*, core::hash<GameObject*>, std::__1::equal_to<GameObject*> >*)
 #19 Camera::RenderEditorCamera(Camera::EditorDrawingMode, DrawGridParameters const*, bool, bool, core::hash_set<GameObject*, core::hash<GameObject*>, std::__1::equal_to<GameObject*> >*)
 #20 Handles_CUSTOM_Internal_DrawCameraWithGrid(ScriptingBackendNativeObjectPtrOpaque*, Camera::EditorDrawingMode, DrawGridParameters&, unsigned char, unsigned char)
 #21  (Mono JIT Code) (wrapper managed-to-native) UnityEditor.Handles:Internal_DrawCameraWithGrid (UnityEngine.Camera,UnityEditor.DrawCameraMode,UnityEditor.DrawGridParameters&,bool,bool)
",
                LogType.Error,
                StackTraceLogType.Full,
                new SentryException
                {
                    Type = "Error",
                    Value = "some error",
                    Stacktrace = new SentryStackTrace
                    {
                        Frames = new List<SentryStackFrame>
                        {
                            new()
                            {
                                Module = "Mono JIT Code",
                                Function = "UnityEditor.Handles:Internal_DrawCameraWithGrid (UnityEngine.Camera,UnityEditor.DrawCameraMode,UnityEditor.DrawGridParameters&,bool,bool)",
                                InApp = false,
                            },
                            new()
                            {
                                Function = "Handles_CUSTOM_Internal_DrawCameraWithGrid(ScriptingBackendNativeObjectPtrOpaque*, Camera::EditorDrawingMode, DrawGridParameters&, unsigned char, unsigned char)",
                            },
                            new()
                            {
                                Function = "Camera::RenderEditorCamera(Camera::EditorDrawingMode, DrawGridParameters const*, bool, bool, core::hash_set<GameObject*, core::hash<GameObject*>, std::__1::equal_to<GameObject*> >*)",
                            },
                            new()
                            {
                                Function = "Camera::RenderEditorCamera(Camera::EditorDrawingMode, DrawGridParameters const*, CullResults*, Camera::RenderFlag, int, bool, bool, core::hash_set<GameObject*, core::hash<GameObject*>, std::__1::equal_to<GameObject*> >*)",
                            },
                            new()
                            {
                                Function = "Camera::ExecuteCustomRenderPipeline(Camera::EditorDrawingMode, DrawGridParameters const*, Camera::RenderFlag)",
                            },
                            new()
                            {
                                Function = "ScriptableRenderContext::ExtractAndExecuteRenderPipeline(dynamic_array<Camera*, 0ul> const&, void (*)(SceneNode const*, AABB const*, IndexList&, SceneCullingParameters const*), void*, ScriptingObjectPtr)",
                            },
                            new()
                            {
                                Function = "ScriptingInvocation::Invoke(ScriptingExceptionPtr*, bool)",
                            },
                            new()
                            {
                                Function = "scripting_method_invoke(ScriptingMethodPtr, ScriptingObjectPtr, ScriptingArguments&, ScriptingExceptionPtr*, bool)",
                            },
                            new()
                            {
                                Function = "mono_runtime_invoke",
                            },
                            new()
                            {
                                Function = "do_runtime_invoke",
                            },
                            new()
                            {
                                Function = "mono_jit_runtime_invoke",
                            },
                            new()
                            {
                                Module = "Mono JIT Code",
                                Function = "<Module>:runtime_invoke_void_object_intptr_object_AtomicSafetyHandle (object,intptr,intptr,intptr)",
                            },
                            new()
                            {
                                Module = "Mono JIT Code",
                                Function = "UnityEngine.Rendering.RenderPipelineManager:DoRenderLoop_Internal (UnityEngine.Rendering.RenderPipelineAsset,intptr,System.Collections.Generic.List`1<UnityEngine.Camera/RenderRequest>,Unity.Collections.LowLevel.Unsafe.AtomicSafetyHandle)",
                                FileName = "RenderPipelineManager.cs",
                                LineNumber = 115,
                                InApp = false,
                            },
                            new()
                            {
                                Module = "Mono JIT Code",
                                Function = "UnityEngine.Rendering.RenderPipeline:InternalRender (UnityEngine.Rendering.ScriptableRenderContext,System.Collections.Generic.List`1<UnityEngine.Camera>)",
                                FileName = "RenderPipeline.cs",
                                LineNumber = 52,
                                InApp = false,
                            },
                            new()
                            {
                                Module = "Mono JIT Code",
                                Function = "UnityEngine.Rendering.HighDefinition.HDRenderPipeline:Render (UnityEngine.Rendering.ScriptableRenderContext,System.Collections.Generic.List`1<UnityEngine.Camera>)",
                                FileName = "HDRenderPipeline.cs",
                                LineNumber = 1856,
                                InApp = false,
                            },
                            new()
                            {
                                Module = "Mono JIT Code",
                                Function = "UnityEngine.Rendering.HighDefinition.HDRenderPipeline:ExecuteRenderRequest (UnityEngine.Rendering.HighDefinition.HDRenderPipeline/RenderRequest,UnityEngine.Rendering.ScriptableRenderContext,UnityEngine.Rendering.CommandBuffer,UnityEngine.Rendering.HighDefinition.AOVRequestData)",
                                FileName = "HDRenderPipeline.cs",
                                LineNumber = 2046,
                                InApp = false,
                            },
                            new()
                            {
                                Module = "Mono JIT Code",
                                Function = "UnityEngine.Rendering.HighDefinition.HDRenderPipeline:PrepareLightsForGPU (UnityEngine.Rendering.CommandBuffer,UnityEngine.Rendering.HighDefinition.HDCamera,UnityEngine.Rendering.CullingResults,UnityEngine.Rendering.HighDefinition.HDProbeCullingResults,UnityEngine.Rendering.HighDefinition.LocalVolumetricFogList,UnityEngine.Rendering.HighDefinition.DebugDisplaySettings,UnityEngine.Rendering.HighDefinition.AOVRequestData)",
                                FileName = "LightLoop.cs",
                                LineNumber = 1878,
                                InApp = false,
                            },
                            new()
                            {
                                Module = "Mono JIT Code",
                                Function = "UnityEngine.Rendering.HighDefinition.HDRenderPipeline:PrepareGPULightdata (UnityEngine.Rendering.CommandBuffer,UnityEngine.Rendering.HighDefinition.HDCamera,UnityEngine.Rendering.CullingResults)",
                                FileName = "LightLoop.cs",
                                LineNumber = 1612,
                                InApp = false,
                            },
                            new()
                            {
                                Module = "Mono JIT Code",
                                Function = "UnityEngine.DebugLogHandler:Internal_Log (UnityEngine.LogType,UnityEngine.LogOption,string,UnityEngine.Object)",
                                InApp = false,
                            },
                            new()
                            {
                                Function = "DebugLogHandler_CUSTOM_Internal_Log(LogType, LogOption, ScriptingBackendNativeStringPtrOpaque*, ScriptingBackendNativeObjectPtrOpaque*)",
                            },
                            new()
                            {
                                Function = "DebugStringToFile(DebugStringToFileData const&)",
                            },
                            new()
                            {
                                Function = "GetStacktrace(int)",
                            }
                        }
                    },
                    Mechanism = new Mechanism
                    {
                        Handled = false,
                        Type = "unity.log"
                    }
                }
            },
            new object[]
            {
                "some error",
                @"0x00007ff765f778dc (Unity) StackWalker::GetCurrentCallstack
0x00007ff765f7ff79 (Unity) StackWalker::ShowCallstack
0x00007ff7674a3ddc (Unity) GetStacktrace
0x00007ff7685d2ee3 (Unity) DebugStringToFile
0x00007ff76604bd76 (Unity) DebugLogHandler_CUSTOM_Internal_Log
0x00000119566b0a3b (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log (UnityEngine.LogType,UnityEngine.LogOption,string,UnityEngine.Object)
0x00000119566b08bb (Mono JIT Code) UnityEngine.DebugLogHandler:LogFormat (UnityEngine.LogType,UnityEngine.Object,string,object[])
0x000001195523ff0e (Mono JIT Code) UnityEngine.Logger:Log (UnityEngine.LogType,object,UnityEngine.Object)
0x00000119566b0d4e (Mono JIT Code) UnityEngine.Debug:LogWarning (object,UnityEngine.Object)
0x00000119552c931b (Mono JIT Code) [NewBehaviourScript.cs:29] NewBehaviourScript:Logger ()
0x00000119552c8e9b (Mono JIT Code) [NewBehaviourScript.cs:16] NewBehaviourScript:Start ()
0x000001185d263a78 (Mono JIT Code) (wrapper runtime-invoke) object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)
0x00007ffc1e38e660 (mono-2.0-bdwgc) [mini-runtime.c:2816] mono_jit_runtime_invoke
0x00007ffc1e312ae2 (mono-2.0-bdwgc) [object.c:2921] do_runtime_invoke
0x00007ffc1e31bb3f (mono-2.0-bdwgc) [object.c:2968] mono_runtime_invoke
0x00007ff765dff0f4 (Unity) scripting_method_invoke
0x00007ff765df75f5 (Unity) ScriptingInvocation::Invoke
0x00007ff765da8d3d (Unity) MonoBehaviour::InvokeMethodOrCoroutineChecked
0x00007ff765da8e68 (Unity) MonoBehaviour::InvokeMethodOrCoroutineChecked
0x00007ff765da3bf2 (Unity) MonoBehaviour::DelayedStartCall
0x00007ff765264b64 (Unity) DelayedCallManager::Update
0x00007ff7656d7ec9 (Unity) `InitPlayerLoopCallbacks'::`2'::EarlyUpdateScriptRunDelayedStartupFrameRegistrator::Forward
0x00007ff7656b962c (Unity) ExecutePlayerLoop
0x00007ff7656b9703 (Unity) ExecutePlayerLoop
0x00007ff7656c0539 (Unity) PlayerLoop
0x00007ff766b169c1 (Unity) PlayerLoopController::UpdateScene
0x00007ff766b14659 (Unity) Application::TickTimer
0x00007ff7674d462e (Unity) WindowsDropTargetImpl::DragPerformed
0x00007ff7674bceaf (Unity) CDropTarget::Drop
0x00007ffca48bbae8 (ole32) RevokeActiveObjectExt
0x00007ffca48bb98c (ole32) RevokeActiveObjectExt
0x00007ffca4879179 (ole32) OleGetPackageClipboardOwner
0x00007ffca4878865 (ole32) OleGetPackageClipboardOwner
0x00007ffca487c938 (ole32) DoDragDrop
0x00007ff7674d6516 (Unity) DragAndDrop::StartDelayedDrag
0x00007ff7674d4808 (Unity) DragAndDrop::ApplyQueuedStartDrag
0x00007ff7674e625f (Unity) GUIView::OnInputEvent
0x00007ff766a2eb20 (Unity) GUIView::ProcessInputEvent
0x00007ff7674e7467 (Unity) GUIView::ProcessEventMessages
0x00007ff7674de9ba (Unity) GUIView::GUIViewWndProc
0x00007ffca524e7e8 (USER32) CallWindowProcW
0x00007ffca524e229 (USER32) DispatchMessageW
0x00007ff7674ad573 (Unity) MainMessageLoop
0x00007ff7674b1611 (Unity) WinMain
0x00007ff7693069b6 (Unity) __scrt_common_main_seh
0x00007ffca4de7034 (KERNEL32) BaseThreadInitThunk
0x00007ffca57c2651 (ntdll) RtlUserThreadStart
",
                LogType.Error,
                StackTraceLogType.Full,
                new SentryException {
                    Type = "Error",
                    Value = "some error",
                    Stacktrace = new SentryStackTrace
                    {
                        Frames = new List<SentryStackFrame>
                        {
                            new()
                            {
                                InstructionAddress = "0x00007ffca57c2651",
                                Module = "ntdll",
                                Function = "RtlUserThreadStart",
                                InApp = false,
                            },
                            new()
                            {
                                InstructionAddress = "0x00007ffca4de7034",
                                Module = "KERNEL32",
                                Function = "BaseThreadInitThunk",
                                InApp = false,
                            },
                            new()
                            {
                                InstructionAddress = "0x00007ff7693069b6",
                                Module = "Unity",
                                Function = "__scrt_common_main_seh",
                                InApp = false,
                            },
                            new()
                            {
                                InstructionAddress = "0x00007ff7674b1611",
                                Module = "Unity",
                                Function = "WinMain",
                                InApp = false,
                            },
                            new()
                            {
                                InstructionAddress = "0x00007ff7674ad573",
                                Module = "Unity",
                                Function = "MainMessageLoop",
                                InApp = false,
                            },
                            new()
                            {
                                InstructionAddress = "0x00007ffca524e229",
                                Module = "USER32",
                                Function = "DispatchMessageW",
                                InApp = false,
                            },
                            new()
                            {
                                InstructionAddress = "0x00007ffca524e7e8",
                                Module = "USER32",
                                Function = "CallWindowProcW",
                                InApp = false,
                            },
                            new()
                            {
                                InstructionAddress = "0x00007ff7674de9ba",
                                Module = "Unity",
                                Function = "GUIView::GUIViewWndProc",
                                InApp = false,
                            },
                            new()
                            {
                                InstructionAddress = "0x00007ff7674e7467",
                                Module = "Unity",
                                Function = "GUIView::ProcessEventMessages",
                                InApp = false,
                            },
                            new()
                            {
                                InstructionAddress = "0x00007ff766a2eb20",
                                Module = "Unity",
                                Function = "GUIView::ProcessInputEvent",
                                InApp = false,
                            },
                            new()
                            {
                                InstructionAddress = "0x00007ff7674e625f",
                                Module = "Unity",
                                Function = "GUIView::OnInputEvent",
                                InApp = false,
                            },
                            new()
                            {
                                InstructionAddress = "0x00007ff7674d4808",
                                Module = "Unity",
                                Function = "DragAndDrop::ApplyQueuedStartDrag",
                                InApp = false,
                            },
                            new()
                            {
                                InstructionAddress = "0x00007ff7674d6516",
                                Module = "Unity",
                                Function = "DragAndDrop::StartDelayedDrag",
                                InApp = false,
                            },
                            new()
                            {
                                InstructionAddress = "0x00007ffca487c938",
                                Module = "ole32",
                                Function = "DoDragDrop",
                                InApp = false,
                            },
                            new()
                            {
                                InstructionAddress = "0x00007ffca4878865",
                                Module = "ole32",
                                Function = "OleGetPackageClipboardOwner",
                                InApp = false,
                            },
                            new()
                            {
                                InstructionAddress = "0x00007ffca4879179",
                                Module = "ole32",
                                Function = "OleGetPackageClipboardOwner",
                                InApp = false,
                            },
                            new()
                            {
                                InstructionAddress = "0x00007ffca48bb98c",
                                Module = "ole32",
                                Function = "RevokeActiveObjectExt",
                                InApp = false,
                            },
                            new()
                            {
                                InstructionAddress = "0x00007ffca48bbae8",
                                Module = "ole32",
                                Function = "RevokeActiveObjectExt",
                                InApp = false,
                            },
                            new()
                            {
                                InstructionAddress = "0x00007ff7674bceaf",
                                Module = "Unity",
                                Function = "CDropTarget::Drop",
                                InApp = false,
                            },
                            new()
                            {
                                InstructionAddress = "0x00007ff7674d462e",
                                Module = "Unity",
                                Function = "WindowsDropTargetImpl::DragPerformed",
                                InApp = false,
                            },
                            new()
                            {
                                InstructionAddress = "0x00007ff766b14659",
                                Module = "Unity",
                                Function = "Application::TickTimer",
                                InApp = false,
                            },
                            new()
                            {
                                InstructionAddress = "0x00007ff766b169c1",
                                Module = "Unity",
                                Function = "PlayerLoopController::UpdateScene",
                                InApp = false,
                            },
                            new()
                            {
                                InstructionAddress = "0x00007ff7656c0539",
                                Module = "Unity",
                                Function = "PlayerLoop",
                                InApp = false,
                            },
                            new()
                            {
                                InstructionAddress = "0x00007ff7656b9703",
                                Module = "Unity",
                                Function = "ExecutePlayerLoop",
                                InApp = false,
                            },
                            new()
                            {
                                InstructionAddress = "0x00007ff7656b962c",
                                Module = "Unity",
                                Function = "ExecutePlayerLoop",
                                InApp = false,
                            },
                            new()
                            {
                                InstructionAddress = "0x00007ff7656d7ec9",
                                Module = "Unity",
                                Function = "`InitPlayerLoopCallbacks'::`2'::EarlyUpdateScriptRunDelayedStartupFrameRegistrator::Forward",
                                InApp = false,
                            },
                            new()
                            {
                                InstructionAddress = "0x00007ff765264b64",
                                Module = "Unity",
                                Function = "DelayedCallManager::Update",
                                InApp = false,
                            },
                            new()
                            {
                                InstructionAddress = "0x00007ff765da3bf2",
                                Module = "Unity",
                                Function = "MonoBehaviour::DelayedStartCall",
                                InApp = false,
                            },
                            new()
                            {
                                InstructionAddress = "0x00007ff765da8e68",
                                Module = "Unity",
                                Function = "MonoBehaviour::InvokeMethodOrCoroutineChecked",
                                InApp = false,
                            },
                            new()
                            {
                                InstructionAddress = "0x00007ff765da8d3d",
                                Module = "Unity",
                                Function = "MonoBehaviour::InvokeMethodOrCoroutineChecked",
                                InApp = false,
                            },
                            new()
                            {
                                InstructionAddress = "0x00007ff765df75f5",
                                Module = "Unity",
                                Function = "ScriptingInvocation::Invoke",
                                InApp = false,
                            },
                            new()
                            {
                                InstructionAddress = "0x00007ff765dff0f4",
                                Module = "Unity",
                                Function = "scripting_method_invoke",
                                InApp = false,
                            },
                            new()
                            {
                                InstructionAddress = "0x00007ffc1e31bb3f",
                                Module = "mono-2.0-bdwgc",
                                Function = "mono_runtime_invoke",
                                FileName = "object.c",
                                LineNumber = 2968,
                                InApp = false,
                            },
                            new()
                            {
                                InstructionAddress = "0x00007ffc1e312ae2",
                                Module = "mono-2.0-bdwgc",
                                Function = "do_runtime_invoke",
                                FileName = "object.c",
                                LineNumber = 2921,
                                InApp = false,
                            },
                            new()
                            {
                                InstructionAddress = "0x00007ffc1e38e660",
                                Module = "mono-2.0-bdwgc",
                                Function = "mono_jit_runtime_invoke",
                                FileName = "mini-runtime.c",
                                LineNumber = 2816,
                                InApp = false,
                            },
                            new()
                            {
                                InstructionAddress = "0x000001185d263a78",
                                Module = "Mono JIT Code",
                                Function = "object:runtime_invoke_void__this__ (object,intptr,intptr,intptr)",
                                InApp = false,
                            },
                            new()
                            {
                                InstructionAddress = "0x00000119552c8e9b",
                                Module = "Mono JIT Code",
                                Function = "NewBehaviourScript:Start ()",
                                FileName = "NewBehaviourScript.cs",
                                LineNumber = 16,
                                InApp = true,
                            },
                            new()
                            {
                                InstructionAddress = "0x00000119552c931b",
                                Module = "Mono JIT Code",
                                Function = "NewBehaviourScript:Logger ()",
                                FileName = "NewBehaviourScript.cs",
                                LineNumber = 29,
                                InApp = true,
                            },
                            new()
                            {
                                InstructionAddress = "0x00000119566b0d4e",
                                Module = "Mono JIT Code",
                                Function = "UnityEngine.Debug:LogWarning (object,UnityEngine.Object)",
                                InApp = false,
                            },
                            new()
                            {
                                InstructionAddress = "0x000001195523ff0e",
                                Module = "Mono JIT Code",
                                Function = "UnityEngine.Logger:Log (UnityEngine.LogType,object,UnityEngine.Object)",
                                InApp = false,
                            },
                            new()
                            {
                                InstructionAddress = "0x00000119566b08bb",
                                Module = "Mono JIT Code",
                                Function = "UnityEngine.DebugLogHandler:LogFormat (UnityEngine.LogType,UnityEngine.Object,string,object[])",
                                InApp = false,
                            },
                            new()
                            {
                                InstructionAddress = "0x00000119566b0a3b",
                                Module = "Mono JIT Code",
                                Function = "UnityEngine.DebugLogHandler:Internal_Log (UnityEngine.LogType,UnityEngine.LogOption,string,UnityEngine.Object)",
                                InApp = false,
                            },
                            new()
                            {
                                InstructionAddress = "0x00007ff76604bd76",
                                Module = "Unity",
                                Function = "DebugLogHandler_CUSTOM_Internal_Log",
                                InApp = false,
                            },
                            new()
                            {
                                InstructionAddress = "0x00007ff7685d2ee3",
                                Module = "Unity",
                                Function = "DebugStringToFile",
                                InApp = false,
                            },
                            new()
                            {
                                InstructionAddress = "0x00007ff7674a3ddc",
                                Module = "Unity",
                                Function = "GetStacktrace",
                                InApp = false,
                            },
                            new()
                            {
                                InstructionAddress = "0x00007ff765f7ff79",
                                Module = "Unity",
                                Function = "StackWalker::ShowCallstack",
                                InApp = false,
                            },
                            new()
                            {
                                InstructionAddress = "0x00007ff765f778dc",
                                Module = "Unity",
                                Function = "StackWalker::GetCurrentCallstack",
                                InApp = false,
                            },
                        },
                    },
                    Mechanism = new Mechanism
                    {
                        Handled = false,
                        Type = "unity.log"
                    }
                },
            }
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
