using System.IO;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

public class CompileInterceptor
{
    [InitializeOnLoadMethod]
    public static void Setup()
    {
        CompilationPipeline.compilationStarted += o => Debug.Log("Compilation started.");

        CompilationPipeline.assemblyCompilationStarted += assemblyPath =>
            Debug.Log($"Compiling: {assemblyPath}");;

        CompilationPipeline.assemblyCompilationFinished += (assemblyPath, compilerMessages) =>
        {
            if (assemblyPath.Contains("Assembly-CSharp.dll"))
            {
                Debug.Log($"<color=red>Finished compiling: {assemblyPath} with size: {new FileInfo(assemblyPath).Length}</color>");

                // This is running on the main thread
                Debug.Log("IL Weaving will happen here.");
            }
        };

        CompilationPipeline.compilationFinished += o => Debug.Log("Compilation finished.");
    }
}
