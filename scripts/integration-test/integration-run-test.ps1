param($path)

. ./scripts/integration-test/IntegrationGlobals.ps1

ShowIntroAndValidateRequiredPaths "True" "Run test" $path
# ============= STEP 3/5 RUN PROJECT

$testProcess = Start-Process -FilePath "$NewProjectBuildPath/Test.exe" -PassThru
WaitProgramToClose $testProcess

If ($testProcess.ExitCode -eq 200) {
    Write-Output " PASSED"   
    ShowCheck
}
Else 
{
    Write-Error "Test process failed with status code $($testProcess.ExitCode)"
}
