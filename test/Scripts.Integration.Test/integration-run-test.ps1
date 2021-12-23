. ./test/Scripts.Integration.Test/IntegrationGlobals.ps1

# ShowIntroAndValidateRequiredPaths $true "Run test" $path

$testAppPath = "$NewProjectBuildPath/$Global:TestApp"

If ($IsMacOS)
{
    $testAppPath = $testAppPath + "/Contents/MacOS/$NewProjectName"
}

$process = Start-Process -FilePath "$testAppPath"  -ArgumentList "--test", "smoke" -PassThru 

If ($null -eq $process) 
{
    Throw "Process not found."
}

$timeout = 60 * 2
$processName = $process.Name
Write-Host -NoNewline "Waiting for $processName"

While (!$process.HasExited -and $timeout -gt 0) 
{
    Start-Sleep -Milliseconds 500
    Write-Host -NoNewline "."
    $timeout-- #Validate
}

If ($process.ExitCode -eq 200) 
{
    Write-Output "`nPASSED"   
}
Else 
{
    Throw "Test process failed with status code $($process.ExitCode)"
}