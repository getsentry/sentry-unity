param($path)
$path = "C:/2019.4.31f1/Editor"
. ./test/Scripts.Integration.Test/IntegrationGlobals.ps1

ShowIntroAndValidateRequiredPaths $true "Update Sentry" $path

If (-not(Test-Path -Path "$PackageReleaseOutput")) 
{
    Throw "Path $PackageReleaseOutput does not exist. Be sure to run ./test/Scripts.Integration.Test/integration-create-project."
}

Write-Output "Removing Log"
ClearUnityLog

Write-Host -NoNewline "Injecting Editor script"
$stdout = New-Item -Path "$NewProjectAssetsPath" -Name "Editor" -ItemType "directory"
Copy-Item "$IntegrationScriptsPath/SentryUpdateSetup.cs"      -Destination "$NewProjectAssetsPath/Editor"
Write-Output " OK"

Write-Host -NoNewline "Applying Sentry package to the project:"
$UnityProcess = Start-Process -FilePath "$Global:UnityPath/$Unity" -ArgumentList "-batchmode", "-projectPath ", "$NewProjectPath", "-logfile", "$NewProjectLogPath/$LogFile" -PassThru
Write-Output " OK"

WaitLogFileToBeCreated 30

Write-Output "Waiting for Unity to add Sentry to  the project."
$stdout = TrackCacheUntilUnityClose $UnityProcess "Sentry setup: SUCCESS" "Sentry setup: FAILED"

Write-Output "Removing Editor script"
Remove-Item -LiteralPath "$NewProjectAssetsPath/Editor" -Recurse

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