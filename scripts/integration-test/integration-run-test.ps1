param($path)

. ./scripts/integration-test/IntegrationGlobals.ps1

ShowIntroAndValidateRequiredPaths "True" "Run test" $path
# ============= STEP 3/5 RUN PROJECT

$testAppPath = "$NewProjectBuildPath/$Global:TestApp"

If ($IsMacOS)
{
    $testAppPath = $testAppPath  + "/Contents/MacOS/IntegrationTest"
}

$testProcess = Start-Process -FilePath "$testAppPath" -PassThru -ErrorAction Stop 

WaitProgramToClose $testProcess

If ($testProcess.ExitCode -eq 200) {
    Write-Output " PASSED"   
    ShowCheck
}
Else 
{
    Throw "Test process failed with status code $($testProcess.ExitCode) $($testProcess.HasExited)"
}
