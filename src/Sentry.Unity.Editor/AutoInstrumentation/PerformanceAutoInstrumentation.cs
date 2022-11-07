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
using Debug = UnityEngine.Debug;

namespace Sentry.Unity.Editor
{
    [InitializeOnLoad]
    public static class PerformanceAutoInstrumentation
    {
        private const string PlayerAssembly = "Assembly-CSharp.dll";

        static PerformanceAutoInstrumentation()
        {
            var sentryUnityAssemblyPath = Path.GetFullPath(Path.Combine("Packages", SentryPackageInfo.GetName(), "Runtime", "Sentry.Unity.dll"));
            CompilationPipeline.assemblyCompilationFinished += (assemblyPath, _) =>
            {
                if (!BuildPipeline.isBuildingPlayer)
                {
                    return;
                }

                var (options, cliOptions) = SentryScriptableObject.ConfiguredBuildtimeOptions();
                if (options == null)
                {
                    return;
                }

                if (assemblyPath.Contains(PlayerAssembly))
                {
                    var logger = options.DiagnosticLogger;
                    if (options.TracesSampleRate <= 0.0f || !options.PerformanceAutoInstrumentationEnabled)
                    {
                        logger?.LogInfo("Performance Auto Instrumentation has been disabled.");
                        return;
                    }

                    logger?.LogInfo("Compilation of '{0}' finished. Running Performance Auto Instrumentation.", assemblyPath);

                    var stopwatch = Stopwatch.StartNew();

                    try
                    {
                        ModifyPlayerAssembly(logger, assemblyPath, sentryUnityAssemblyPath);
                    }
                    catch (Exception e)
                    {
                        logger?.LogError("Failed to add the performance auto instrumentation. " +
                                         "The assembly has not been modified.", e);
                    }

                    stopwatch.Stop();
                    logger?.LogInfo("Auto Instrumentation finished in '{0}'.", stopwatch.Elapsed);
                }
            };
        }

        private static TypeDefinition GetTypeDefinition(ModuleDefinition module, Type type)
        {
            var typeDefinition = module.GetType(type.FullName);
            if (typeDefinition is null)
            {
                throw new ArgumentException($"Failed to get requested type definition in {module.Name}", type.FullName);
            }

            return typeDefinition;
        }

        private static MethodReference ImportReference(ModuleDefinition module, MethodDefinition method)
        {
            var reference = module.ImportReference(method);
            if (reference is null)
            {
                throw new ArgumentException($"Failed to import requested reference in {module.Name}", method.FullName);
            }

            return reference;
        }

        private static MethodDefinition GetMethodDefinition(TypeDefinition typeDefinition, string name, Type[]? requiredParameters = null)
        {
            foreach (var method in typeDefinition.Methods)
            {
                if (method?.Name == name)
                {
                    switch (requiredParameters)
                    {
                        case null when !method.HasParameters:
                            return method;
                        case null:
                            continue;
                    }

                    if (method.Parameters.Count != requiredParameters.Length)
                    {
                        continue;
                    }

                    // We go over all the required parameters and compare them to the parameters found in the method.
                    var hasMatchingParameters = true;
                    foreach (var parameter in requiredParameters)
                    {
                        var parameterDefinitions = method.Parameters.Where(p =>
                            string.Equals(
                                p.ParameterType.FullName,
                                parameter.FullName,
                                StringComparison.Ordinal))
                            .ToList();

                        if (parameterDefinitions.Count == 0)
                        {
                            hasMatchingParameters = false;
                            break;
                        }
                    }

                    if (hasMatchingParameters)
                    {
                        return method;
                    }
                }
            }

            throw new Exception(
                $"Failed to find method '{name}' " +
                $"in '{typeDefinition.FullName}' " +
                $"with parameters: '{(requiredParameters is not null ? string.Join(",", requiredParameters.AsEnumerable()) : "none")}'");
        }

        private static void ModifyPlayerAssembly(
            IDiagnosticLogger? logger,
            string playerAssemblyPath,
            string sentryUnityAssemblyPath)
        {
            if (!File.Exists(playerAssemblyPath))
            {
                throw new FileNotFoundException($"Failed to find '{PlayerAssembly}' at '{playerAssemblyPath}'.");
            }

            if (!File.Exists(sentryUnityAssemblyPath))
            {
                throw new FileNotFoundException($"Failed to find '{Path.GetFileName(sentryUnityAssemblyPath)}' at '{sentryUnityAssemblyPath}'.");
            }

            var (sentryModule, _) = ModuleReaderWriter.Read(sentryUnityAssemblyPath);
            var (playerModule, hasSymbols) = ModuleReaderWriter.Read(playerAssemblyPath);

            var sentryMonoBehaviourDefinition = GetTypeDefinition(sentryModule, typeof(SentryMonoBehaviour));
            var monoBehaviourReference = playerModule.ImportReference(typeof(MonoBehaviour));

            var getInstanceMethod = ImportReference(playerModule, GetMethodDefinition(sentryMonoBehaviourDefinition, "get_Instance"));
            var startAwakeSpanMethod = ImportReference(playerModule, GetMethodDefinition(sentryMonoBehaviourDefinition, "StartAwakeSpan", new[] { typeof(MonoBehaviour) }));
            var finishAwakeSpanMethod = ImportReference(playerModule, GetMethodDefinition(sentryMonoBehaviourDefinition, "FinishAwakeSpan"));

            foreach (var type in playerModule.GetTypes())
            {
                if (type.BaseType?.FullName != monoBehaviourReference?.FullName)
                {
                    continue;
                }

                logger?.LogDebug("\tChecking: '{0}'", type.FullName);

                foreach (var method in type.Methods.Where(method => method.Name == "Awake"))
                {
                    logger?.LogDebug("\tDetected 'Awake' method.");

                    if (method.Body is null)
                    {
                        logger?.LogDebug("\tMethod body is null. Skipping.");
                        continue;
                    }

                    if (method.Body.Instructions is null)
                    {
                        logger?.LogDebug("\tInstructions are null. Skipping.");
                        continue;
                    }

                    logger?.LogDebug("\t\tAdding 'Start Awake Span'.");

                    var processor = method.Body.GetILProcessor();

                    // Adding in reverse order because we're inserting *before* the 0ths element
                    processor.InsertBefore(method.Body.Instructions[0], processor.Create(OpCodes.Callvirt, startAwakeSpanMethod));
                    processor.InsertBefore(method.Body.Instructions[0], processor.Create(OpCodes.Ldarg_0));
                    processor.InsertBefore(method.Body.Instructions[0], processor.Create(OpCodes.Call, getInstanceMethod));

                    logger?.LogDebug("\t\tAdding 'Finish Awake Span'.");

                    // We're checking the instructions for OpCode.Ret so we can insert the finish span instruction before it.
                    // Iterating over the collection backwards because we're modifying it's length
                    for (var i = method.Body.Instructions.Count - 1; i >= 0; i--)
                    {
                        if (method.Body.Instructions[i].OpCode == OpCodes.Ret)
                        {
                            processor.InsertBefore(method.Body.Instructions[i], processor.Create(OpCodes.Call, getInstanceMethod));
                            processor.InsertBefore(method.Body.Instructions[i + 1], processor.Create(OpCodes.Call, finishAwakeSpanMethod)); // +1 because return just moved one back
                        }
                    }
                }
            }

            logger?.LogInfo("Applying Auto Instrumentation by overwriting '{0}'.", Path.GetFileName(playerAssemblyPath));
            ModuleReaderWriter.Write(null, hasSymbols, playerModule, playerAssemblyPath);
        }
    }
}
