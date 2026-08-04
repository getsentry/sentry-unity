<#
.SYNOPSIS
    Runs Unity tests through a connected Editor or Unity CLI.

.DESCRIPTION
    This local test harness uses an open Unity Pipeline Editor when available.
    When the target project has no running Editor, Unity CLI launches the test
    runner headlessly instead.
#>

param(
    [ValidateSet("All", "EditMode", "PlayMode")]
    [string] $Mode = "All",
    [string] $Filter
)

Set-StrictMode -Version latest
$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path "$PSScriptRoot/..").Path
$timeoutSeconds = 300
$headlessResultsDirectory = Join-Path $repoRoot "artifacts/test/unity-cli"

. $PSScriptRoot/unity-test-cli.ps1
. $PSScriptRoot/unity-test-pipeline.ps1

if (-not (Get-Command unity -ErrorAction SilentlyContinue)) {
    throw "Unity CLI executable 'unity' was not found on PATH."
}

$projectPath = Join-Path $repoRoot "samples/unity-of-bugs-local"
if (-not (Test-Path $projectPath -PathType Container)) {
    throw "Local test project was not found at $projectPath."
}

$projectPath = (Resolve-Path $projectPath).Path

$executionMode = Get-UnityExecutionMode -ProjectPath $projectPath

$runs = @()
if ($executionMode -eq "Pipeline") {
    $runs += Invoke-UnityPipelineTests -ProjectPath $projectPath -Mode $Mode -TimeoutSeconds $timeoutSeconds -Filter $Filter
}
else {
    $modes = switch ($Mode) {
        "EditMode" { @("EditMode") }
        "PlayMode" { @("PlayMode") }
        default { @("EditMode", "PlayMode") }
    }

    foreach ($testMode in $modes) {
        $runs += Invoke-UnityHeadlessTest -ProjectPath $projectPath -TestMode $testMode -ResultsDirectory $headlessResultsDirectory -TimeoutSeconds $timeoutSeconds -Filter $Filter
    }
}

$success = @($runs | Where-Object { -not $_.Success }).Count -eq 0
foreach ($run in $runs) {
    $status = if ($run.Success) { "Passed" } else { "Failed" }
    Write-Host "$($run.Mode): $status in $($run.Duration)s"
    Write-Host "        Passed: $($run.Passed)"
    Write-Host "        Failed: $($run.Failed)"
    Write-Host "       Skipped: $($run.Skipped)"
    Write-Host "  Inconclusive: $($run.Inconclusive)"

    foreach ($test in $run.FailedTests) {
        Write-Host "`nFailed: $($test.Name)" -ForegroundColor Red
        if ($test.Message) {
            Write-Host $test.Message
        }
        if ($test.StackTrace) {
            Write-Host $test.StackTrace
        }
    }
}

if (-not $success) {
    exit 1
}
