function Limit-Text([object] $Value) {
    $text = [string] $Value
    if ($text.Length -le 2000) {
        return $text
    }

    return "$($text.Substring(0, 2000))`n... truncated"
}

function Invoke-UnityCommand([string] $Command, [string[]] $CommandArguments = @(), [int] $CommandTimeout = 30) {
    $arguments = @(
        "--json", "command", $Command,
        "--project-path", $ProjectPath,
        "--timeout", $CommandTimeout
    ) + $CommandArguments

    $output = & unity @arguments 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "Unity command '$Command' failed:`n$($output | Out-String)"
    }

    try {
        $response = $output | Out-String | ConvertFrom-Json
    }
    catch {
        throw "Unity command '$Command' returned invalid JSON:`n$($output | Out-String)"
    }

    if (-not $response.success -or -not $response.data.success) {
        throw "Unity command '$Command' failed:`n$($output | Out-String)"
    }

    return $response.data.result
}

function Wait-ForEditorReady {
    $deadline = (Get-Date).AddSeconds($testTimeout)
    $lastError = $null

    while ((Get-Date) -lt $deadline) {
        try {
            $status = Invoke-UnityCommand "editor_status"
        }
        catch {
            $lastError = $_
            Start-Sleep -Seconds 1
            continue
        }

        if ($status.status -eq "ready" -and -not $status.compiling -and -not $status.domainReloadInProgress) {
            if ($status.playMode -ne "stopped") {
                throw "Unity Editor must be stopped before running tests. Current play mode: $($status.playMode)."
            }

            if ((Resolve-Path $status.projectPath).Path -ne $ProjectPath) {
                throw "Unity Pipeline is connected to $($status.projectPath), not $ProjectPath."
            }

            return
        }

        Start-Sleep -Seconds 1
    }

    if ($lastError) {
        throw "Unity Editor did not become ready within $testTimeout seconds. $lastError"
    }

    throw "Unity Editor did not become ready within $testTimeout seconds."
}

function Wait-ForRecompile {
    $recompile = Invoke-UnityCommand "recompile" @() $testTimeout
    if ($recompile.status -eq "up_to_date") {
        return
    }

    $deadline = (Get-Date).AddSeconds($testTimeout)
    $lastError = $null
    while ((Get-Date) -lt $deadline) {
        try {
            $status = Invoke-UnityCommand "recompile_status"
            if ($status -is [string]) {
                $status = $status | ConvertFrom-Json
            }
        }
        catch {
            $lastError = $_
            Start-Sleep -Seconds 1
            continue
        }

        if ($status.failed) {
            throw "Unity script recompilation failed:`n$($status.errors -join "`n")"
        }

        if ($status.status -in @("completed", "up_to_date", "idle")) {
            return
        }

        Start-Sleep -Seconds 1
    }

    if ($lastError) {
        throw "Unity script recompilation did not finish within $testTimeout seconds. $lastError"
    }

    throw "Unity script recompilation did not finish within $testTimeout seconds."
}

function Wait-ForPlayModeTests {
    $deadline = (Get-Date).AddSeconds($testTimeout)
    $lastError = $null
    while ((Get-Date) -lt $deadline) {
        try {
            $status = Invoke-UnityCommand "test_status"
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
        throw "Unity PlayMode tests did not finish within $testTimeout seconds. $lastError"
    }

    throw "Unity PlayMode tests did not finish within $testTimeout seconds."
}

function Get-TestSummary([string] $RequestedMode, [object] $Result) {
    if ($null -eq $Result -or $null -eq $Result.Summary) {
        throw "Unity did not return a test summary for $RequestedMode tests."
    }

    $commandSuccess = $Result.PSObject.Properties["success"]
    if ($commandSuccess -and -not $commandSuccess.Value -and $Result.error) {
        throw "Unity $RequestedMode tests could not run: $($Result.error)"
    }

    $summary = $Result.Summary
    $failedTests = @($Result.Results | Where-Object { $_.Status -eq "Failed" } | ForEach-Object {
            [pscustomobject]@{
                Name       = $_.FullName
                Message    = Limit-Text $_.Message
                StackTrace = Limit-Text $_.StackTrace
            }
        })

    $success = $summary.Total -gt 0 -and $summary.Failed -eq 0 -and $summary.Inconclusive -eq 0
    [pscustomobject]@{
        Mode         = $Result.Mode
        Duration     = $Result.Duration
        Total        = $summary.Total
        Passed       = $summary.Passed
        Failed       = $summary.Failed
        Skipped      = $summary.Skipped
        Inconclusive = $summary.Inconclusive
        FailedTests  = $failedTests
        Success      = $success
    }
}

function Invoke-UnityPipelineTests([string] $Mode) {
    Wait-ForEditorReady
    Invoke-UnityCommand "set_autotick" @("--enable", "true") | Out-Null
    Wait-ForRecompile

    $modes = switch ($Mode) {
        "EditMode" { @("editor") }
        "PlayMode" { @("playmode") }
        default { @("editor", "playmode") }
    }

    $runs = @()
    foreach ($testPlatform in $modes) {
        $commandArguments = @("--mode", $testPlatform)
        if ($Filter) {
            $commandArguments += "--filter", $Filter, "--filter_type", "testName"
        }

        if ($testPlatform -eq "playmode") {
            $started = Invoke-UnityCommand "run_tests" ($commandArguments + @("--async_tests", "true")) 30
            if (-not $started.success) {
                throw "Unity PlayMode tests could not start: $($started.error)"
            }
            $result = Wait-ForPlayModeTests
        }
        else {
            $result = Invoke-UnityCommand "run_tests" $commandArguments ($testTimeout + 30)
        }

        $runs += Get-TestSummary $testPlatform $result
    }

    return $runs
}
