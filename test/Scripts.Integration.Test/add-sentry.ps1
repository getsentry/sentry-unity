param(
    [Parameter(Mandatory = $true)][string] $UnityPath,
    [Parameter(Mandatory = $true)][string] $PackagePath
)

if (-not $Global:NewProjectPathCache)
{
    . ./test/Scripts.Integration.Test/globals.ps1
}

. ./test/Scripts.Integration.Test/common.ps1

$UnityPath = FormatUnityPath $UnityPath

Write-Log "Installing Sentry package..."
RunUnityAndExpect $UnityPath "AddSentryPackage" "Sentry Package Installation: SUCCESS" @( `
        "-batchmode", "-projectPath", "$(GetNewProjectPath)", `
        "-installSentry", "Disk", `
        "-sentryPackagePath", $PackagePath)

Write-Log "Copying Integration Test Files..."
New-Item -Path "$(GetNewProjectAssetsPath)" -Name "Scripts" -ItemType "directory" | Out-Null
Copy-Item -Recurse "$IntegrationScriptsPath/Scripts/*" -Destination "$(GetNewProjectAssetsPath)/Scripts/"
Write-PhaseSuccess "Sentry added"
