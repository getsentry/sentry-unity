# Clean up previous release artifacts
if (Test-Path "package-release") {
    Remove-Item -Path "package-release" -Recurse -Force
}

if (Test-Path "package-release.zip") {
    Remove-Item -Path "package-release.zip" -Force
}

assemblyalias --target-directory "package-dev/Runtime" --internalize --prefix "Sentry." --assemblies-to-alias "Microsoft*;System*"
assemblyalias --target-directory "package-dev/Editor" --internalize --prefix "Sentry." --assemblies-to-alias "Microsoft*;Mono.Cecil*"

. $PSScriptRoot/unity-utils.ps1
$unity_path = FindNewestUnity

# Start up Unity to create the appropriate .meta files
Start-Process -FilePath $unity_path -ArgumentList "-quit -batchmode -nographics -logFile - -projectPath `"samples/unity-of-bugs/`"" -Wait

& "./scripts/pack.ps1"
& "./test/Scripts.Tests/test-pack-contents.ps1" "accept"