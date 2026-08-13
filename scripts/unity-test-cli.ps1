. $PSScriptRoot/unity-pipeline.ps1
. $PSScriptRoot/test-utils.ps1

function Invoke-UnityHeadlessTest(
    [string] $projectPath,
    [string] $testMode,
    [string] $resultsDirectory,
    [int] $timeoutSeconds,
    [string] $filter
) {
    New-Item -ItemType Directory -Force -Path $resultsDirectory | Out-Null
    $resultPath = Join-Path $resultsDirectory "$($testMode.ToLowerInvariant()).xml"
    Remove-Item $resultPath -Force -ErrorAction Ignore

    $arguments = @("test", $projectPath, "--mode", $testMode, "--output", $resultPath, "--timeout", $timeoutSeconds)
    if ($filter) {
        $arguments += "--filter", ".*$([regex]::Escape($filter)).*"
    }

    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    $run = Invoke-UnityCli $arguments
    $stopwatch.Stop()

    $summary = Parse-TestResults $resultPath
    if ($null -eq $summary) {
        throw "Unity CLI $testMode tests did not produce valid results at ${resultPath}:`n$($run.Output)"
    }

    if ($summary.Total -eq 0) {
        throw "Unity CLI $testMode test results are empty."
    }

    if ($run.ExitCode -ne 0 -and $summary.Success) {
        throw "Unity CLI $testMode tests exited with code $($run.ExitCode):`n$($run.Output)"
    }

    return [pscustomobject]@{
        Mode         = $testMode
        Duration     = $stopwatch.Elapsed.TotalSeconds
        Total        = $summary.Total
        Passed       = $summary.Passed
        Failed       = $summary.Failed
        Skipped      = $summary.Skipped
        Inconclusive = $summary.Inconclusive
        FailedTests  = $summary.FailedTests
        Success      = $run.ExitCode -eq 0 -and $summary.Success
    }
}
