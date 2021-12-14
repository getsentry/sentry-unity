param($path)

. ./scripts/integration-test/IntegrationGlobals.ps1

ShowIntroAndValidateRequiredPaths "True" "Build project" $path

Write-Output "Removing Log"
ClearUnityLog

$buildTarget = $null
If ($IsMacOS)
{
    $buildTarget = "-buildOSXUniversalPlayer"
}
ElseIf($IsWindows)
{
    $buildTarget = "-buildWindows64Player"
}
Else
{
    Throw "Unsupported build"
}

Write-Output "Checking if Project has no errors "
Write-Host -NoNewline "Creating integration project:"
$UnityProcess = Start-Process -FilePath "$Global:UnityPath/$Unity" -ArgumentList "-batchmode", "-projectPath ", "$NewProjectPath", "-logfile", "$NewProjectLogPath/$LogFile", $buildTarget , "$NewProjectBuildPath/$Global:TestApp", "-quit" -PassThru
Write-Output " OK"

WaitLogFileToBeCreated 30
Write-Output "Waiting for Unity to build the project."
TrackCacheUntilUnityClose($UnityProcess)

if ($UnityProcess.ExitCode -ne 0)
{
    Throw "Unity exited with code $($UnityProcess.ExitCode)"
}
else
{
    Write-Output "`nProject Built!!"
}