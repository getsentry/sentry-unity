using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Mono.Cecil.Cil;
using Sentry.Extensibility;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Sentry.Unity.Editor;

public class SentryPerformanceAutoInstrumentation : IPostBuildPlayerScriptDLLs
{
    public int callbackOrder { get; }
    public void OnPostBuildPlayerScriptDLLs(BuildReport report)
    {
        var (options, cliOptions) = SentryScriptableObject.ConfiguredBuildTimeOptions();
        if (options == null)
        {
            return;
        }

        var logger = options.DiagnosticLogger ?? new UnityLogger(options);

        if (!options.IsValid())
        {
            logger.LogDebug("Performance Auto Instrumentation disabled.");
            return;
        }

        if (options.TracesSampleRate <= 0.0f || !options.PerformanceAutoInstrumentationEnabled)
        {
            logger.LogInfo("Performance Auto Instrumentation has been disabled.");
            return;
        }

        logger.LogInfo("Running Performance Auto Instrumentation in PostBuildScriptPhase.");

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var workingDirectory = Path.Combine(Application.dataPath, "..", "Temp", "StagingArea", "Data", "Managed");
            var playerReaderWriter = SentryPlayerReaderWriter.ReadAssemblies(workingDirectory);
            ModifyPlayerAssembly(logger, playerReaderWriter);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to add the performance auto instrumentation. " +
                               "The assembly has not been modified.");
        }

        stopwatch.Stop();
        logger.LogInfo("Auto Instrumentation finished in {0} seconds.", stopwatch.Elapsed.Seconds);
    }

    private static void ModifyPlayerAssembly(IDiagnosticLogger logger, SentryPlayerReaderWriter playerReaderWriter)
    {
        var getInstanceMethod = playerReaderWriter.ImportSentryMonoBehaviourMethod("get_Instance");
        var startAwakeSpanMethod = playerReaderWriter.ImportSentryMonoBehaviourMethod("StartAwakeSpan", new[] { typeof(MonoBehaviour) });
        var finishAwakeSpanMethod = playerReaderWriter.ImportSentryMonoBehaviourMethod("FinishAwakeSpan");

        var monoBehaviourReference = playerReaderWriter.ImportType(typeof(MonoBehaviour));

        foreach (var type in playerReaderWriter.GetTypes())
        {
            if (type.BaseType?.FullName != monoBehaviourReference?.FullName)
            {
                continue;
            }

            logger.LogDebug("\tChecking: '{0}'", type.FullName);

            foreach (var method in type.Methods.Where(method => method.Name == "Awake"))
            {
                logger.LogDebug("\tDetected 'Awake' method.");

                if (method.Body is null)
                {
                    logger.LogDebug("\tMethod body is null. Skipping.");
                    continue;
                }

                if (method.Body.Instructions is null)
                {
                    logger.LogDebug("\tInstructions are null. Skipping.");
                    continue;
                }

                logger.LogDebug("\t\tAdding 'Start Awake Span'.");

                var processor = method.Body.GetILProcessor();

                // Adding in reverse order because we're inserting *before* the 0ths element
                processor.InsertBefore(method.Body.Instructions[0], processor.Create(OpCodes.Callvirt, startAwakeSpanMethod));
                processor.InsertBefore(method.Body.Instructions[0], processor.Create(OpCodes.Ldarg_0));
                processor.InsertBefore(method.Body.Instructions[0], processor.Create(OpCodes.Call, getInstanceMethod));

                logger.LogDebug("\t\tAdding 'Finish Awake Span'.");

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

        logger.LogInfo("Applying Auto Instrumentation.");
        playerReaderWriter.Write();
    }
}