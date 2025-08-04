# Clean up previous release artifacts
if (Test-Path "package-release") {
    Remove-Item -Path "package-release" -Recurse -Force
    Write-Host "Removed package-release directory"
}

if (Test-Path "package-release.zip") {
    Remove-Item -Path "package-release.zip" -Force
    Write-Host "Removed package-release.zip file"
}

assemblyalias --target-directory "package-dev/Runtime" --internalize --prefix "Sentry." --assemblies-to-alias "Microsoft*;System*"
assemblyalias --target-directory "package-dev/Editor" --internalize --prefix "Sentry." --assemblies-to-alias "Microsoft*;Mono.Cecil*"

if ($IsWindows) {
    $unity_versions = Get-ChildItem "C:\Program Files\Unity\Hub\Editor\" -Directory | Select-Object -ExpandProperty Name
    $unity_version = $unity_versions | Sort-Object -Descending | Select-Object -First 1
    $unity_path = "C:\Program Files\Unity\Hub\Editor\${unity_version}\Editor\Unity.exe"
}
else {
    $unity_versions = Get-ChildItem "/Applications/Unity/Hub/Editor/" -Directory | Select-Object -ExpandProperty Name 
    $unity_version = $unity_versions | Sort-Object -Descending | Select-Object -First 1
    $unity_path = "/Applications/Unity/Hub/Editor/$unity_version/Unity.app/Contents/MacOS/Unity"
}

# Start up Unity to create the appropriate .meta files
Start-Process -FilePath $unity_path -ArgumentList "-quit -batchmode -nographics -logFile - -projectPath `"samples/unity-of-bugs/`"" -Wait

& "./scripts/pack.ps1"
& "./test/Scripts.Tests/test-pack-contents.ps1" "accept"