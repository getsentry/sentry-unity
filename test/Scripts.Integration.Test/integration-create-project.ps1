param($arg)

. ./test/Scripts.Integration.Test/IntegrationGlobals.ps1

$unityPath = FormatUnityPath $arg

# Check if Unity path is correct.
If (Test-Path -Path "$unityPath")
{
    Write-Host "Found Unity"
}
Else
{
    Throw "Expected Unity on $unityPath but it was not found."
}

# Delete Previous Integration Project Folder if found
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

Write-Host -NoNewline "Creating integration project:"
$UnityProcess = RunUnity $unityPath @("-batchmode", "-createProject", "$NewProjectPath", "-logfile", "$NewProjectLogPath", "-quit")
Write-Host " OK"

WaitForLogFile 30

Write-Host "Waiting for Unity to create the project."
SubscribeToUnityLogFile $UnityProcess

If ($UnityProcess.ExitCode -ne 0)
{
    Throw "Unity exited with code $($UnityProcess.ExitCode)"
}
Else
{
    Write-Host -NoNewline  "Copying Test scene"
    New-Item -Path "$NewProjectAssetsPath/Scenes" -Name $NewProjectName -ItemType "directory"
    Copy-Item -Recurse "$UnityOfBugsPath/Assets/Scenes/*" -Destination "$NewProjectAssetsPath/Scenes/"
    Write-Host " OK"

    Write-Host -NoNewline  "Copying Scripts"
    $stdout = New-Item -Path "$NewProjectAssetsPath" -Name "Scripts" -ItemType "directory"
    $stdout = New-Item -Path "$NewProjectAssetsPath" -Name "Editor" -ItemType "directory"
    Copy-Item -Recurse "$IntegrationScriptsPath/SmokeTester.*" -Destination "$NewProjectAssetsPath/Scripts/"
    Copy-Item -Recurse "$UnityOfBugsPath/Assets/Editor/*" -Destination "$NewProjectAssetsPath/Editor/"
    Copy-Item -Recurse "$IntegrationScriptsPath/SentrySetup.*" -Destination "$NewProjectAssetsPath/Editor/"
    Write-Host " OK"

    # Don't print stack traces in debug logs. See ./samples/unity-of-bugs/ProjectSettings/PresetManager.asset
    $projectSettingsPath = "$NewProjectPath/ProjectSettings/ProjectSettings.asset"
    (Get-Content $projectSettingsPath) -replace "m_StackTraceTypes: ?[01]+", "m_StackTraceTypes: 010000000000000000000000000000000100000001000000" | `
        Out-File $projectSettingsPath

    Write-Host "`nProject created!!"
}
