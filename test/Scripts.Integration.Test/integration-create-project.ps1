param($arg)

. ./test/Scripts.Integration.Test/IntegrationGlobals.ps1

$unityPath = FormatUnityPath $arg

# Check if Unity path is correct.
If (Test-Path -Path "$unityPath")
{
    Write-Output "Found Unity"
}
Else
{
    Throw "Expected Unity on $unityPath but it was not found."
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
    Remove-Item -Path "$PackageReleaseOutput" -Recurse
}

Expand-Archive -LiteralPath "$(ProjectRoot)/package-release.zip" -DestinationPath "$PackageReleaseOutput"
Write-Output "OK"

# Delete Previous Integration Project Folder if found
If (Test-Path -Path "$NewProjectPath" ) 
{
    Write-Host -NoNewline "Removing previous integration test:"
    Remove-Item -LiteralPath "$NewProjectPath" -Force -Recurse
    Write-Output " OK"
}

ClearUnityLog

# Create Integration Project Folder
Write-Host -NoNewline "Creating directory for integration test:"
New-Item -Path "$(ProjectRoot)/samples" -Name $NewProjectName -ItemType "directory"
Write-Output " OK"

# Create New Unity Project
Write-Host -NoNewline "Creating integration project:"
$UnityProcess = Start-Process -FilePath $unityPath -ArgumentList "-batchmode", "-createProject", "$NewProjectPath", "-logfile", "$NewProjectLogPath", "-quit" -PassThru
Write-Output " OK"

# Track log
WaitForLogFile 30

Write-Output "Waiting for Unity to create the project."
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
    Write-Output " OK"
    
    Write-Host -NoNewline  "Copying Scripts"
    $stdout = New-Item -Path "$NewProjectAssetsPath" -Name "Scripts" -ItemType "directory"
    $stdout = New-Item -Path "$NewProjectAssetsPath" -Name "Editor" -ItemType "directory"
    Copy-Item -Recurse "$IntegrationScriptsPath/SmokeTester.*" -Destination "$NewProjectAssetsPath/Scripts/"
    Copy-Item -Recurse "$UnityOfBugsPath/Assets/Editor/*" -Destination "$NewProjectAssetsPath/Editor/"
    Copy-Item -Recurse "$IntegrationScriptsPath/SentrySetup.*" -Destination "$NewProjectAssetsPath/Editor/"
    Write-Output " OK"

    Write-Output "`nProject created!!"
}