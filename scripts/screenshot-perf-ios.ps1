<#
.SYNOPSIS
    Measures the performance impact of screenshot attachments on a connected iOS device.

.DESCRIPTION
    Builds the screenshot performance harness from the local sample, installs it on the device, runs it to
    completion and pulls the results. Close the Unity Editor on the sample before running - the build runs
    headlessly through Unity CLI.

.EXAMPLE
    pwsh scripts/screenshot-perf-ios.ps1
    pwsh scripts/screenshot-perf-ios.ps1 -Device "iPhone Sentry" -Label branch
    pwsh scripts/screenshot-perf-ios.ps1 -SkipBuild -Label rerun
    pwsh scripts/screenshot-perf-ios.ps1 -SkipBuild -Configs baseline,high-250ms -Rounds 2
#>

param(
    # Device name or identifier as listed by `xcrun devicectl list devices`. Defaults to the only connected iPhone.
    [string] $Device,
    # Distinguishes runs from each other, e.g. the SDK version under test
    [string] $Label = "run",
    [string] $BundleId = "com.DefaultCompany.unity-of-bugs",
    # Configurations to run, by name (see ScreenshotPerfHarness). Defaults to all of them.
    [string[]] $Configs,
    # Rounds per configuration. Defaults to the harness' default.
    [int] $Rounds,
    [switch] $SkipBuild,
    [int] $TimeoutMinutes = 30
)

Set-StrictMode -Version latest
$ErrorActionPreference = "Stop"

. $PSScriptRoot/unity-pipeline.ps1

$repoRoot = (Resolve-Path "$PSScriptRoot/..").Path
$projectPath = Join-Path $repoRoot "samples/unity-of-bugs-local"
$buildRoot = Join-Path $projectPath "Builds/iOS-ScreenshotPerf"
$xcodeProjectPath = Join-Path $buildRoot "xcode"
$derivedDataPath = Join-Path $buildRoot "DerivedData"
$resultsRoot = Join-Path $repoRoot "artifacts/screenshot-perf"

function Get-ConnectedDevice([string] $nameOrId) {
    $jsonPath = [System.IO.Path]::GetTempFileName()
    try {
        xcrun devicectl list devices --json-output $jsonPath | Out-Null
        $devices = @((Get-Content $jsonPath -Raw | ConvertFrom-Json).result.devices | Where-Object {
                $_.hardwareProperties.platform -eq "iOS" -and
                $_.hardwareProperties.reality -eq "physical" -and
                $_.connectionProperties.tunnelState -ne "unavailable"
            })
    }
    finally {
        Remove-Item $jsonPath -Force -ErrorAction Ignore
    }

    if ($nameOrId) {
        $devices = @($devices | Where-Object { $_.deviceProperties.name -eq $nameOrId -or $_.identifier -eq $nameOrId })
    }

    if ($devices.Count -eq 0) {
        throw "No connected iOS device found$(if ($nameOrId) { " matching '$nameOrId'" }). Connect and unlock the device, then check 'xcrun devicectl list devices'."
    }
    if ($devices.Count -gt 1) {
        throw "Multiple connected iOS devices found: $($devices.deviceProperties.name -join ', '). Pick one with -Device."
    }

    return $devices[0]
}

$targetDevice = Get-ConnectedDevice $Device
$deviceId = $targetDevice.identifier
Write-Host "Using device '$($targetDevice.deviceProperties.name)' ($($targetDevice.hardwareProperties.productType))"

if (-not $SkipBuild) {
    if ((Get-UnityExecutionMode -ProjectPath $projectPath) -eq "Pipeline") {
        throw "A Unity Editor has the sample open. Close it so the build can run headlessly."
    }

    Write-Host "Building the Xcode project..."
    $run = Invoke-UnityCli -Arguments @(
        "build", $projectPath,
        "--target", "iOS",
        "--execute-method", "Editor.ScreenshotPerfBuilder.Build",
        "--output-path", $xcodeProjectPath
    )
    if ($run.ExitCode -ne 0) {
        throw "Unity build failed with exit code $($run.ExitCode):`n$($run.Output)"
    }

    # Building for the device lets automatic signing register it with the team and include it in the profile
    Write-Host "Building and signing the app..."
    xcodebuild `
        -project (Join-Path $xcodeProjectPath "Unity-iPhone.xcodeproj") `
        -scheme Unity-iPhone `
        -configuration Release `
        -destination "id=$($targetDevice.hardwareProperties.udid)" `
        -derivedDataPath $derivedDataPath `
        -allowProvisioningUpdates `
        -quiet `
        build
    if ($LASTEXITCODE -ne 0) {
        throw "xcodebuild failed with exit code $LASTEXITCODE."
    }
}

$appPath = Get-ChildItem -Path (Join-Path $derivedDataPath "Build/Products/Release-iphoneos") -Filter "*.app" -Directory |
    Select-Object -First 1
if (-not $appPath) {
    throw "No app found under $derivedDataPath. Run without -SkipBuild first."
}

Write-Host "Installing $($appPath.Name)..."
xcrun devicectl device install app --device $deviceId $appPath.FullName --quiet
if ($LASTEXITCODE -ne 0) {
    throw "Installing the app failed with exit code $LASTEXITCODE."
}

# Clear results from earlier runs so the pull only contains this one
$emptyDirectory = Join-Path ([System.IO.Path]::GetTempPath()) "screenshot-perf-empty"
New-Item -ItemType Directory -Force -Path $emptyDirectory | Out-Null
xcrun devicectl device copy to --device $deviceId --domain-type appDataContainer --domain-identifier $BundleId `
    --source $emptyDirectory --destination "Documents/screenshot-perf" --remove-existing-content true --quiet 2>$null

$environment = @{}
if ($Configs) { $environment["SCREENSHOT_PERF_CONFIGS"] = $Configs -join "," }
if ($Rounds) { $environment["SCREENSHOT_PERF_ROUNDS"] = "$Rounds" }
$launchArguments = @("device", "process", "launch", "--device", $deviceId, "--terminate-existing", "--console", "--timeout", ($TimeoutMinutes * 60))
if ($environment.Count) {
    $launchArguments += "--environment-variables", ($environment | ConvertTo-Json -Compress)
}

Write-Host "Running the harness. A full run takes about 17 minutes - keep the device unlocked and the app in the foreground."
xcrun devicectl @launchArguments $BundleId |
    Select-String -Pattern "\[ScreenshotPerf\]" | ForEach-Object { Write-Host $_.Line }
if ($LASTEXITCODE -ne 0) {
    throw "Launching the harness failed with exit code $LASTEXITCODE. Make sure the device is unlocked, then rerun with -SkipBuild."
}

$resultsPath = Join-Path $resultsRoot "$(Get-Date -Format 'yyyyMMdd-HHmmss')-$Label"
New-Item -ItemType Directory -Force -Path $resultsPath | Out-Null
xcrun devicectl device copy from --device $deviceId --domain-type appDataContainer --domain-identifier $BundleId `
    --source "Documents/screenshot-perf" --destination $resultsPath --quiet
if ($LASTEXITCODE -ne 0) {
    throw "Pulling the results failed with exit code $LASTEXITCODE."
}

$runDirectory = Get-ChildItem -Path $resultsPath -Recurse -Filter "done" -File | Select-Object -First 1
if (-not $runDirectory) {
    throw "The harness did not complete. Partial results, if any, are in $resultsPath."
}

Write-Host "Results: $($runDirectory.DirectoryName)"
python3 -I (Join-Path $PSScriptRoot "screenshot-perf-report.py") $runDirectory.DirectoryName
