param($arg)

. ./test/Scripts.Integration.Test/IntegrationGlobals.ps1

$unityPath = FormatUnityPath $arg

If (-not(Test-Path -Path "$PackageReleaseOutput"))
{
    Throw "Path $PackageReleaseOutput does not exist. Be sure to run ./test/Scripts.Integration.Test/integration-create-project."
}

ClearUnityLog

Write-Host -NoNewline "Starting Unity process:"
$UnityProcess = RunUnity $unityPath @("-batchmode", "-projectPath ", "$NewProjectPath", "-logfile", "$NewProjectLogPath", "-installSentry", "Disk")
Write-Host " OK"

WaitForLogFile 30
$successMessage =  "Sentry Package Installation:"

Write-Host "Waiting for Unity to add Sentry to the project."
$stdout = SubscribeToUnityLogFile $UnityProcess $successMessage "Sentry setup: FAILED"

Write-Host $stdout
If ($UnityProcess.ExitCode -ne 0)
{
    $exitCode = $UnityProcess.ExitCode
    Throw "Unity exited with code $exitCode"
}
ElseIf ($null -ne ($stdout | select-string $successMessage))
{
    Write-Host "`nSentry added!!"
}
Else
{
    Throw "Unity exited but failed to add the Sentry package."
}

Write-Host -NoNewline "Updating test files "
# We were previously using an empty SmokeTester to not generate Build errors.
# It was only required to not cause build errors since the new project did't have Sentry installed.
Remove-Item -Path "$NewProjectAssetsPath/Scripts/SmokeTester.cs"
Remove-Item -Path "$NewProjectAssetsPath/Scripts/SmokeTester.cs.meta"
Copy-Item "$PackageReleaseAssetsPath/Scripts/SmokeTester.cs"      -Destination "$NewProjectAssetsPath/Scripts"
Copy-Item "$PackageReleaseAssetsPath/Scripts/SmokeTester.cs.meta" -Destination "$NewProjectAssetsPath/Scripts"
Copy-Item "$PackageReleaseAssetsPath/Scripts/NativeSupport/CppPlugin.*" -Destination "$NewProjectAssetsPath/Scripts/"
Write-Host " OK"
