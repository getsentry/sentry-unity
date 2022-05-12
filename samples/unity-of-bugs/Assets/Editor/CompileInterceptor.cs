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
            Debug.Log($"Starting to compile: {assemblyPath}");;

        CompilationPipeline.assemblyCompilationFinished += (assemblyPath, compilerMessages) =>
        {
            if (assemblyPath.Contains("Assembly-CSharp.dll"))
            {
                Debug.Log($"Finished compiling: {assemblyPath}");

                // This is running on the main thread
                Debug.Log("This is where IL Weaving will happen.");
            }
        };

        CompilationPipeline.compilationFinished += o => Debug.Log("Compilation finished.");
    }
}
