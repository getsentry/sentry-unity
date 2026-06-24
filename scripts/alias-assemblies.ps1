# Single source of truth for the assembly-aliasing invocation. Used by the local
# release script (repack.ps1) and by CI (build.yml, which runs it inside the Unity
# docker container).
#
# --assemblies-to-exclude: BCL assemblies Unity's unityaot profile already provides
# For more info see sentry-unity #2717, #1777).
param(
    # Pass an empty string to skip either target.
    [string]$RuntimeDir = "package-dev/Runtime",
    [string]$EditorDir = "package-dev/Editor",
    # assemblyalias is on PATH for local host builds; CI runs it from the dotnet
    # global tools path inside the container (/home/gh/.dotnet/tools/assemblyalias).
    [string]$AssemblyAlias = "assemblyalias"
)

$ErrorActionPreference = "Stop"

if ($RuntimeDir)
{
    & $AssemblyAlias --target-directory $RuntimeDir --internalize --prefix "Sentry." `
        --assemblies-to-alias "Microsoft*;System*" `
        --assemblies-to-exclude "System.Buffers;System.Memory;System.Numerics.Vectors;System.Threading.Tasks.Extensions"
    if ($LASTEXITCODE -ne 0) { throw "assemblyalias failed for '$RuntimeDir' (exit $LASTEXITCODE)" }
}

if ($EditorDir)
{
    & $AssemblyAlias --target-directory $EditorDir --internalize --prefix "Sentry." `
        --assemblies-to-alias "Microsoft*;Mono.Cecil*"
    if ($LASTEXITCODE -ne 0) { throw "assemblyalias failed for '$EditorDir' (exit $LASTEXITCODE)" }
}
