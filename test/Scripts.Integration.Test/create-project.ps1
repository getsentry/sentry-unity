param(
    [string] $UnityPath
)

. ./test/Scripts.Integration.Test/globals.ps1

$UnityPath = FormatUnityPath $UnityPath

# Delete previous integration test project folder if found
If (Test-Path -Path "$NewProjectPath" )
{
    Write-Host -NoNewline "Removing previous integration test:"
    Remove-Item -LiteralPath "$NewProjectPath" -Force -Recurse
    Write-Host " OK"
}

Write-Host -NoNewline "Creating directory for integration test:"
New-Item -Path "$(ProjectRoot)/samples" -Name $NewProjectName -ItemType "directory"
Write-Host " OK"

Write-Host "Creating integration project:"
RunUnityCustom $UnityPath @("-batchmode", "-createProject", "$NewProjectPath", "-quit")

Write-Host "Copying Editor scripts to integration project:"
New-Item -Path "$NewProjectAssetsPath" -Name "Editor" -ItemType "directory"
Copy-Item -Recurse "$IntegrationScriptsPath/Editor/*" -Destination "$NewProjectAssetsPath/Editor/"
New-Item -Path "$NewProjectAssetsPath" -Name "Scenes" -ItemType "directory"
Copy-Item -Recurse "$IntegrationScriptsPath/Scenes/*" -Destination "$NewProjectAssetsPath/Scenes/"
Write-Host " OK"

# Update ProjectSettings
$projectSettingsPath = "$NewProjectPath/ProjectSettings/ProjectSettings.asset"
$projectSettings = Get-Content $projectSettingsPath
# Don't print stack traces in debug logs. See ./samples/unity-of-bugs/ProjectSettings/PresetManager.asset
$projectSettings = $projectSettings -replace "m_StackTraceTypes: ?[01]+", "m_StackTraceTypes: 010000000000000000000000000000000100000001000000"
# Build Android for x86_64 - for the emulator
$projectSettings = $projectSettings -replace "AndroidTargetArchitectures: ?[0-9]+", "AndroidTargetArchitectures: 8"
# Build for iOS Simulator
$projectSettings = $projectSettings -replace "iPhoneSdkVersion: ?[0-9]+", "iPhoneSdkVersion: 989"
$projectSettings | Out-File $projectSettingsPath

Write-Host "`nProject created!!"
