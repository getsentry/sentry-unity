param(
    [string] $UnityPath
)

. ./test/Scripts.Integration.Test/globals.ps1
. ./test/Scripts.Integration.Test/common.ps1

$UnityPath = FormatUnityPath $UnityPath

RunUnityAndExpect $UnityPath "AddSentryPackage" "Sentry Package Installation: SUCCESS" @( `
        "-batchmode", "-projectPath ", "$NewProjectPath", "-installSentry", "Disk")

Write-Host -NoNewline "Copying Integration Test Files"
New-Item -Path "$NewProjectAssetsPath" -Name "Scripts" -ItemType "directory"
Copy-Item -Recurse "$IntegrationScriptsPath/Scripts/*" -Destination "$NewProjectAssetsPath/Scripts/"
Copy-Item -Recurse "$IntegrationScriptsPath/Editor/BuildTimeOptions.cs" -Destination "$NewProjectAssetsPath/Editor/"
