using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
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

        private static IDiagnosticLogger? Logger;

        static PerformanceAutoInstrumentation()
        {
            PlayerAssemblyPath = Path.Combine(Application.dataPath, "..", "Library", OutputDirectory, PlayerAssembly);
            SentryAssemblyPath = Path.Combine(Application.dataPath, "..", "Packages", SentryPackageInfo.GetName(), "Runtime", "Sentry.Unity.dll");
        }

        [InitializeOnLoadMethod]
        public static void InitializeCompilationCallback()
        {
            var options = SentryScriptableObject.Load<ScriptableSentryUnityOptions>(ScriptableSentryUnityOptions.GetConfigPath());
            if (options == null || options.TracesSampleRate <= 0 && !options.AutoInstrumentPerformance)
            {
                return;
            }

            Logger = options.ToSentryUnityOptions(isBuilding: true).DiagnosticLogger;

            CompilationPipeline.assemblyCompilationFinished += (assemblyPath, compilerMessages) =>
            {
                if (assemblyPath.Contains(Path.Combine(OutputDirectory, PlayerAssembly)))
                {
                    Logger?.LogInfo("Compilation of '{0}' finished. Attempting to adding performance auto instrumentation.", PlayerAssembly);
                    var stopwatch = new Stopwatch();
                    stopwatch.Start();

                    try
                    {
                        ModifyPlayerAssembly(PlayerAssemblyPath, SentryAssemblyPath);
                    }
                    catch (Exception e)
                    {
                        Logger?.LogError("Failed to add the performance auto instrumentation. " +
                                         "The player assembly has not been modified.", e);
                    }

                    stopwatch.Stop();
                    Logger?.LogInfo("Finished attempt in '{0}'", stopwatch.Elapsed);
                }
            };
        }

        [MenuItem("Tools/Modify Assembly")]
        public static void JustForDevelopingSoIDontHaveToBuildEveryTimeAndICanCompareTheAssemblies()
        {
            var playerAssemblyPath = Path.Combine(Application.dataPath, "..", "Builds",
                "TestBuild.app", "Contents", "Resources", "Data", "Managed", PlayerAssembly);
            var sentryAssemblyPath = Path.Combine(Application.dataPath, "..", "Builds",
                "TestBuild.app", "Contents", "Resources", "Data", "Managed", "Sentry.Unity.dll");

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
                ModifyPlayerAssembly(PlayerAssemblyPath, SentryAssemblyPath);
            }
            catch (Exception e)
            {
                Logger?.LogError("Failed to add the performance auto instrumentation. " +
                                 "The player assembly has not been modified.", e);
            }

            stopwatch.Stop();
            Logger?.LogInfo("Finished Adding performance auto instrumentation in '{0}'", stopwatch.Elapsed);
        }

        private static void ModifyPlayerAssembly(string playerAssemblyPath, string sentryAssemblyPath)
        {
            if (!File.Exists(playerAssemblyPath))
            {
                throw new FileNotFoundException($"Player assembly at '{playerAssemblyPath}' not found.");
            }

            if (!File.Exists(sentryAssemblyPath))
            {
                throw new FileNotFoundException($"Sentry assembly at '{sentryAssemblyPath}' not found.");
            }

            var (sentryModule, _) = ModuleReaderWriter.Read(sentryAssemblyPath);
            var (gameModule, hasSymbols) = ModuleReaderWriter.Read(playerAssemblyPath);

            var sentryAwake = sentryModule.GetType(typeof(SentryAwakeHelpers).FullName);
            if (sentryAwake is null)
            {
                throw new Exception($"Failed to find type definition for '{typeof(SentryAwakeHelpers).FullName}'");
            }

            var startSpanMethodReference = gameModule.ImportReference(GetMethod(sentryAwake, "StartSpan"));
            var finishSpanMethodReference = gameModule.ImportReference(GetMethod(sentryAwake, "FinishSpan"));

            var monoBehaviourReference = gameModule.ImportReference(typeof(MonoBehaviour));
            if (monoBehaviourReference is null)
            {
                throw new Exception("Failed to import 'MonoBehaviour' type reference.");
            }

            foreach (var type in gameModule.GetTypes())
            {
                if (type.BaseType != monoBehaviourReference)
                {
                    continue;
                }

                Logger?.LogDebug("Modifying '{0}'", type.FullName);

                foreach (var method in type.Methods.Where(method => method.Name == "Awake"))
                {
                    Logger?.LogDebug("Found 'Awake' method.");

                    if (method.Body is null)
                    {
                        Logger?.LogDebug("Method body is null. Skipping.");
                        continue;
                    }
                    var instructions = method.Body.Instructions;
                    if (instructions is null)
                    {
                        Logger?.LogDebug("Instructions are null. Skipping.");
                        continue;
                    }

                    var processor = method.Body.GetILProcessor();

                    var firstInstruction = method.Body.Instructions[0];
                    var startSpanInstruction = processor.Create(OpCodes.Call, startSpanMethodReference);
                    processor.InsertBefore(firstInstruction, startSpanInstruction);
                    processor.InsertBefore(startSpanInstruction, processor.Create(OpCodes.Ldarg_0));

                    var finishSpanInstruction = processor.Create(OpCodes.Call, finishSpanMethodReference);

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

        private static MethodDefinition? GetMethod(TypeDefinition type, string name) =>
            type.Methods.FirstOrDefault(method => method.Name == name);
    }
}
