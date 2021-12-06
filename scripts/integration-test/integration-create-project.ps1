param($path)

. ./scripts/integration-test/IntegrationGlobals.ps1

Write-Output "Create Project => $function:ProjectRoot"


ShowIntroAndValidateRequiredPaths "True" "Create project" $path
    Write-Output "Unity path is $Global:UnityPath"

# Check if Unity path is correct.
If (Test-Path -Path "$Global:UnityPath/$Unity" ) 
{
    Write-Output "Found Unity"
}
Else
{
    Throw "Expected Unity on $Global:UnityPath/$Unity but it was not found."
}

Write-Output "Create Project => $(ProjectRoot)"


# ============= STEP 1 CREATE NEW PROJECT
# Delete Previous Integration Project Folder if found
If (Test-Path -Path "$NewProjectPath" ) 
{
    Write-Host -NoNewline "Removing previous integration test:"
    Remove-Item -LiteralPath "$NewProjectPath" -Force -Recurse -ErrorAction Stop
    Write-Output " OK"
}
Write-Output "Create Project => $(ProjectRoot)"

# Delete Previous Log File if found
ClearUnityLog

# Create Integration Project Folder
Write-Host -NoNewline "Creating directory for integration test:"
$stdout = New-Item -Path "$(ProjectRoot)/samples" -Name $NewProjectName -ItemType "directory" -ErrorAction Stop
Write-Output " OK"

# Create Integration Project for Unity
Write-Host -NoNewline "Creating integration project:"
$UnityProcess = Start-Process -FilePath "$Global:UnityPath/$Unity" -ArgumentList "-batchmode", "-createProject", "$NewProjectPath", "-logfile", "$NewProjectLogPath/$LogFile", "-quit" -PassThru
Write-Output " OK"

# Track log
WaitLogFileToBeCreated 30

Write-Output "Waiting for Unity to create the project."
TrackCacheUntilUnityClose($UnityProcess)

Write-Host -NoNewline  "Copying Test scene"
Copy-Item "$IntegrationScriptsPath/EmptyScene.unity" -Destination "$NewProjectAssetsPath/EmptyScene.unity" -ErrorAction Stop
Copy-Item "$IntegrationScriptsPath/EmptyScene.unity.meta" -Destination "$NewProjectAssetsPath/EmptyScene.unity.meta" -ErrorAction Stop
Write-Output " OK"

Write-Host -NoNewline  "Copying Scripts"
$stdout = New-Item -Path "$NewProjectAssetsPath" -Name "Scripts" -ItemType "directory" -ErrorAction Stop
Copy-Item "$IntegrationScriptsPath/SmokeTester.cs" -Destination "$NewProjectAssetsPath/Scripts" -ErrorAction Stop
Copy-Item "$IntegrationScriptsPath/SmokeTester.cs.meta" -Destination "$NewProjectAssetsPath/Scripts" -ErrorAction Stop
Write-Output " OK"


Write-Host -NoNewline "Applying Scene to EditorBuildSettings: "
$EditorBuildSettings = Get-Content -path "$NewProjectSettingsPath/EditorBuildSettings.asset" -ErrorAction Stop
$EditorBuildSettings = $EditorBuildSettings.Replace("m_Scenes: []", "m_Scenes:`n  - enabled: 1`n    path: Assets/EmptyScene.unity")
$EditorBuildSettings | Set-Content -Path "$NewProjectSettingsPath/EditorBuildSettings.asset" -ErrorAction Stop
Write-Output " OK"

if ($UnityProcess.ExitCode -ne 0)
{
    Throw "Unity exited with code $($UnityProcess.ExitCode)"
}
else
{
    Write-Output ""
    Write-Output "Project created!!"
    ShowCheck
}