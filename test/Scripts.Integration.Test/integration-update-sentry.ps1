param($arg)

. ./test/Scripts.Integration.Test/IntegrationGlobals.ps1 

$unityPath = FormatUnityPath $arg

If (-not(Test-Path -Path "$PackageReleaseOutput")) 
{
    Throw "Path $PackageReleaseOutput does not exist. Be sure to run ./test/Scripts.Integration.Test/integration-create-project."
}

ClearUnityLog

Write-Host -NoNewline "Starting Unity process:"
$UnityProcess = Start-Process -FilePath $unityPath -ArgumentList "-batchmode", "-projectPath ", "$NewProjectPath", "-logfile", "$NewProjectLogPath", "-installSentry", "Disk" -PassThru
Write-Output " OK"

WaitForLogFile 30
$successMessage =  "Sentry Package Installation:"

Write-Output "Waiting for Unity to add Sentry to  the project."
$stdout = SubscribeToUnityLogFile $UnityProcess $successMessage "Sentry setup: FAILED"

Write-Output $stdout
If ($UnityProcess.ExitCode -ne 0)
{
    $exitCode = $UnityProcess.ExitCode
    Throw "Unity exited with code $exitCode"
}
ElseIf ($null -ne ($stdout | select-string $successMessage))
{
    Write-Output "`nSentry added!!"
}
Else
{
    Throw "Unity exited but failed to add Sentry package."
}

Write-Host -NoNewline "Updating test files "
Remove-Item -Path "$NewProjectAssetsPath/Scripts/SmokeTester.cs"
Remove-Item -Path "$NewProjectAssetsPath/Scripts/SmokeTester.cs.meta"
Copy-Item "$UnityOfBugsPath/Assets/Scripts/SmokeTester.cs"      -Destination "$NewProjectAssetsPath/Scripts"
Copy-Item "$UnityOfBugsPath/Assets/Scripts/SmokeTester.cs.meta" -Destination "$NewProjectAssetsPath/Scripts"
Write-Output " OK"