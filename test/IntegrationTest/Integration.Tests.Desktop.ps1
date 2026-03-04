#!/usr/bin/env pwsh
#
# Integration tests for Sentry Unity SDK (Desktop: Windows, Linux)
#
# Environment variables:
#   SENTRY_TEST_APP: path to the test executable
#   SENTRY_TEST_DSN: test DSN
#   SENTRY_AUTH_TOKEN: authentication token for Sentry API

Set-StrictMode -Version latest
$ErrorActionPreference = "Stop"

# Import app-runner modules
. $PSScriptRoot/../../modules/app-runner/import-modules.ps1

# Import shared test cases and utility functions
. $PSScriptRoot/CommonTestCases.ps1

BeforeAll {
    # Run a test app action using Start-Process with file-based logging.
    # We avoid piping stdout because on Windows it kills crashpad_handler.exe
    # when the process crashes, preventing native crash capture.
    function Invoke-TestAction {
        param (
            [Parameter(Mandatory=$true)]
            [string]$Action
        )

        Write-Host "Running $Action..."

        $resultsDir = Resolve-Path "$PSScriptRoot/results/"
        $logFile = Join-Path $resultsDir "$Action-player.log"
        $appArgs = @("--test", $Action, "-logFile", $logFile)

        $process = Start-Process $env:SENTRY_TEST_APP -ArgumentList $appArgs -PassThru
        $process | Wait-Process -Timeout 60 -ErrorAction SilentlyContinue

        if (-not $process.HasExited) {
            $process | Stop-Process -Force
        }

        $output = @(Get-Content $logFile -ErrorAction SilentlyContinue)

        $runResult = @{
            Output   = $output
            ExitCode = $process.ExitCode
        }

        # Save result to JSON file
        $runResult | ConvertTo-Json -Depth 5 | Out-File -FilePath (Get-OutputFilePath "${Action}-result.json")

        # For crash tests: relaunch the app to flush the cached crash report
        if ($Action -eq "crash-capture") {
            Write-Host "Running crash-send to ensure crash report is sent..."

            $sendLogFile = Join-Path $resultsDir "crash-send-player.log"
            $sendProcess = Start-Process $env:SENTRY_TEST_APP `
                -ArgumentList "--test", "crash-send", "-logFile", $sendLogFile `
                -PassThru
            $sendProcess | Wait-Process -Timeout 60 -ErrorAction SilentlyContinue

            $sendOutput = @(Get-Content $sendLogFile -ErrorAction SilentlyContinue)

            Write-Host "::group::App output (crash-send)"
            $sendOutput | ForEach-Object { Write-Host $_ }
            Write-Host "::endgroup::"

            $runResult.CrashSendOutput = $sendOutput
        }

        # Print app output so it's visible in CI logs
        Write-Host "::group::App output ($Action)"
        $runResult.Output | ForEach-Object { Write-Host $_ }
        Write-Host "::endgroup::"

        return $runResult
    }

    # Create directory for the test results
    New-Item -ItemType Directory -Path "$PSScriptRoot/results/" -ErrorAction Continue 2>&1 | Out-Null
    Set-OutputDir -Path "$PSScriptRoot/results/"

    # Initialize test parameters
    $script:TestSetup = [PSCustomObject]@{
        Platform = "Desktop"
        AppPath = $env:SENTRY_TEST_APP
        Dsn = $env:SENTRY_TEST_DSN
        AuthToken = $env:SENTRY_AUTH_TOKEN
    }

    # Validate environment
    if ([string]::IsNullOrEmpty($script:TestSetup.AppPath)) {
        throw "SENTRY_TEST_APP environment variable is not set."
    }
    if (-not (Test-Path $script:TestSetup.AppPath)) {
        throw "App not found at: $($script:TestSetup.AppPath)"
    }
    if ([string]::IsNullOrEmpty($script:TestSetup.Dsn)) {
        throw "SENTRY_TEST_DSN environment variable is not set."
    }
    if ([string]::IsNullOrEmpty($script:TestSetup.AuthToken)) {
        throw "SENTRY_AUTH_TOKEN environment variable is not set."
    }

    Connect-SentryApi `
        -ApiToken $script:TestSetup.AuthToken `
        -DSN $script:TestSetup.Dsn
}


AfterAll {
    Disconnect-SentryApi
}


Describe "Unity Desktop Integration Tests" {

    Context "Message Capture" {
        BeforeAll {
            $script:runEvent = $null
            $script:runResult = Invoke-TestAction -Action "message-capture"

            $eventId = Get-EventIds -AppOutput $script:runResult.Output -ExpectedCount 1
            if ($eventId) {
                Write-Host "::group::Getting event content"
                $script:runEvent = Get-SentryTestEvent -EventId "$eventId"
                Write-Host "::endgroup::"
            }
        }

        It "<Name>" -ForEach $CommonTestCases {
            & $testBlock -SentryEvent $runEvent -TestType "message-capture" -RunResult $runResult -TestSetup $script:TestSetup
        }

        It "Has message level info" {
            ($runEvent.tags | Where-Object { $_.key -eq "level" }).value | Should -Be "info"
        }

        It "Has message content" {
            $runEvent.title | Should -Not -BeNullOrEmpty
        }
    }

    Context "Exception Capture" {
        BeforeAll {
            $script:runEvent = $null
            $script:runResult = Invoke-TestAction -Action "exception-capture"

            $eventId = Get-EventIds -AppOutput $script:runResult.Output -ExpectedCount 1
            if ($eventId) {
                Write-Host "::group::Getting event content"
                $script:runEvent = Get-SentryTestEvent -EventId "$eventId"
                Write-Host "::endgroup::"
            }
        }

        It "<Name>" -ForEach $CommonTestCases {
            & $testBlock -SentryEvent $runEvent -TestType "exception-capture" -RunResult $runResult -TestSetup $script:TestSetup
        }

        It "Has exception information" {
            $runEvent.exception | Should -Not -BeNullOrEmpty
            $runEvent.exception.values | Should -Not -BeNullOrEmpty
        }

        It "Has exception with stacktrace" {
            $exception = $runEvent.exception.values[0]
            $exception | Should -Not -BeNullOrEmpty
            $exception.type | Should -Not -BeNullOrEmpty
            $exception.stacktrace | Should -Not -BeNullOrEmpty
        }

        It "Has error level" {
            ($runEvent.tags | Where-Object { $_.key -eq "level" }).value | Should -Be "error"
        }
    }

    Context "Crash Capture" {
        BeforeAll {
            $script:runEvent = $null
            $script:runResult = Invoke-TestAction -Action "crash-capture"

            $eventId = Get-EventIds -AppOutput $script:runResult.Output -ExpectedCount 1
            if ($eventId) {
                Write-Host "::group::Getting event content"
                $script:runEvent = Get-SentryTestEvent -TagName "test.crash_id" -TagValue "$eventId" -TimeoutSeconds 120
                Write-Host "::endgroup::"
            }
        }

        It "<Name>" -ForEach $CommonTestCases {
            & $testBlock -SentryEvent $runEvent -TestType "crash-capture" -RunResult $runResult -TestSetup $script:TestSetup
        }

        It "Has fatal level" {
            ($runEvent.tags | Where-Object { $_.key -eq "level" }).value | Should -Be "fatal"
        }

        It "Has exception with stacktrace" {
            $runEvent.exception | Should -Not -BeNullOrEmpty
            $runEvent.exception.values | Should -Not -BeNullOrEmpty
            $exception = $runEvent.exception.values[0]
            $exception | Should -Not -BeNullOrEmpty
            $exception.stacktrace | Should -Not -BeNullOrEmpty
        }

        It "Reports crashedLastRun as Crashed on relaunch" {
            $crashedLastRunLine = $runResult.CrashSendOutput | Where-Object {
                $_ -match "crashedLastRun=Crashed"
            }
            $crashedLastRunLine | Should -Not -BeNullOrEmpty -Because "Native SDK should report crashedLastRun=Crashed after a native crash"
        }

        It "Crash-send completes flush successfully" {
            $flushLine = $runResult.CrashSendOutput | Where-Object {
                $_ -match "Flush complete"
            }
            $flushLine | Should -Not -BeNullOrEmpty -Because "crash-send should complete its flush before quitting"
        }
    }
}
