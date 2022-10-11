using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;
using Sentry.Extensibility;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace Sentry.Unity.Editor
{
    public static class PerformanceAutoInstrumentation
    {
        private const string PlayerAssembly = "Assembly-CSharp.dll";
        private const string OutputDirectory = "PlayerScriptAssemblies"; // There are multiple directories involved - we want this one in particular

        private static readonly string PlayerAssemblyPath;
        private static readonly string SentryAssemblyPath;
        private static readonly string CoreAssemblyPath;


        private static IDiagnosticLogger? Logger;

        static PerformanceAutoInstrumentation()
        {
            PlayerAssemblyPath = Path.Combine(Application.dataPath, "..", "Library", OutputDirectory, PlayerAssembly);
            SentryAssemblyPath = Path.Combine(Application.dataPath, "..", "Packages", SentryPackageInfo.GetName(), "Runtime", "Sentry.dll");
            CoreAssemblyPath = Path.Combine(Application.dataPath, "..", "Library", OutputDirectory, "UnityEngine.CoreModule.dll");
        }

        [InitializeOnLoadMethod]
        public static void InitializeCompilationCallback()
        {
            // var options = SentryScriptableObject.Load<ScriptableSentryUnityOptions>(ScriptableSentryUnityOptions.GetConfigPath());
            // if (options == null || options.TracesSampleRate <= 0 && !options.AutoInstrumentPerformance)
            // {
            //     return;
            // }
            //
            // Logger = options.ToSentryUnityOptions(isBuilding: true).DiagnosticLogger;
            //
            // CompilationPipeline.assemblyCompilationFinished += (assemblyPath, compilerMessages) =>
            // {
            //     if (assemblyPath.Contains(Path.Combine(OutputDirectory, PlayerAssembly)))
            //     {
            //         Logger?.LogInfo("Compilation of '{0}' finished. Attempting to adding performance auto instrumentation.", PlayerAssembly);
            //         var stopwatch = new Stopwatch();
            //         stopwatch.Start();
            //
            //         try
            //         {
            //             ModifyPlayerAssembly(PlayerAssemblyPath, SentryAssemblyPath);
            //         }
            //         catch (Exception e)
            //         {
            //             Logger?.LogError("Failed to add the performance auto instrumentation. " +
            //                              "The player assembly has not been modified.", e);
            //         }
            //
            //         stopwatch.Stop();
            //         Logger?.LogInfo("Finished attempt in '{0}'", stopwatch.Elapsed);
            //     }
            // };
        }

        [MenuItem("Tools/Modify Assembly")]
        public static void JustForDevelopingSoIDontHaveToBuildEveryTimeAndICanCompareTheAssemblies()
        {
            var options = SentryScriptableObject.Load<ScriptableSentryUnityOptions>(ScriptableSentryUnityOptions.GetConfigPath());
            Logger = new UnityLogger(options!.ToSentryUnityOptions(true));

            var playerAssemblyPath = Path.Combine(Application.dataPath, "..", "Builds",
                "Test.app", "Contents", "Resources", "Data", "Managed", PlayerAssembly);
            var sentryAssemblyPath = Path.Combine(Application.dataPath, "..", "Builds",
                "Test.app", "Contents", "Resources", "Data", "Managed", "Sentry.dll");
            var coreAssemblyPath = Path.Combine(Application.dataPath, "..", "Builds",
                "Test.app", "Contents", "Resources", "Data", "Managed", "UnityEngine.CoreModule.dll");
            var netstandardAssemblyPath = Path.Combine(Application.dataPath, "..", "Builds",
                "Test.app", "Contents", "Resources", "Data", "Managed", "netstandard.dll");
            var mscorelibAssemblyPath = Path.Combine(Application.dataPath, "..", "Builds",
                "Test.app", "Contents", "Resources", "Data", "Managed", "mscorlib.dll");

            var workingPlayerAssemblyPath = playerAssemblyPath.Replace(PlayerAssembly, "Assembly-CSharp-WIP");
            if (File.Exists(workingPlayerAssemblyPath))
            {
                File.Delete(workingPlayerAssemblyPath);
            }

            File.Copy(playerAssemblyPath, workingPlayerAssemblyPath);

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            try
            {
                ModifyPlayerAssembly(workingPlayerAssemblyPath, sentryAssemblyPath, coreAssemblyPath, netstandardAssemblyPath, mscorelibAssemblyPath);
            }
            catch (Exception e)
            {
                Logger?.LogError("Failed to add the performance auto instrumentation. " +
                                 "The player assembly has not been modified.", e);
            }

            stopwatch.Stop();
            Logger?.LogInfo("Finished Adding performance auto instrumentation in '{0}'", stopwatch.Elapsed);
        }

        private static void ModifyPlayerAssembly(
            string playerAssemblyPath,
            string sentryAssemblyPath,
            string coreAssemblyPath,
            string netstandardAssemblyPath,
            string mscorelibAssemblyPath)
        {
            if (!File.Exists(playerAssemblyPath))
            {
                throw new FileNotFoundException($"Player assembly at '{playerAssemblyPath}' not found.");
            }

            if (!File.Exists(sentryAssemblyPath))
            {
                throw new FileNotFoundException($"Sentry assembly at '{sentryAssemblyPath}' not found.");
            }

            if (!File.Exists(coreAssemblyPath))
            {
                throw new FileNotFoundException($"Core assembly at '{coreAssemblyPath}' not found.");
            }

            if (!File.Exists(netstandardAssemblyPath))
            {
                throw new FileNotFoundException($"netstandard assembly at '{netstandardAssemblyPath}' not found.");
            }

            if (!File.Exists(mscorelibAssemblyPath))
            {
                throw new FileNotFoundException($"mscorelib assembly at '{mscorelibAssemblyPath}' not found.");
            }

            var (sentryModule, _) = ModuleReaderWriter.Read(sentryAssemblyPath);
            var (gameModule, hasSymbols) = ModuleReaderWriter.Read(playerAssemblyPath);
            var (coreModule, _) = ModuleReaderWriter.Read(coreAssemblyPath);
            var (netstandardModule, _) = ModuleReaderWriter.Read(netstandardAssemblyPath);
            var (mscorelibModule, _) = ModuleReaderWriter.Read(mscorelibAssemblyPath);

            var sentryType = sentryModule.GetType(typeof(SentrySdk).FullName);
            if (sentryType is null)
            {
                throw new Exception($"Failed to find type definition for '{typeof(SentrySdk).FullName}'");
            }

            var spanExtensionType = sentryModule.GetType(typeof(SpanExtensions).FullName);
            if (spanExtensionType is null)
            {
                throw new Exception("Failed to find 'SpanExtensions'");
            }

            var spanType = sentryModule.GetType(typeof(ISpan).FullName);
            if (spanType is null)
            {
                throw new Exception("Failed to find 'ISpan'");
            }

            var gameObjectType = coreModule.GetType(typeof(GameObject).FullName);
            if (gameObjectType is null)
            {
                throw new Exception($"Failed to find type definition for '{typeof(GameObject).FullName}'");
            }

            var objectType = coreModule.GetType(typeof(UnityEngine.Object).FullName);
            if (objectType is null)
            {
                throw new Exception($"Failed to find type definition for '{typeof(UnityEngine.Object).FullName}'");
            }

            var stringType = mscorelibModule.GetType(typeof(string).FullName);
            if (stringType is null)
            {
                throw new Exception($"Failed to find type definition for '{typeof(String).FullName}'");
            }

            var getSpanReference = gameModule.ImportReference(GetMethod(sentryType, "GetSpan"));

            var monoBehaviourReference = gameModule.ImportReference(typeof(MonoBehaviour));
            if (monoBehaviourReference is null)
            {
                throw new Exception("Failed to import 'MonoBehaviour' type reference.");
            }

            foreach (var type in gameModule.GetTypes())
            {
                if (type.BaseType?.FullName != monoBehaviourReference?.FullName)
                {
                    continue;
                }

                if (type.FullName != "SentryEmptyBehaviour")
                {
                    continue;
                }

                Logger?.LogDebug("Checking: '{0}'", type.FullName);

                foreach (var method in type.Methods.Where(method => method.Name == "Awake"))
                {
                    Logger?.LogDebug("Found 'Awake' method.");

                    if (method.Body is null)
                    {
                        Logger?.LogDebug("Method body is null. Skipping.");
                        continue;
                    }
                    var instructions = new Collection<Instruction>(method.Body.Instructions);
                    if (instructions is null)
                    {
                        Logger?.LogDebug("Instructions are null. Skipping.");
                        continue;
                    }

                    var processor = method.Body.GetILProcessor();
                    processor.Clear();

                    var awakeInstruction = processor.Create(OpCodes.Ldstr, "Awake");
                    processor.Append(awakeInstruction);
                    processor.Append(processor.Create(OpCodes.Ldarg_0));

                    var gameObjectReference = gameModule.ImportReference(GetMethod(gameObjectType, "get_gameObject"));
                    var getGameObjectInstruction = processor.Create(OpCodes.Call, gameObjectReference);
                    processor.Append(getGameObjectInstruction);

                    var getNameReference = gameModule.ImportReference(GetMethod(objectType, "get_name"));
                    var getNameInstruction = processor.Create(OpCodes.Callvirt, getNameReference);
                    processor.Append(getNameInstruction);

                    processor.Append(processor.Create(OpCodes.Ldstr, "."));
                    processor.Append(processor.Create(OpCodes.Ldarg_0));

                    processor.Append(processor.Create(OpCodes.Call, getNameReference));


                    var concatReference = gameModule.ImportReference(GetMethod(stringType, "Concat", 3));
                    processor.Append(processor.Create(OpCodes.Call, concatReference));

                    var startSpanReference = gameModule.ImportReference(GetMethod(spanExtensionType, "StartChild"));
                    processor.Append(processor.Create(OpCodes.Call, startSpanReference));

                    processor.Append(processor.Create(OpCodes.Pop));

                    // Append original contents of 'Awake'
                    var firstOriginalInstruction = instructions[0];
                    processor.Append(firstOriginalInstruction);
                    for (var i = 1; i < instructions.Count; i++)
                    {
                        processor.Append(instructions[i]);
                    }

                    //
                    processor.InsertBefore(awakeInstruction, processor.Create(OpCodes.Call, getSpanReference));
                    processor.InsertBefore(awakeInstruction, processor.Create(OpCodes.Dup));
                    processor.InsertBefore(awakeInstruction, processor.Create(OpCodes.Brtrue_S, awakeInstruction));

                    processor.InsertBefore(awakeInstruction, processor.Create(OpCodes.Pop));
                    processor.InsertBefore(awakeInstruction, processor.Create(OpCodes.Brtrue_S, firstOriginalInstruction));



                    // Finishing Span at all Returns
                    //
                    var finishSpanReference = gameModule.ImportReference(GetMethod(spanType, "FinishSpan"));
                    var finishSpanInstruction = processor.Create(OpCodes.Call, finishSpanReference);

                    // We're checking the instructions for OpCode.Ret so we can insert the finish span instruction before it.
                    // Iterating over the collection backwards because we're modifying it's length
                    for (var i = instructions.Count - 1; i >= 0; i--)
                    {
                        if (instructions[i].OpCode == OpCodes.Ret)
                        {
                            processor.InsertBefore(instructions[i], finishSpanInstruction);
                            i++; // Because we inserted *before* the current call we set 'i' to currently checked instruction
                        }
                    }
                }
            }

            Logger?.LogInfo("Applying auto instrumentation by overwriting player assembly.");
            ModuleReaderWriter.Write(null, hasSymbols, gameModule, playerAssemblyPath);
        }

        private static MethodDefinition? GetMethod(TypeDefinition type, string name, int paramscount = -1)
        {
            foreach (var method in type.Methods)
            {
                if (method.Name == name && (paramscount == -1 || method.Parameters.Count == paramscount))
                {
                    return method;
                }
            }
            return null;
        }
    }
}
