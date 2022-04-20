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
$UnityProcess = RunUnity $UnityPath @("-batchmode", "-createProject", "$NewProjectPath", "-logfile", "$NewProjectLogPath", "-quit")

WaitForLogFile 30

Write-Host "Waiting for Unity to create the project."
SubscribeToUnityLogFile $UnityProcess

If ($UnityProcess.ExitCode -ne 0)
{
    Throw "Unity exited with code $($UnityProcess.ExitCode)"
}
Else
{
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

    # Don't print stack traces in debug logs. See ./samples/unity-of-bugs/ProjectSettings/PresetManager.asset
    $projectSettingsPath = "$NewProjectPath/ProjectSettings/ProjectSettings.asset"
    $projectSettings = Get-Content $projectSettingsPath
    $projectSettings = $projectSettings -replace "m_StackTraceTypes: ?[01]+", "m_StackTraceTypes: 010000000000000000000000000000000100000001000000"
    $projectSettings = $projectSettings -replace "AndroidTargetArchitectures: ?[01]+", "AndroidTargetArchitectures: 4" # x86_64 for the emulator
    $projectSettings | Out-File $projectSettingsPath

    Write-Host "`nProject created!!"
}
