# Single source of truth for the assembly-aliasing invocation. Used by the local
# release script (repack.ps1) and by CI (build.yml, which runs it inside the Unity
# docker container).
#
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
        --assemblies-to-alias "Microsoft*;System*"
    if ($LASTEXITCODE -ne 0) { throw "assemblyalias failed for '$RuntimeDir' (exit $LASTEXITCODE)" }
}

if ($EditorDir)
{
    & $AssemblyAlias --target-directory $EditorDir --internalize --prefix "Sentry." `
        --assemblies-to-alias "Microsoft*;Mono.Cecil*"
    if ($LASTEXITCODE -ne 0) { throw "assemblyalias failed for '$EditorDir' (exit $LASTEXITCODE)" }
}
