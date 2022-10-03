using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace Sentry.Unity.Editor
{
    public static class BuildsInterceptor
    {
        private static readonly string PlayerAssemblyPath;
        private static readonly string SentryAssemblyPath;

        static BuildsInterceptor()
        {
            PlayerAssemblyPath = Path.Combine(Application.dataPath, "..", "Library", "PlayerScriptAssemblies",
                "Assembly-CSharp.dll");
            SentryAssemblyPath = Path.Combine(Application.dataPath, "..", "Packages",
                SentryPackageInfo.GetName(), "Runtime", "Sentry.Unity.dll");
        }

        [InitializeOnLoadMethod]
        public static void CompilationHook()
        {
            // CompilationPipeline.assemblyCompilationFinished += (assemblyPath, compilerMessages) =>
            // {
            //     if (assemblyPath.Contains("PlayerScriptAssemblies/Assembly-CSharp.dll"))
            //     {
            //         Debug.Log($"<color=green>Finished compiling: {assemblyPath} with size: {new FileInfo(assemblyPath).Length}</color>");
            //         Debug.Log("Modifying assembly now.");
            //         ModifyAssembly(PlayerAssemblyPath, SentryAssemblyPath);
            //     }
            // };
        }

        [MenuItem("Tools/Test Paths")]
        public static void TestThePaths()
        {
            Debug.Log(File.Exists(PlayerAssemblyPath) ? "found assembly to process" : "failed to find assembly to process");
            Debug.Log(File.Exists(SentryAssemblyPath) ? "found assembly to reference" : "failed to find assembly to reference");
        }

        [MenuItem("Tools/Modify Assembly")]
        public static void JustForDevelopingSoIDontHaveToBuildEveryTime()
        {
            var playerAssemblyPath = Path.Combine(Application.dataPath, "..", "Builds",
                "TestBuild.app", "Contents", "Resources", "Data", "Managed", "Assembly-CSharp.dll");
            var sentryAssemblyPath = Path.Combine(Application.dataPath, "..", "Builds",
                "TestBuild.app", "Contents", "Resources", "Data", "Managed", "Sentry.Unity.dll");

            var workingPlayerAssemblyPath = playerAssemblyPath.Replace("Assembly-CSharp", "Assembly-CSharp-WIP");
            if (File.Exists(workingPlayerAssemblyPath))
            {
                File.Delete(workingPlayerAssemblyPath);
            }

            File.Copy(playerAssemblyPath, workingPlayerAssemblyPath);

            ModifyAssembly(workingPlayerAssemblyPath, sentryAssemblyPath);
        }

        private static void ModifyAssembly(string playerAssemblyPath, string sentryAssemblyPath)
        {
            if (!File.Exists(playerAssemblyPath))
            {
                Debug.Log($"Failed to find player assembly at: {playerAssemblyPath}");
                return;
            }

            if (!File.Exists(sentryAssemblyPath))
            {
                Debug.Log($"Failed to find Sentry assembly at: {sentryAssemblyPath}");
                return;
            }

            var (sentryModule, _) = ModuleReaderWriter.Read(sentryAssemblyPath);
            var (gameModule, hasSymbols) = ModuleReaderWriter.Read(playerAssemblyPath);
            // moduleToProcess.ModuleReferences.Add(sentryModule);

            // var replacementTypeReference = moduleToProcess.ImportReference(typeof(SentryMonoBehaviour));
            // if (replacementTypeReference is null)
            // {
            //     Debug.Log("Failed to find replacement type reference.");
            //     return;
            // }

            foreach (var type in gameModule.GetTypes())
            {
                if (type.FullName == "SentryEmptyBehaviour")
                {
                    Debug.Log("Found 'SentryEmptyBehaviour'.");

                    foreach (var method in type.Methods)
                    {
                        if (method.Name == "Awake")
                        {
                            Debug.Log("Found 'Awake'.");
                            var sentryType = sentryModule.GetType("Sentry.Unity.SentryAwakeIntegration");
                            if (sentryType is null)
                            {
                                Debug.Log("Sentry type null. Aborting");
                                return;
                            }

                            var startSpanMethodReference = gameModule.ImportReference(GetMethod(sentryType, "StartSpan"));
                            var finishSpanMethodReference = gameModule.ImportReference(GetMethod(sentryType, "FinishSpan"));

                            var instructions = method.Body?.Instructions;
                            if (instructions is null)
                            {
                                Debug.Log($"Instructions of '{method.Name}' are null.");
                                continue;
                            }

                            if (method.Body is null)
                            {
                                Debug.Log("Method body is null. Aborting");
                                continue;
                            }

                            // method.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Call, spanMethodReference));

                            var firstInstruction = method.Body.Instructions[0];
                            var processor = method.Body.GetILProcessor();

                            var thisInstruction = processor.Create(OpCodes.Ldarg_0);
                            processor.InsertBefore(firstInstruction, thisInstruction);

                            var startSpanInstruction = processor.Create(OpCodes.Call, startSpanMethodReference);
                            processor.InsertBefore(firstInstruction, startSpanInstruction);

                            var finishSpanInstruction = processor.Create(OpCodes.Call, finishSpanMethodReference);

                            var lastInstruction = method.Body.Instructions[method.Body.Instructions.Count - 1];
                            processor.InsertBefore(lastInstruction, finishSpanInstruction);
                        }
                    }
                }

                // foreach (var method in type.Methods)
                // {
                //     var instructions = method.Body?.Instructions;
                //     if (instructions is null)
                //     {
                //         continue;
                //     }
                //
                //     foreach (var instruction in instructions.Where(x => x.OpCode == OpCodes.Newobj))
                //     {
                //         if (instruction.Operand is not MethodReference reference)
                //         {
                //             continue;
                //         }
                //
                //         var declaringType = reference.DeclaringType;
                //         if (reference.Name == "Awake" && declaringType.FullName == typeof(MonoBehaviour).FullName)
                //         {
                //             // reference.DeclaringType = replacementTypeReference;
                //         }
                //     }
                // }
            }

            Debug.Log("Overwriting player assembly with modified assembly.");
            ModuleReaderWriter.Write(null, hasSymbols, gameModule, playerAssemblyPath);
        }

        private static MethodDefinition? GetMethod(TypeDefinition type, string name)
        {
            foreach (var method in type.Methods)
            {
                if (method.Name == name)
                {
                    return method;
                }
            }
            return null;
        }

        private static void InjectInstructions(MethodBody body, int line, params Instruction[] instructions)
        {
            foreach (var instr in instructions)
            {
                body.Instructions.Insert(line, instr);
                line++;
            }
        }
    }
}
