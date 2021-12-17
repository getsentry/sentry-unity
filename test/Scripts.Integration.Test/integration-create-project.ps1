param($path)

. ./test/Scripts.Integration.Test/IntegrationGlobals.ps1

ShowIntroAndValidateRequiredPaths "True" "Create project" $path

# Check if Unity path is correct.
If (Test-Path -Path "$Global:UnityPath/$Unity" ) 
{
    Write-Output "Found Unity"
}
Else
{
    Throw "Expected Unity on $Global:UnityPath/$Unity but it was not found."
}

# Check if SDK is packed.
If (Test-Path -Path "$(ProjectRoot)/package-release.zip" ) 
{
    Write-Output "Found package-release.zip"
}
Else
{
    Throw "sentry-release.zip on $(ProjectRoot) but it was not found. Be sure you run ./scripts/pack.ps1"
}

# Clear previous extrated package
Write-Host -NoNewline "clearing $PackageReleaseOutput and Extracting package-release.zip :"
if (Test-Path -Path "$PackageReleaseOutput")
{
    Remove-Item -Path "$PackageReleaseOutput" -Force -Recurse -ErrorAction Stop
}

Expand-Archive -LiteralPath "$(ProjectRoot)/package-release.zip" -DestinationPath "$PackageReleaseOutput" -ErrorAction Stop
Write-Output "OK"

# Delete Previous Integration Project Folder if found
If (Test-Path -Path "$NewProjectPath" ) 
{
    Write-Host -NoNewline "Removing previous integration test:"
    Remove-Item -LiteralPath "$NewProjectPath" -Force -Recurse -ErrorAction Stop
    Write-Output " OK"
}

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

If ($UnityProcess.ExitCode -ne 0)
{
    Throw "Unity exited with code $($UnityProcess.ExitCode)"
}
Else
{
    Write-Host -NoNewline  "Copying Test scene"
    Copy-Item "$UnityOfBugsPath/Assets/Scenes/1_Bugfarm.unity" -Destination "$NewProjectAssetsPath/1_Bugfarm.unity" -ErrorAction Stop
    Copy-Item "$UnityOfBugsPath/Assets/Scenes/1_Bugfarm.unity.meta" -Destination "$NewProjectAssetsPath/1_Bugfarm.unity.meta" -ErrorAction Stop
    Write-Output " OK"
    
    Write-Host -NoNewline  "Copying Scripts"
    $stdout = New-Item -Path "$NewProjectAssetsPath" -Name "Scripts" -ItemType "directory" -ErrorAction Stop
    Copy-Item "$IntegrationScriptsPath/SmokeTester.cs" -Destination "$NewProjectAssetsPath/Scripts" -ErrorAction Stop
    Copy-Item "$IntegrationScriptsPath/SmokeTester.cs.meta" -Destination "$NewProjectAssetsPath/Scripts" -ErrorAction Stop
    Write-Output " OK"
    
    Write-Host -NoNewline "Applying Scene to EditorBuildSettings: "
    $EditorBuildSettings = Get-Content -path "$NewProjectSettingsPath/EditorBuildSettings.asset" -ErrorAction Stop
    $EditorBuildSettings = $EditorBuildSettings.Replace("m_Scenes: []", "m_Scenes:`n  - enabled: 1`n    path: Assets/1_Bugfarm.unity")
    $EditorBuildSettings | Set-Content -Path "$NewProjectSettingsPath/EditorBuildSettings.asset" -ErrorAction Stop
    Write-Output " OK"

    Write-Output "`nProject created!!"
}