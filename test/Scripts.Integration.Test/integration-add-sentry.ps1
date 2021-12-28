param($arg)

. ./test/Scripts.Integration.Test/IntegrationGlobals.ps1

$unityPath = FormatUnityPath $arg

ClearUnityLog

Write-Host -NoNewline "Injecting Editor script"
Copy-Item "$IntegrationScriptsPath/SentrySetup.cs"      -Destination "$NewProjectAssetsPath/Editor"
Copy-Item "$IntegrationScriptsPath/SentrySetup.cs.meta" -Destination "$NewProjectAssetsPath/Editor"
Write-Output " OK"

Write-Host -NoNewline "Starting Unity proccess:"
$UnityProcess = Start-Process -FilePath $unityPath -ArgumentList "-batchmode", "-projectPath ", "$NewProjectPath", "-logfile", "$NewProjectLogPath", "-installSentry", "Git" -PassThru
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
    Write-Output ""
    Write-Output "Sentry added!!"
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