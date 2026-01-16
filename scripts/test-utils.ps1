# Shared test utilities for Unity test result parsing

<#
.SYNOPSIS
    Parses NUnit XML test results from Unity test runner.

.DESCRIPTION
    Reads an NUnit XML test results file and returns a hashtable with test statistics
    and details about failed tests.

.PARAMETER Path
    Path to the NUnit XML test results file.

.OUTPUTS
    Hashtable with: Total, Passed, Failed, Inconclusive, Skipped, Duration, Success, FailedTests
    Returns $null if the file doesn't exist or cannot be parsed.

.EXAMPLE
    $results = Parse-TestResults "artifacts/test/playmode/results.xml"
    if ($results.Success) { Write-Host "All tests passed!" }
#>
function Parse-TestResults([string] $Path) {
    if (-not (Test-Path $Path)) {
        return $null
    }

    try {
        [xml]$xml = Get-Content $Path
    }
    catch {
        Write-Host "  Failed to parse XML: $_" -ForegroundColor Red
        return $null
    }

    $testRun = $xml.'test-run'

    if ($null -eq $testRun) {
        Write-Host "  Invalid test results XML" -ForegroundColor Red
        return $null
    }

    $result = @{
        Total        = [int]$testRun.total
        Passed       = [int]$testRun.passed
        Failed       = [int]$testRun.failed
        Inconclusive = [int]$testRun.inconclusive
        Skipped      = [int]$testRun.skipped
        Duration     = [double]$testRun.duration
        Success      = $testRun.result -eq "Passed"
        FailedTests  = @()
    }

    # Collect failed test details
    if ($result.Failed -gt 0) {
        $failedNodes = $xml.SelectNodes("//test-case[@result='Failed']")
        $result.FailedTests = @($failedNodes | ForEach-Object {
                $msg = $null
                if ($_.failure -and $_.failure.message) {
                    $msg = $_.failure.message.InnerText
                }
                @{
                    Name    = $_.fullname
                    Message = $msg
                }
            })
    }

    return $result
}
