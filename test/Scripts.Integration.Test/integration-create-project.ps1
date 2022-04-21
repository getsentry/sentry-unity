param(
    [string] $UnityPath
)

. ./test/Scripts.Integration.Test/IntegrationGlobals.ps1

$UnityPath = FormatUnityPath $UnityPath

# # Delete Previous Integration Project Folder if found
If (Test-Path -Path "$NewProjectPath" )
{
    Write-Host -NoNewline "Removing previous integration test:"
    Remove-Item -LiteralPath "$NewProjectPath" -Force -Recurse
    Write-Host " OK"
}

ClearUnityLog

Write-Host -NoNewline "Creating directory for integration test:"
New-Item -Path "$(ProjectRoot)/samples" -Name $NewProjectName -ItemType "directory"
Write-Host " OK"

Write-Host "Creating integration project:"
RunUnity $UnityPath @("-batchmode", "-createProject", "$NewProjectPath", "-logfile", "$NewProjectLogPath", "-quit") > $null

Write-Host -NoNewline "Copying Test scene"
New-Item -Path "$NewProjectAssetsPath/Scenes" -Name $NewProjectName -ItemType "directory"
Copy-Item -Recurse "$UnityOfBugsPath/Assets/Scenes/*" -Destination "$NewProjectAssetsPath/Scenes/"
Write-Host " OK"

Write-Host -NoNewline "Copying Scripts"
$stdout = New-Item -Path "$NewProjectAssetsPath" -Name "Scripts" -ItemType "directory"
$stdout = New-Item -Path "$NewProjectAssetsPath" -Name "Editor" -ItemType "directory"
Copy-Item -Recurse "$IntegrationScriptsPath/SmokeTester.*" -Destination "$NewProjectAssetsPath/Scripts/"
Copy-Item -Recurse "$UnityOfBugsPath/Assets/Editor/*" -Destination "$NewProjectAssetsPath/Editor/"
Copy-Item -Recurse "$IntegrationScriptsPath/SentrySetup.*" -Destination "$NewProjectAssetsPath/Editor/"
Write-Host " OK"

# Update ProjectSettings
$projectSettingsPath = "$NewProjectPath/ProjectSettings/ProjectSettings.asset"
$projectSettings = Get-Content $projectSettingsPath
# Don't print stack traces in debug logs. See ./samples/unity-of-bugs/ProjectSettings/PresetManager.asset
$projectSettings = $projectSettings -replace "m_StackTraceTypes: ?[01]+", "m_StackTraceTypes: 010000000000000000000000000000000100000001000000"
# Build Android for x86_64 - for the emulator
$projectSettings = $projectSettings -replace "AndroidTargetArchitectures: ?[0-9]+", "AndroidTargetArchitectures: 4"
$projectSettings | Out-File $projectSettingsPath

Write-Host "`nProject created!!"
