Remove-Item "package-dev/Runtime/*.dll"
dotnet build
# Exclude the BCL assemblies that Unity's unityaot profile already provides (as facades or real lib).
# Aliasing them mints a duplicate System.Span<T>/ReadOnlySpan<T> definition that collides with the
# runtime's copy, breaking IL2CPP's intrinsic remap on Unity 2022+/6000 (see sentry-unity #2717, #1777).
assemblyalias --target-directory "./package-dev/Runtime/" --internalize --prefix "Sentry." --assemblies-to-alias "Microsoft*;System*" --assemblies-to-exclude "System.Buffers;System.Memory;System.Numerics.Vectors;System.Threading.Tasks.Extensions"