using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

[InitializeOnLoad]
public class CompileInterceptor
{
    static CompileInterceptor()
    {
        CompilationPipeline.compilationStarted -= OnCompilationStarted;
        CompilationPipeline.compilationStarted += OnCompilationStarted;

        CompilationPipeline.assemblyCompilationStarted -= OnAssemblyCompilationStarted;
        CompilationPipeline.assemblyCompilationStarted += OnAssemblyCompilationStarted;

        CompilationPipeline.assemblyCompilationFinished -= OnAssemblyCompilationFinished;
        CompilationPipeline.assemblyCompilationFinished += OnAssemblyCompilationFinished;

        CompilationPipeline.compilationFinished -= OnCompilationFinished;
        CompilationPipeline.compilationFinished += OnCompilationFinished;
    }

    private static void OnCompilationStarted(object o) => Debug.Log("Compilation started.");
    private static void OnAssemblyCompilationStarted(string assemblyPath) =>
        Debug.Log($"Starting to compile: {assemblyPath}");

    private static void OnAssemblyCompilationFinished(string assemblyPath, CompilerMessage[] compilerMessages)
    {
        if (assemblyPath.Contains("Assembly-CSharp.dll"))
        {
            Debug.Log($"Finished compiling: {assemblyPath}");

            // This is running on the main thread
            Debug.Log("This is where IL Weaving will happen.");
        }
    }

    private static void OnCompilationFinished(object o) => Debug.Log("Compilation finished.");
}
