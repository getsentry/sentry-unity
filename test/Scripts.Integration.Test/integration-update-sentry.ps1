param($arg)

. ./test/Scripts.Integration.Test/IntegrationGlobals.ps1 

$unityPath = FormatUnityPath $arg

If (-not(Test-Path -Path "$PackageReleaseOutput")) 
{
    Throw "Path $PackageReleaseOutput does not exist. Be sure to run ./test/Scripts.Integration.Test/integration-create-project."
}

Write-Output "Removing Log"
ClearUnityLog

Write-Host -NoNewline "Starting Unity proccess:"
$UnityProcess = Start-Process -FilePath $unityPath -ArgumentList "-batchmode", "-projectPath ", "$NewProjectPath", "-logfile", "$NewProjectLogPath", "-installSentry", "Disk" -PassThru
Write-Output " OK"

WaitForLogFile 30

Write-Output "Waiting for Unity to add Sentry to  the project."
$stdout = SubscribeToUnityLogFile $UnityProcess "Sentry setup: SUCCESS" "Sentry setup: FAILED"

Write-Output $stdout
If ($UnityProcess.ExitCode -ne 0)
{
    $exitCode = $UnityProcess.ExitCode
    Throw "Unity exited with code $exitCode"
}
ElseIf ($null -ne ($stdout | select-string "SUCCESS"))
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