<#
.SYNOPSIS
    Builds and runs Unity tests through an open Unity Pipeline Editor.

.DESCRIPTION
    This local test harness connects to the Unity Editor already running the
    target project. It validates the test result payload and exits with code 1
    for failed, inconclusive, empty, or malformed test runs.
    Filter accepts literal, case-insensitive partial test names. Unity CLI does
    not support regular expression filters.

.EXAMPLE
    pwsh scripts/run-tests.ps1

.EXAMPLE
    pwsh scripts/run-tests.ps1 -Mode EditMode -Filter "EditorModeTests"

.EXAMPLE
    pwsh scripts/run-tests.ps1 -Mode PlayMode -SkipBuild
#>

param(
    [ValidateSet("All", "EditMode", "PlayMode")]
    [string] $Mode = "All",
    [string] $Filter,
    [switch] $SkipBuild
)

Set-StrictMode -Version latest
$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path "$PSScriptRoot/..").Path
$testTimeout = 300

function Limit-Text([object] $Value) {
    $text = [string] $Value
    if ($text.Length -le 2000) {
        return $text
    }

    return "$($text.Substring(0, 2000))`n... truncated"
}

function Test-UnityProject([string] $Path) {
    $projectVersionFile = Join-Path $Path "ProjectSettings/ProjectVersion.txt"
    if (-not (Test-Path $projectVersionFile -PathType Leaf)) {
        throw "Unity project at $Path has no ProjectSettings/ProjectVersion.txt."
    }

    $projectVersionMatch = [regex]::Match((Get-Content $projectVersionFile -Raw), "m_EditorVersion:\s*(.+)")
    if (-not $projectVersionMatch.Success) {
        throw "Could not parse Unity version from $projectVersionFile."
    }

    $projectVersion = $projectVersionMatch.Groups[1].Value.Trim()
    $versionMatch = [regex]::Match($projectVersion, "^(?<major>\d+)\.(?<minor>\d+)")
    if (-not $versionMatch.Success) {
        throw "Could not parse Unity version '$projectVersion' from $projectVersionFile."
    }

    $major = [int]$versionMatch.Groups["major"].Value
    $minor = [int]$versionMatch.Groups["minor"].Value
    if (($major -ne 6 -and $major -ne 6000) -or $minor -lt 6) {
        throw "Unity project at $Path uses $projectVersion. This harness requires Unity 6.6 (6000.6) or newer."
    }

    $manifestFile = Join-Path $Path "Packages/manifest.json"
    if (-not (Test-Path $manifestFile -PathType Leaf)) {
        throw "Unity project at $Path has no Packages/manifest.json."
    }

    $manifest = Get-Content $manifestFile -Raw | ConvertFrom-Json

    if (-not $manifest.dependencies."com.unity.pipeline") {
        throw "Unity project at $Path must install com.unity.pipeline before this harness can connect."
    }

    $packageSource = $manifest.dependencies."io.sentry.unity.dev"
    if (-not $packageSource -or -not $packageSource.StartsWith("file:")) {
        throw "Unity project at $Path must install io.sentry.unity.dev from this checkout's package-dev directory."
    }

    if ($packageSource.StartsWith("file://")) {
        $packagePath = ([Uri]$packageSource).LocalPath
    }
    else {
        $packagePath = [Uri]::UnescapeDataString($packageSource.Substring("file:".Length))
    }

    if (-not [IO.Path]::IsPathRooted($packagePath)) {
        $packagePath = Join-Path $Path $packagePath
    }

    if (-not (Test-Path $packagePath -PathType Container)) {
        throw "io.sentry.unity.dev points to missing package-dev directory $packagePath."
    }

    $expectedPackagePath = (Resolve-Path (Join-Path $repoRoot "package-dev")).Path
    $packagePath = (Resolve-Path $packagePath).Path
    if ($packagePath -ne $expectedPackagePath) {
        throw "io.sentry.unity.dev points to $packagePath. This harness requires $expectedPackagePath so it tests this checkout's SDK build."
    }
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

if (-not (Get-Command unity -ErrorAction SilentlyContinue)) {
    throw "Unity CLI executable 'unity' was not found on PATH."
}

$ProjectPath = Join-Path $repoRoot "samples/unity-of-bugs-local"
if (-not (Test-Path $ProjectPath -PathType Container)) {
    throw "Local test project was not found at $ProjectPath. Create it with Unity 6.6+, com.unity.pipeline, and io.sentry.unity.dev linked to $repoRoot/package-dev."
}

$ProjectPath = (Resolve-Path $ProjectPath).Path
Test-UnityProject $ProjectPath

$modes = switch ($Mode) {
    "EditMode" { @("editor") }
    "PlayMode" { @("playmode") }
    default { @("editor", "playmode") }
}

Wait-ForEditorReady

if (-not $SkipBuild) {
    $buildOutput = & dotnet build $repoRoot --configuration Release --verbosity quiet 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "SDK build failed:`n$($buildOutput | Out-String)"
    }
}

Wait-ForRecompile

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
