param($path)

. ./scripts/integration-test/IntegrationGlobals.ps1

ShowIntroAndValidateRequiredPaths "True" "Run test" $path
# ============= STEP 3/5 RUN PROJECT

If ($IsMacOS)
{
    If (Test-Path -Path "$NewProjectBuildPath/test.txt")
    {
        Remove-Item "$NewProjectBuildPath/test.txt"
    }
    
    #https://stackoverflow.com/questions/66495254/opening-app-on-macos-11-big-sur-from-javafx-application-randomly-fails-with-klsn
    /System/Library/Frameworks/CoreServices.framework/Frameworks/LaunchServices.framework/Support/lsregister -f "$NewProjectBuildPath/$Global:TestApp"
    $testProcess = Start-Process -FilePath "$NewProjectBuildPath/$Global:TestApp" -PassThru -ErrorAction Stop

    #Start-Process does not give the test app hook so we need to hook to it with Get-Process once it started.
    $testProcess.WaitForExit()
    $testProcess = Get-Process "IntegrationTest"
}
Else
{
    $testProcess = Start-Process -FilePath "$NewProjectBuildPath/$Global:TestApp" -PassThru -ErrorAction Stop 
}

WaitProgramToClose $testProcess

If ($testProcess.ExitCode -eq 200) {
    Write-Output " PASSED"   
    ShowCheck
}
ElseIf ($IsMacOS -and (Get-Content -Path "$NewProjectBuildPath/test.txt" -TotalCount 1) -eq "200")
{
    #.APP does not return the Exit code so we relly on a file wrote with the status code from the Integration test.
    Write-Output " PASSED"   
    ShowCheck
}
Else 
{
    Throw "Test process failed with status code $($testProcess.ExitCode) $($testProcess.HasExited)"
}
