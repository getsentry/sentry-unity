#!/usr/bin/env pwsh
#
# Integration tests for Sentry Unity SDK (WebGL)
#
# Environment variables:
#   SENTRY_WEBGL_BUILD_PATH: path to the WebGL build directory
#   SENTRY_TEST_DSN: test DSN
#   SENTRY_AUTH_TOKEN: authentication token for Sentry API

Set-StrictMode -Version latest
$ErrorActionPreference = "Stop"

# Import app-runner modules
. $PSScriptRoot/../../modules/app-runner/import-modules.ps1

# Import shared test cases and utility functions
. $PSScriptRoot/CommonTestCases.ps1

BeforeAll {
    # Run integration test action via WebGL (HTTP server + headless Chrome)
    function Invoke-TestAction {
        param (
            [Parameter(Mandatory=$true)]
            [string]$Action
        )

        Write-Host "Running $Action..."

        $serverScript = Join-Path $PSScriptRoot "webgl-server.py"
        $buildPath = $env:SENTRY_WEBGL_BUILD_PATH
        $timeoutSeconds = 120

        $process = Start-Process -FilePath "python3" `
            -ArgumentList @($serverScript, $buildPath, $Action, $timeoutSeconds) `
            -NoNewWindow -PassThru -RedirectStandardOutput "$PSScriptRoot/results/${Action}-stdout.txt" `
            -RedirectStandardError "$PSScriptRoot/results/${Action}-stderr.txt"

        $process | Wait-Process -Timeout ($timeoutSeconds + 30)

        $exitCode = $process.ExitCode
        $stdoutContent = Get-Content "$PSScriptRoot/results/${Action}-stdout.txt" -Raw -ErrorAction SilentlyContinue
        $stderrContent = Get-Content "$PSScriptRoot/results/${Action}-stderr.txt" -Raw -ErrorAction SilentlyContinue

        # Parse the JSON array of console lines from stdout
        $output = @()
        if ($stdoutContent) {
            try {
                $output = $stdoutContent | ConvertFrom-Json
            }
            catch {
                Write-Host "Failed to parse webgl-server.py output as JSON: $_"
                Write-Host "Raw stdout: $stdoutContent"
                $output = @($stdoutContent)
            }
        }

        if ($stderrContent) {
            Write-Host "::group::Server stderr ($Action)"
            Write-Host $stderrContent
            Write-Host "::endgroup::"
        }

        $runResult = [PSCustomObject]@{
            Output = $output
            ExitCode = $exitCode
        }

        # Save result to JSON file
        $runResult | ConvertTo-Json -Depth 5 | Out-File -FilePath (Get-OutputFilePath "${Action}-result.json")

        # Print app output so it's visible in CI logs
        Write-Host "::group::Browser console output ($Action)"
        $runResult.Output | ForEach-Object { Write-Host $_ }
        Write-Host "::endgroup::"

        if ($exitCode -ne 0) {
            Write-Warning "WebGL test action '$Action' did not complete (exit code: $exitCode)"
        }

        return $runResult
    }

    # Create directory for the test results
    New-Item -ItemType Directory -Path "$PSScriptRoot/results/" -ErrorAction Continue 2>&1 | Out-Null
    Set-OutputDir -Path "$PSScriptRoot/results/"

    # Initialize test parameters
    $script:TestSetup = [PSCustomObject]@{
        Platform = "WebGL"
        BuildPath = $env:SENTRY_WEBGL_BUILD_PATH
        Dsn = $env:SENTRY_TEST_DSN
        AuthToken = $env:SENTRY_AUTH_TOKEN
    }

    # Validate environment
    if ([string]::IsNullOrEmpty($script:TestSetup.BuildPath)) {
        throw "SENTRY_WEBGL_BUILD_PATH environment variable is not set."
    }
    if (-not (Test-Path $script:TestSetup.BuildPath)) {
        throw "WebGL build not found at: $($script:TestSetup.BuildPath)"
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


Describe "Unity WebGL Integration Tests" {

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
}
