<#
.SYNOPSIS
    Reports Unity test results from NUnit XML file.

.DESCRIPTION
    Parses NUnit XML test results and prints a summary. Exits with code 1 if tests failed.
    Designed to be called from MSBuild targets.

.PARAMETER Path
    Path to the NUnit XML test results file.

.EXAMPLE
    pwsh scripts/report-test-results.ps1 artifacts/test/playmode/results.xml
#>

param(
    [Parameter(Mandatory = $true, Position = 0)]
    [string] $Path
)

Set-StrictMode -Version latest
$ErrorActionPreference = "Stop"

. $PSScriptRoot/test-utils.ps1

if (-not (Test-Path $Path)) {
    Write-Host "Test results file not found at $Path" -ForegroundColor Red
    exit 1
}

$results = Parse-TestResults $Path

if ($null -eq $results) {
    Write-Host "Failed to parse test results" -ForegroundColor Red
    exit 1
}

if ($results.Total -eq 0) {
    Write-Host "Unity test results is empty." -ForegroundColor Red
    exit 1
}

# Print summary (matching format from original C# implementation)
$status = if ($results.Success) { "Passed" } else { "Failed" }
Write-Host "$status in $($results.Duration)s"
Write-Host ("        Passed: {0,3}" -f $results.Passed)
Write-Host ("        Failed: {0,3}" -f $results.Failed)
Write-Host ("       Skipped: {0,3}" -f $results.Skipped)
Write-Host ("  Inconclusive: {0,3}" -f $results.Inconclusive)

# Print failed test details
if ($results.Failed -gt 0) {
    Write-Host ""
    
    # Re-parse to get stack traces (not included in Parse-TestResults)
    [xml]$xml = Get-Content $Path
    $failedNodes = $xml.SelectNodes("//test-case[@result='Failed']")
    
    foreach ($node in $failedNodes) {
        # Skip parent test-cases that contain child test-cases
        if ($node.SelectNodes(".//test-case").Count -gt 0) {
            continue
        }
        
        $name = $node.GetAttribute("name")
        $id = $node.GetAttribute("id")
        Write-Host "Test $id`: $name"
        
        $message = $node.SelectSingleNode("failure/message")
        if ($message) {
            Write-Host $message.InnerText
        }
        
        $stackTrace = $node.SelectSingleNode("failure/stack-trace")
        if ($stackTrace) {
            Write-Host "Test StackTrace:"
            Write-Host $stackTrace.InnerText
        }
        
        Write-Host ""
    }
    
    $testWord = if ($results.Failed -gt 1) { "tests" } else { "test" }
    Write-Host "Test run completed with $($results.Failed) failing $testWord." -ForegroundColor Red
}

# Exit based on overall success (handles edge cases where result != "Passed" but failed count is 0)
if (-not $results.Success) {
    exit 1
}
exit 0
