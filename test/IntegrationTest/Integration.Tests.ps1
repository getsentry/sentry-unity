#!/usr/bin/env pwsh
#
# Integration tests for Sentry Unity SDK (Android)
#
# Environment variables:
#   SENTRY_TEST_APK: path to the test APK file
#   SENTRY_TEST_DSN: test DSN
#   SENTRY_AUTH_TOKEN: authentication token for Sentry API

Set-StrictMode -Version latest
$ErrorActionPreference = "Stop"

# Import app-runner modules
. $PSScriptRoot/../../modules/app-runner/import-modules.ps1

# Import shared test cases and utility functions
. $PSScriptRoot/CommonTestCases.ps1


BeforeAll {
    $script:PackageName = "io.sentry.unity.integrationtest"

    # Run integration test action on device
    function Invoke-TestAction {
        param (
            [Parameter(Mandatory=$true)]
            [string]$Action
        )

        Write-Host "Running $Action..."

        $extras = @("-e", "test", $Action)
        $runResult = Invoke-DeviceApp -ExecutablePath $script:AndroidComponent -Arguments $extras

        # Save result to JSON file
        $runResult | ConvertTo-Json -Depth 5 | Out-File -FilePath (Get-OutputFilePath "${Action}-result.json")

        # Launch app again to ensure crash report is sent
        if ($Action -eq "crash-capture") {
            Write-Host "Running crash-send to ensure crash report is sent..."

            $sendExtras = @("-e", "test", "crash-send")
            $sendResult = Invoke-DeviceApp -ExecutablePath $script:AndroidComponent -Arguments $sendExtras

            # Save crash-send result to JSON for debugging
            $sendResult | ConvertTo-Json -Depth 5 | Out-File -FilePath (Get-OutputFilePath "crash-send-result.json")

            # Print crash-send output
            Write-Host "::group::App output (crash-send)"
            $sendResult.Output | ForEach-Object { Write-Host $_ }
            Write-Host "::endgroup::"

            # Attach to runResult for test access
            $runResult | Add-Member -NotePropertyName "CrashSendOutput" -NotePropertyValue $sendResult.Output
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
        ApkPath = $env:SENTRY_TEST_APK
        Dsn = $env:SENTRY_TEST_DSN
        AuthToken = $env:SENTRY_AUTH_TOKEN
    }

    # Validate environment
    if ([string]::IsNullOrEmpty($script:TestSetup.ApkPath)) {
        throw "SENTRY_TEST_APK environment variable is not set."
    }
    if (-not (Test-Path $script:TestSetup.ApkPath)) {
        throw "APK not found at: $($script:TestSetup.ApkPath)"
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

    Connect-Device -Platform "Adb"
    Install-DeviceApp -Path $script:TestSetup.ApkPath

    # Detect the launcher activity from the installed package
    $dumpOutput = & adb shell dumpsys package $script:PackageName 2>&1 | Out-String
    if ($dumpOutput -match "com.unity3d.player.UnityPlayerGameActivity") {
        $script:AndroidComponent = "$($script:PackageName)/com.unity3d.player.UnityPlayerGameActivity"
    } else {
        $script:AndroidComponent = "$($script:PackageName)/com.unity3d.player.UnityPlayerActivity"
    }
    Write-Host "Detected activity: $($script:AndroidComponent)"
}


AfterAll {
    Disconnect-SentryApi
    Disconnect-Device
}


Describe "Unity Android Integration Tests" {

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
