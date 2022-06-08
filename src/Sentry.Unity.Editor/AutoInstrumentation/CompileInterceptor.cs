using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace Sentry.Unity.Editor.AutoInstrumentation
{
    public static class CompileInterceptor
    {
        private static readonly string PlayerAssemblyPath;
        private static readonly string SentryAssemblyPath;

        static CompileInterceptor()
        {
            PlayerAssemblyPath = Path.Combine(Application.dataPath, "..", "Library", "PlayerScriptAssemblies",
                "Assembly-CSharp.dll");
            SentryAssemblyPath = Path.Combine(Application.dataPath, "..", "Packages",
                SentryPackageInfo.GetName(), "Runtime", "Sentry.Unity.dll");
        }

        [InitializeOnLoadMethod]
        public static void CompilationHook()
        {
            CompilationPipeline.assemblyCompilationFinished += (assemblyPath, compilerMessages) =>
            {
                if (assemblyPath.Contains("PlayerScriptAssemblies/Assembly-CSharp.dll"))
                {
                    Debug.Log($"<color=green>Finished compiling: {assemblyPath} with size: {new FileInfo(assemblyPath).Length}</color>");
                    Debug.Log("Modifying assembly now.");
                    ModifyAssembly(PlayerAssemblyPath, SentryAssemblyPath);
                }
            };
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
                "test_BackUpThisFolder_ButDontShipItWithYourGame", "Managed", "Assembly-CSharp.dll");
            var sentryAssemblyPath = Path.Combine(Application.dataPath, "..", "Builds",
                "test_BackUpThisFolder_ButDontShipItWithYourGame", "Managed", "Sentry.Unity.dll");

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

            var (moduleToReference, _) = ModuleReaderWriter.Read(sentryAssemblyPath);
            var (moduleToProcess, hasSymbols) = ModuleReaderWriter.Read(playerAssemblyPath);
            moduleToProcess.ModuleReferences.Add(moduleToReference);

            var replacementTypeReference = moduleToProcess.ImportReference(typeof(ReplacementClass));
            if (replacementTypeReference is null)
            {
                Debug.Log("Failed to find replacement type reference.");
                return;
            }

            foreach (var type in moduleToProcess.GetTypes())
            {
                foreach (var method in type.Methods)
                {
                    var instructions = method.Body?.Instructions;
                    if (instructions is null)
                    {
                        continue;
                    }

                    foreach (var instruction in instructions.Where(x => x.OpCode == OpCodes.Newobj))
                    {
                        if (instruction.Operand is not MethodReference reference)
                        {
                            continue;
                        }

                        var declaringType = reference.DeclaringType;
                        if (reference.Name == ".ctor" && declaringType.FullName == typeof(ClassToBeReplaced).FullName)
                        {
                            reference.DeclaringType = replacementTypeReference;
                        }
                    }
                }
            }

            Debug.Log("Overwriting player assembly with modified assembly.");
            ModuleReaderWriter.Write(null, hasSymbols, moduleToProcess, playerAssemblyPath);
        }
    }
}
