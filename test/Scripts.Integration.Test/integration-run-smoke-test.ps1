. ./test/Scripts.Integration.Test/IntegrationGlobals.ps1


If ($IsMacOS)
{
    $testAppPath = "$NewProjectBuildPath/test.app/Contents/MacOS/$NewProjectName"
}
ElseIf ($IsWindows)
{
    $testAppPath = "$NewProjectBuildPath/test.app/Contents/MacOS/test.exe"
}
ElseIf ($IsLinux) {
    $testAppPath = "$NewProjectBuildPath/test"
    ls -alh $testAppPath
    # $testAppPath = $testAppPath + "/Contents/MacOS/test.exe"
}
Else
{
    Throw "Unsupported build"
}

$process = Start-Process -FilePath "$testAppPath"  -ArgumentList "--test", "smoke" -PassThru

If ($null -eq $process)
{
    Throw "Process not found."
}

# Wait for 1 minute (sleeps for 500ms per iteration)
$timeout = 60 * 2
$processName = $process.Name
Write-Host -NoNewline "Waiting for $processName"

While (!$process.HasExited -and $timeout -gt 0)
{
    Start-Sleep -Milliseconds 500
    Write-Host -NoNewline "."
    $timeout--
}

# ExitCode 200 is the status code indicating success inside SmokeTest.cs
If ($process.ExitCode -eq 200)
{
    Write-Host "`nPASSED"
}
ElseIf ($timeout -eq 0)
{
    Throw "Test process timed out."
}
Else
{
    Throw "Test process failed with status code $($process.ExitCode)."
}
