param($path)

. ./scripts/integration-test/IntegrationGlobals.ps1

ShowIntroAndValidateRequiredPaths "True" "Run test" $path

$testAppPath = "$NewProjectBuildPath/$Global:TestApp"

If ($IsMacOS)
{
    $testAppPath = $testAppPath + "/Contents/MacOS/$NewProjectName"
}

$testProcess = Start-Process -FilePath "$testAppPath"  -ArgumentList "--test", "smoke" -PassThru -ErrorAction Stop 

WaitProgramToClose $testProcess

If ($testProcess.ExitCode -eq 200) 
{
    Write-Output "`nPASSED"   
}
Else 
{
    Throw "Test process failed with status code $($testProcess.ExitCode)"
}
