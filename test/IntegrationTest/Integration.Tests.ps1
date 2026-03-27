#!/usr/bin/env pwsh
#
# Integration tests for Sentry Unity SDK
#
# Environment variables:
#   SENTRY_TEST_PLATFORM: target platform (Android, Desktop, iOS, WebGL, Xbox)
#   SENTRY_TEST_DSN: test DSN
#   SENTRY_AUTH_TOKEN: authentication token for Sentry API
#
#   SENTRY_TEST_APP: path to the test app (APK, executable, .app bundle, WebGL build directory,
#                    or Xbox package directory containing .xvc)
#
# Platform-specific environment variables:
#   iOS:     SENTRY_IOS_VERSION - iOS simulator version (e.g. "17.0" or "latest")
#   Xbox:    XBCONNECT_TARGET - Xbox devkit IP address

Set-StrictMode -Version latest
$ErrorActionPreference = "Stop"

# Import app-runner modules
. $PSScriptRoot/../../modules/app-runner/import-modules.ps1

# Import shared test cases and utility functions
. $PSScriptRoot/CommonTestCases.ps1

BeforeAll {
    # Build app arguments for a given test action
    function Get-AppArguments {
        param([string]$Action)

        switch ($script:Platform) {
            "Android" { return @("-e", "test", $Action) }
            "Desktop" { return @("--test", $Action, "-logFile", "-") }
            "iOS"     { return @("--test", $Action) }
            "Xbox"    { return @("--test", $Action) }
        }
    }

    # Run xbdir.exe to list a directory on the Xbox devkit. Returns the output lines.
    # Errors are caught and logged rather than thrown — this is a diagnostic tool.
    function Invoke-XboxDirListing {
        param(
            [Parameter(Mandatory=$true)]
            [string]$DevicePath
        )

        Write-Host "  xbdir x$DevicePath" -ForegroundColor Gray
        try {
            $output = & xbdir.exe "x$DevicePath" 2>&1
            $output | ForEach-Object { Write-Host "    $_" -ForegroundColor Gray }
            return $output
        } catch {
            Write-Host "    (xbdir failed: $_)" -ForegroundColor Yellow
            return @()
        }
    }

    # Retrieve the integration test log file from Xbox and attach it to the run result.
    # The Unity app writes to Application.persistentDataPath/unity-integration-test.log.
    # We don't know the exact devkit-accessible path yet, so we search several candidates
    # and log directory listings for diagnostics.
    function Get-XboxLogOutput {
        param(
            [Parameter(Mandatory=$true)]
            $RunResult,

            [Parameter(Mandatory=$true)]
            [string]$Action
        )

        $logFileName = "unity-integration-test.log"
        $logLocalDir = "$PSScriptRoot/results/xbox-logs/$Action"
        New-Item -ItemType Directory -Path $logLocalDir -Force | Out-Null

        # Check if the app exited with a non-zero code (Logger.Open likely failed)
        if ($RunResult.ExitCode -and $RunResult.ExitCode -ne 0) {
            Write-Warning "Xbox app exited with code $($RunResult.ExitCode) — the log file may not have been created."
        }

        # Candidate paths where Application.persistentDataPath might map on the devkit.
        # We try each one via xbcopy and stop at the first success.
        $packageFamilyName = $script:ExecutablePath -replace '!.*$', ''

        $candidateDirs = @(
            "D:\DevelopmentFiles\$packageFamilyName\LocalState"
            "D:\DevelopmentFiles\$packageFamilyName\AC\LocalState"
            "D:\Logs"
        )

        $logContent = $null

        foreach ($candidateDir in $candidateDirs) {
            Write-Host "Trying to retrieve log from: $candidateDir" -ForegroundColor Yellow
            try {
                $copyDest = "$logLocalDir/$($candidateDir -replace '[:\\]', '_')"
                New-Item -ItemType Directory -Path $copyDest -Force | Out-Null
                Copy-DeviceItem -DevicePath "$candidateDir\$logFileName" -Destination $copyDest
                $localFile = Join-Path $copyDest $logFileName
                if (Test-Path $localFile) {
                    $logContent = Get-Content $localFile -ErrorAction SilentlyContinue
                    if ($logContent -and $logContent.Count -gt 0) {
                        Write-Host "Retrieved log file from $candidateDir ($($logContent.Count) lines)" -ForegroundColor Green
                        break
                    }
                }
            } catch {
                Write-Host "  Not found at $candidateDir ($_)" -ForegroundColor Gray
            }
        }

        if (-not $logContent -or $logContent.Count -eq 0) {
            # Log file not found — dump directory listings to help diagnose the correct path
            Write-Warning "Log file not found at any candidate path. Listing directories for diagnostics:"
            Write-Host "::group::Xbox directory diagnostics"

            Write-Host "`n--- D:\ root ---" -ForegroundColor Cyan
            Invoke-XboxDirListing "D:\"

            Write-Host "`n--- D:\DevelopmentFiles\ ---" -ForegroundColor Cyan
            Invoke-XboxDirListing "D:\DevelopmentFiles\"

            Write-Host "`n--- D:\DevelopmentFiles\$packageFamilyName\ ---" -ForegroundColor Cyan
            Invoke-XboxDirListing "D:\DevelopmentFiles\$packageFamilyName\"

            Write-Host "`n--- D:\DevelopmentFiles\$packageFamilyName\AC\ ---" -ForegroundColor Cyan
            Invoke-XboxDirListing "D:\DevelopmentFiles\$packageFamilyName\AC\"

            Write-Host "`n--- S:\ root ---" -ForegroundColor Cyan
            Invoke-XboxDirListing "S:\"

            Write-Host "::endgroup::"

            throw "Failed to retrieve Xbox log file ($logFileName). See directory diagnostics above."
        }

        $RunResult.Output = $logContent
        return $RunResult
    }

    # Run a WebGL test action via headless Chrome
    function Invoke-WebGLTestAction {
        param (
            [Parameter(Mandatory=$true)]
            [string]$Action
        )

        $serverScript = Join-Path $PSScriptRoot "webgl-server.py"
        $buildPath = $env:SENTRY_TEST_APP
        $timeoutSeconds = 120

        $process = Start-Process -FilePath "python3" `
            -ArgumentList @($serverScript, $buildPath, $Action, $timeoutSeconds) `
            -NoNewWindow -PassThru -RedirectStandardOutput "$PSScriptRoot/results/${Action}-stdout.txt" `
            -RedirectStandardError "$PSScriptRoot/results/${Action}-stderr.txt"

        $process | Wait-Process -Timeout ($timeoutSeconds + 30) -ErrorAction SilentlyContinue

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

    # Run integration test action
    function Invoke-TestAction {
        param (
            [Parameter(Mandatory=$true)]
            [string]$Action
        )

        Write-Host "Running $Action..."

        if ($script:Platform -eq "WebGL") {
            return Invoke-WebGLTestAction -Action $Action
        }

        $appArgs = Get-AppArguments -Action $Action
        $runResult = Invoke-DeviceApp -ExecutablePath $script:ExecutablePath -Arguments $appArgs

        # On Xbox, console output is not available in non-development builds.
        # Instead, Unity writes to a log file on the device which we retrieve and use as output.
        if ($script:Platform -eq "Xbox") {
            $runResult = Get-XboxLogOutput -RunResult $runResult -Action $Action
        }

        # Save result to JSON file
        $runResult | ConvertTo-Json -Depth 5 | Out-File -FilePath (Get-OutputFilePath "${Action}-result.json")

        # Launch app again to ensure crash report is sent
        if ($Action -eq "crash-capture") {
            Write-Host "Running crash-send to ensure crash report is sent..."

            $sendArgs = Get-AppArguments -Action "crash-send"
            $sendResult = Invoke-DeviceApp -ExecutablePath $script:ExecutablePath -Arguments $sendArgs

            # On Xbox, retrieve the log file for crash-send output too
            if ($script:Platform -eq "Xbox") {
                $sendResult = Get-XboxLogOutput -RunResult $sendResult -Action "crash-send"
            }

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

    $script:Platform = $env:SENTRY_TEST_PLATFORM
    if ([string]::IsNullOrEmpty($script:Platform)) {
        throw "SENTRY_TEST_PLATFORM environment variable is not set. Expected: Android, Desktop, iOS, WebGL, or Xbox"
    }

    # Validate common environment
    if ([string]::IsNullOrEmpty($env:SENTRY_TEST_DSN)) {
        throw "SENTRY_TEST_DSN environment variable is not set."
    }
    if ([string]::IsNullOrEmpty($env:SENTRY_AUTH_TOKEN)) {
        throw "SENTRY_AUTH_TOKEN environment variable is not set."
    }
    if ([string]::IsNullOrEmpty($env:SENTRY_TEST_APP)) {
        throw "SENTRY_TEST_APP environment variable is not set."
    }
    if (-not (Test-Path $env:SENTRY_TEST_APP)) {
        throw "App not found at: $env:SENTRY_TEST_APP"
    }

    # Platform-specific device setup
    switch ($script:Platform) {
        "Android" {
            $script:PackageName = "io.sentry.unity.integrationtest"

            Connect-Device -Platform "Adb"
            Install-DeviceApp -Path $env:SENTRY_TEST_APP

            # Detect the launcher activity from the installed package
            $dumpOutput = & adb shell dumpsys package $script:PackageName 2>&1 | Out-String
            if ($dumpOutput -match "com.unity3d.player.UnityPlayerGameActivity") {
                $script:ExecutablePath = "$($script:PackageName)/com.unity3d.player.UnityPlayerGameActivity"
            } else {
                $script:ExecutablePath = "$($script:PackageName)/com.unity3d.player.UnityPlayerActivity"
            }
            Write-Host "Detected activity: $($script:ExecutablePath)"
        }
        "Desktop" {
            $script:ExecutablePath = $env:SENTRY_TEST_APP
            Connect-Device -Platform "Local"
        }
        "iOS" {
            if ([string]::IsNullOrEmpty($env:SENTRY_IOS_VERSION)) {
                throw "SENTRY_IOS_VERSION environment variable is not set."
            }

            $script:ExecutablePath = "com.DefaultCompany.IntegrationTest"

            $target = $env:SENTRY_IOS_VERSION
            # Convert bare version numbers (e.g. "17.0") to "iOS 17.0" format expected by iOSSimulatorProvider
            if ($target -match '^\d+\.\d+$') {
                $target = "iOS $target"
            }
            Connect-Device -Platform "iOSSimulator" -Target $target
            Install-DeviceApp -Path $env:SENTRY_TEST_APP
        }
        "Xbox" {
            if ([string]::IsNullOrEmpty($env:XBCONNECT_TARGET)) {
                throw "XBCONNECT_TARGET environment variable is not set."
            }

            Connect-Device -Platform "Xbox" -Target $env:XBCONNECT_TARGET

            # Xbox uses packaged .xvc flow — SENTRY_TEST_APP points to the package directory
            $xvcFile = Get-ChildItem -Path $env:SENTRY_TEST_APP -Filter "*.xvc" | Select-Object -First 1
            if (-not $xvcFile) {
                throw "No .xvc package found in: $env:SENTRY_TEST_APP"
            }
            Install-DeviceApp -Path $xvcFile.FullName
            $script:ExecutablePath = Get-PackageAumid -PackagePath $env:SENTRY_TEST_APP
            Write-Host "Using AUMID: $($script:ExecutablePath)"
        }
        "WebGL" {
        }
        default {
            throw "Unknown platform: $($script:Platform). Expected: Android, Desktop, iOS, WebGL, or Xbox"
        }
    }

    # Create directory for the test results
    New-Item -ItemType Directory -Path "$PSScriptRoot/results/" -ErrorAction Continue 2>&1 | Out-Null
    Set-OutputDir -Path "$PSScriptRoot/results/"

    # Initialize test parameters
    $script:TestSetup = [PSCustomObject]@{
        Platform = $script:Platform
        Dsn = $env:SENTRY_TEST_DSN
        AuthToken = $env:SENTRY_AUTH_TOKEN
    }

    Connect-SentryApi `
        -ApiToken $script:TestSetup.AuthToken `
        -DSN $script:TestSetup.Dsn
}


AfterAll {
    Disconnect-SentryApi
    if ($script:Platform -ne "WebGL") {
        Disconnect-Device
    }
}


Describe "Unity $($env:SENTRY_TEST_PLATFORM) Integration Tests" {

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

if ($env:SENTRY_TEST_PLATFORM -ne "WebGL") {
    Describe "Unity $($env:SENTRY_TEST_PLATFORM) Crash Tests" {

        Context "Crash Capture" {
            BeforeAll {
                $script:runEvent = $null
                $script:runResult = Invoke-TestAction -Action "crash-capture"

                # Validate crash-send completed before polling Sentry (avoids a 300s blind wait)
                $flushLine = $runResult.CrashSendOutput | Where-Object { $_ -match "Flush complete" }
                if (-not $flushLine) {
                    $crashSendOutput = ($runResult.CrashSendOutput | Out-String)
                    throw "crash-send did not complete flush. The crash envelope was likely not sent. Output:`n$crashSendOutput"
                }

                $eventId = Get-EventIds -AppOutput $script:runResult.Output -ExpectedCount 1
                if ($eventId) {
                    Write-Host "::group::Getting event content"
                    $script:runEvent = Get-SentryTestEvent -TagName "test.crash_id" -TagValue "$eventId" -TimeoutSeconds 300
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
}
