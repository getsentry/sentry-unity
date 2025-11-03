param(
    [string] $UnityPath
)

if (-not $Global:NewProjectPathCache)
{
    . ./test/Scripts.Integration.Test/globals.ps1
}

. ./test/Scripts.Integration.Test/common.ps1

$UnityPath = FormatUnityPath $UnityPath

RunUnityAndExpect $UnityPath "AddSentryPackage" "Sentry Package Installation: SUCCESS" @( `
        "-batchmode", "-projectPath ", "$(GetNewProjectPath)", "-installSentry", "Disk")

Write-Log "Copying Integration Test Files"
New-Item -Path "$(GetNewProjectAssetsPath)" -Name "Scripts" -ItemType "directory"
Copy-Item -Recurse "$IntegrationScriptsPath/Scripts/*" -Destination "$(GetNewProjectAssetsPath)/Scripts/"
