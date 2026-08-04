function Limit-Text([object] $value) {
    $text = [string] $value
    if ($text.Length -le 2000) {
        return $text
    }

    return "$($text.Substring(0, 2000))`n... truncated"
}

function Wait-ForPlayModeTests([string] $projectPath, [int] $timeoutSeconds) {
    $deadline = (Get-Date).AddSeconds($timeoutSeconds)
    $lastError = $null
    while ((Get-Date) -lt $deadline) {
        try {
            $status = Invoke-UnityPipelineCommand -ProjectPath $projectPath -Command "test_status"
            if ($status -is [string]) {
                $status = $status | ConvertFrom-Json
            }
        }
        catch {
            $lastError = $_
            Start-Sleep -Seconds 1
            continue
        }

        if ($status.status -eq "completed") {
            return [pscustomobject]@{
                Mode = "PlayMode"
                Duration = $status.duration
                Summary = [pscustomobject]@{
                    Total = $status.summary.total
                    Passed = $status.summary.passed
                    Failed = $status.summary.failed
                    Skipped = $status.summary.skipped
                    Inconclusive = $status.summary.inconclusive
                }
                Results = $status.results
            }
        }

        if ($status.status -notin @("running", "queued")) {
            throw "Unity PlayMode tests ended with status '$($status.status)': $($status.message)"
        }

        Start-Sleep -Seconds 1
    }

    if ($lastError) {
        throw "Unity PlayMode tests did not finish within $timeoutSeconds seconds. $lastError"
    }

    throw "Unity PlayMode tests did not finish within $timeoutSeconds seconds."
}

function Get-TestSummary([string] $requestedMode, [object] $result) {
    if ($null -eq $result -or $null -eq $result.Summary) {
        throw "Unity did not return a test summary for $requestedMode tests."
    }

    $commandSuccess = $result.PSObject.Properties["success"]
    if ($commandSuccess -and -not $commandSuccess.Value -and $result.error) {
        throw "Unity $requestedMode tests could not run: $($result.error)"
    }

    $summary = $result.Summary
    $failedTests = @($result.Results | Where-Object { $_.Status -eq "Failed" } | ForEach-Object {
            [pscustomobject]@{
                Name       = $_.FullName
                Message    = Limit-Text $_.Message
                StackTrace = Limit-Text $_.StackTrace
            }
        })

    $success = $summary.Total -gt 0 -and $summary.Failed -eq 0 -and $summary.Inconclusive -eq 0
    [pscustomobject]@{
        Mode         = $result.Mode
        Duration     = $result.Duration
        Total        = $summary.Total
        Passed       = $summary.Passed
        Failed       = $summary.Failed
        Skipped      = $summary.Skipped
        Inconclusive = $summary.Inconclusive
        FailedTests  = $failedTests
        Success      = $success
    }
}

function Invoke-UnityPipelineTests(
    [string] $projectPath,
    [string] $mode,
    [int] $timeoutSeconds,
    [string] $filter
) {
    Prepare-UnityPipelineEditor -ProjectPath $projectPath -TimeoutSeconds $timeoutSeconds

    $modes = switch ($mode) {
        "EditMode" { @("editor") }
        "PlayMode" { @("playmode") }
        default { @("editor", "playmode") }
    }

    $runs = @()
    foreach ($testPlatform in $modes) {
        $commandArguments = @("--mode", $testPlatform)
        if ($filter) {
            $commandArguments += "--filter", $filter, "--filter_type", "testName"
        }

        if ($testPlatform -eq "playmode") {
            $started = Invoke-UnityPipelineCommand -ProjectPath $projectPath -Command "run_tests" -CommandArguments ($commandArguments + @("--async_tests", "true"))
            if (-not $started.success) {
                throw "Unity PlayMode tests could not start: $($started.error)"
            }
            $result = Wait-ForPlayModeTests -ProjectPath $projectPath -TimeoutSeconds $timeoutSeconds
        }
        else {
            $result = Invoke-UnityPipelineCommand -ProjectPath $projectPath -Command "run_tests" -CommandArguments $commandArguments -TimeoutSeconds ($timeoutSeconds + 30)
        }

        $runs += Get-TestSummary -RequestedMode $testPlatform -Result $result
    }

    return $runs
}
