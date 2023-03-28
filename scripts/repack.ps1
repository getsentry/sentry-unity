assemblyalias --target-directory "package-dev/Runtime" --internalize --prefix "Sentry." --assemblies-to-alias "Microsoft*;System*"
assemblyalias --target-directory "package-dev/Editor" --internalize --prefix "Sentry." --assemblies-to-alias "Microsoft*;Mono.Cecil*"

$unity_version = Get-Content ./samples/unity-of-bugs/ProjectSettings/ProjectVersion.txt | grep m_EditorVersion -m 1 | cut -d ' ' -f 2

if ($IsWindows){
    $unity_path = "C:\Program Files\Unity\Hub\Editor\${unity_version}\Editor\Unity.exe"
}
else {
    $unity_path = "/Applications/Unity/Hub/Editor/$unity_version/Unity.app/Contents/MacOS/Unity"
}

# Start up Unity to create the appropriate .meta files
Start-Process -FilePath $unity_path -ArgumentList "-quit -batchmode -nographics -logFile - -projectPath `"samples/unity-of-bugs/`"" -Wait

& "./scripts/pack.ps1"
& "./test/Scripts.Tests/test-pack-contents.ps1" "accept"