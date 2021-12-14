param($path)

. ./scripts/integration-test/IntegrationGlobals.ps1

ShowIntroAndValidateRequiredPaths "True" "Update Sentry" $path

If (!Test-Path -Path "$NewProjectPath/package-release" ) 
{
    Throw "Path $NewProjectPath/package-release does not exist. Be sure to run ./scripts/pack.ps1 and extract it."
}

Write-Output "Removing Log"
ClearUnityLog

Write-Host -NoNewline "Injecting Editor script"
$stdout = New-Item -Path "$NewProjectAssetsPath" -Name "Editor" -ItemType "directory" -ErrorAction Stop
Copy-Item "$IntegrationScriptsPath/SentryUpdateSetup.cs"      -Destination "$NewProjectAssetsPath/Editor" -ErrorAction Stop
Write-Output " OK"

Write-Host -NoNewline "Applying Sentry package to the project:"
$UnityProcess = Start-Process -FilePath "$Global:UnityPath/$Unity" -ArgumentList "-batchmode", "-projectPath ", "$NewProjectPath", "-logfile", "$NewProjectLogPath/$LogFile" -PassThru
Write-Output " OK"

WaitLogFileToBeCreated 30

Write-Output "Waiting for Unity to add Sentry to  the project."
$stdout = TrackCacheUntilUnityClose $UnityProcess "Sentry setup: SUCCESS" "Sentry setup: FAILED"

Write-Output "Removing Editor script"
Remove-Item -LiteralPath "$NewProjectAssetsPath/Editor" -Force -Recurse -ErrorAction Stop

Write-Output $stdout
If ($UnityProcess.ExitCode -ne 0)
{
    $exitCode = $UnityProcess.ExitCode
    Throw "Unity exited with code $exitCode"
}
ElseIf (($stdout | select-string "SUCCESS") -ne $null)
{
    Write-Output "`nSentry added!!"
}
Else
{
    Throw "Unity exited but failed to add Sentry package."
}

Write-Host -NoNewline "Updating test files "
Remove-Item -Path "$NewProjectAssetsPath/Scripts/SmokeTester.cs" -Force
Remove-Item -Path "$NewProjectAssetsPath/Scripts/SmokeTester.cs.meta" -Force
Copy-Item "$UnityOfBugsPath/Assets/Scripts/SmokeTester.cs"      -Destination "$NewProjectAssetsPath/Scripts" -ErrorAction Stop
Copy-Item "$UnityOfBugsPath/Assets/Scripts/SmokeTester.cs.meta" -Destination "$NewProjectAssetsPath/Scripts" -ErrorAction Stop
Write-Output " OK"