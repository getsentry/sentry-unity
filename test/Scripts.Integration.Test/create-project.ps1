param(
    [string] $UnityPath
)

if (-not $Global:NewProjectPathCache)
{
    . ./test/Scripts.Integration.Test/globals.ps1
}

$UnityPath = FormatUnityPath $UnityPath

# Delete previous integration test project folder if found
If (Test-Path -Path "$(GetNewProjectPath)" )
{
    Write-Host -NoNewline "Removing previous integration test:"
    Remove-Item -LiteralPath "$(GetNewProjectPath)" -Force -Recurse
    Write-Host " OK"
}

Write-Host -NoNewline "Creating directory for integration test: '$(GetNewProjectName)'"
New-Item -Path "$(ProjectRoot)/samples" -Name $(GetNewProjectName) -ItemType "directory"
Write-Host " OK"

Write-Host "Creating integration project:"
RunUnityCustom $UnityPath @("-batchmode", "-createProject", "$(GetNewProjectPath)", "-quit")

Write-Host "Copying Editor scripts to integration project:"
New-Item -Path "$(GetNewProjectAssetsPath)" -Name "Editor" -ItemType "directory"
Copy-Item -Recurse "$IntegrationScriptsPath/Editor/*" -Destination "$(GetNewProjectAssetsPath)/Editor/" `
    -Exclude "BuildTimeOptions.cs"
New-Item -Path "$(GetNewProjectAssetsPath)" -Name "Scenes" -ItemType "directory"
Copy-Item -Recurse "$IntegrationScriptsPath/Scenes/*" -Destination "$(GetNewProjectAssetsPath)/Scenes/"
Write-Host " OK"

# Update ProjectSettings
$projectSettingsPath = "$(GetNewProjectPath)/ProjectSettings/ProjectSettings.asset"
$projectSettings = Get-Content $projectSettingsPath
# Don't print stack traces in debug logs. See ./samples/unity-of-bugs/ProjectSettings/PresetManager.asset
$projectSettings = $projectSettings -replace "m_StackTraceTypes: ?[01]+", "m_StackTraceTypes: 010000000000000000000000000000000100000001000000"
# Build Android for x86_64 - for the emulator
$projectSettings = $projectSettings -replace "AndroidTargetArchitectures: ?[0-9]+", "AndroidTargetArchitectures: 8"
# Build for iOS Simulator
$projectSettings = $projectSettings -replace "iPhoneSdkVersion: ?[0-9]+", "iPhoneSdkVersion: 989"
$projectSettings | Out-File $projectSettingsPath

# Add Unity UI package to manifest.json
Write-Host "Adding Unity UI package to manifest.json:"
$manifestPath = "$(GetNewProjectPath)/Packages/manifest.json"
$manifest = Get-Content $manifestPath | ConvertFrom-Json
$manifest.dependencies | Add-Member -MemberType NoteProperty -Name "com.unity.ugui" -Value "2.0.0"
$manifest | ConvertTo-Json -Depth 10 | Out-File $manifestPath -Encoding utf8
Write-Host " OK"

Write-Host "`nProject created!!"
