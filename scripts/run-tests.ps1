<#
.SYNOPSIS
    Runs Unity tests for the Sentry SDK for Unity.

.DESCRIPTION
    This script builds the SDK and runs PlayMode and/or EditMode tests with optional filtering.

.PARAMETER PlayMode
    Run PlayMode tests (runtime tests).

.PARAMETER EditMode
    Run EditMode tests (editor tests).

.PARAMETER Filter
    Test name filter passed to Unity's -testFilter (regex supported).

.PARAMETER Category
    Test category filter passed to Unity's -testCategory.

.PARAMETER UnityVersion
    Override Unity version (default: read from ProjectVersion.txt or $env:UNITY_VERSION).

.PARAMETER SkipBuild
    Skip the dotnet build step (for faster iteration when DLLs are current).

.EXAMPLE
    pwsh scripts/run-tests.ps1
    # Runs all tests (PlayMode + EditMode)

.EXAMPLE
    pwsh scripts/run-tests.ps1 -PlayMode -Filter "Throttler"
    # Runs only PlayMode tests matching "Throttler"

.EXAMPLE
    pwsh scripts/run-tests.ps1 -SkipBuild -EditMode
    # Runs EditMode tests without rebuilding
#>

param(
    [switch] $PlayMode,
    [switch] $EditMode,
    [string] $Filter,
    [string] $Category,
    [string] $UnityVersion,
    [switch] $SkipBuild
)

Set-StrictMode -Version latest
$ErrorActionPreference = "Stop"

. $PSScriptRoot/test-utils.ps1

$repoRoot = Resolve-Path "$PSScriptRoot/.."
$sampleProject = "$repoRoot/samples/unity-of-bugs"

# Default to both if neither specified
if (-not $PlayMode -and -not $EditMode) {
    $PlayMode = $true
    $EditMode = $true
}

# Find Unity version
if (-not $UnityVersion) {
    $UnityVersion = $env:UNITY_VERSION
    if (-not $UnityVersion) {
        $projectVersionFile = "$sampleProject/ProjectSettings/ProjectVersion.txt"
        if (-not (Test-Path $projectVersionFile)) {
            Write-Host "Error: ProjectVersion.txt not found at $projectVersionFile" -ForegroundColor Red
            exit 1
        }
        $content = Get-Content $projectVersionFile -Raw
        $match = [regex]::Match($content, "m_EditorVersion:\s*(.+)")
        if (-not $match.Success) {
            Write-Host "Error: Could not parse Unity version from ProjectVersion.txt" -ForegroundColor Red
            exit 1
        }
        $UnityVersion = $match.Groups[1].Value.Trim()
    }
}
# Find Unity path (platform-specific)
if ($IsMacOS) {
    $unityPath = "/Applications/Unity/Hub/Editor/$UnityVersion/Unity.app/Contents/MacOS/Unity"
}
elseif ($IsWindows) {
    $unityPath = "C:/Program Files/Unity/Hub/Editor/$UnityVersion/Editor/Unity.exe"
}
elseif ($IsLinux) {
    $unityPath = "$env:HOME/Unity/Hub/Editor/$UnityVersion/Editor/Unity"
}
else {
    Write-Host "Error: Unsupported platform" -ForegroundColor Red
    exit 1
}

if (-not (Test-Path $unityPath)) {
    Write-Host "Error: Unity $UnityVersion not found at: $unityPath" -ForegroundColor Red
    exit 1
}

# Build SDK
if (-not $SkipBuild) {
    Write-Host "Building SDK... " -NoNewline
    $buildStart = Get-Date
    & dotnet build "$repoRoot" --configuration Release --verbosity quiet 2>&1 | Out-Null
    if ($LASTEXITCODE -ne 0) {
        Write-Host "[FAIL]" -ForegroundColor Red
        exit 1
    }
    $buildTime = (Get-Date) - $buildStart
    Write-Host "[OK] ($([math]::Round($buildTime.TotalSeconds, 1))s)" -ForegroundColor Green
}

# Build test arguments
function Build-TestArgs([string] $testPlatform, [string] $resultsPath) {
    $testArgs = @(
        "-batchmode", "-nographics", "-runTests",
        "-testPlatform", $testPlatform,
        "-projectPath", $sampleProject,
        "-testResults", $resultsPath
    )
    if ($Filter) { $testArgs += @("-testFilter", $Filter) }
    if ($Category) { $testArgs += @("-testCategory", $Category) }
    return $testArgs
}

# Run Unity quietly and return success/failure
function Run-Unity-Quiet([string] $unityExe, [string[]] $arguments) {
    $logFile = "$repoRoot/unity-test.log"
    $arguments += @("-logFile", $logFile)

    # Remove old log
    Remove-Item $logFile -ErrorAction SilentlyContinue

    # Run Unity and wait for completion
    $process = Start-Process -FilePath $unityExe -ArgumentList $arguments -PassThru
    $process | Wait-Process

    return $process.ExitCode
}

# Run tests and report results
function Run-Tests([string] $name, [string] $platform, [string] $resultsPath) {
    $filterInfo = if ($Filter) { " (filter: `"$Filter`")" } else { "" }
    Write-Host "Running $name tests$filterInfo... " -NoNewline

    # Ensure results directory exists
    $resultsDir = Split-Path $resultsPath -Parent
    if (-not (Test-Path $resultsDir)) {
        New-Item -ItemType Directory -Path $resultsDir -Force | Out-Null
    }

    # Remove old results
    Remove-Item $resultsPath -ErrorAction SilentlyContinue

    # Build and run Unity quietly
    $testArgs = Build-TestArgs $platform $resultsPath
    $null = Run-Unity-Quiet $unityPath $testArgs

    # Parse and display results
    $results = Parse-TestResults $resultsPath
    if ($null -eq $results) {
        Write-Host "[FAIL] Could not parse test results" -ForegroundColor Red
        Write-Host "  Check unity-test.log for details" -ForegroundColor DarkGray
        return $false
    }

    if ($results.Total -eq 0) {
        Write-Host "[WARN] No tests found" -ForegroundColor Yellow
        return $true
    }

    $symbol = if ($results.Success) { "[PASS]" } else { "[FAIL]" }
    $color = if ($results.Success) { "Green" } else { "Red" }
    $duration = [math]::Round($results.Duration, 1)

    Write-Host "$symbol $($results.Passed) passed, $($results.Failed) failed, $($results.Inconclusive) inconclusive ($duration`s)" -ForegroundColor $color

    # Show failed test details
    if ($results.Failed -gt 0) {
        Write-Host ""
        foreach ($test in $results.FailedTests) {
            Write-Host "  FAILED: $($test.Name)" -ForegroundColor Red
            if ($test.Message) {
                $test.Message.Trim() -split "`n" | Select-Object -First 3 | ForEach-Object {
                    Write-Host "    $($_.Trim())" -ForegroundColor DarkGray
                }
            }
        }
    }

    return $results.Success
}

# Run requested tests
$allPassed = $true

if ($PlayMode) {
    $result = Run-Tests "PlayMode" "PlayMode" "$repoRoot/artifacts/test/playmode/results.xml"
    if ($result -ne $true) { $allPassed = $false }
}

if ($EditMode) {
    $result = Run-Tests "EditMode" "EditMode" "$repoRoot/artifacts/test/editmode/results.xml"
    if ($result -ne $true) { $allPassed = $false }
}

# Final summary
if ($allPassed) {
    Write-Host "`nAll tests passed." -ForegroundColor Green
    exit 0
}
else {
    Write-Host "`nTests failed." -ForegroundColor Red
    exit 1
}
