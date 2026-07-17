#!/usr/bin/env pwsh

param(
    [Parameter(Mandatory = $true)]
    [string]$CocoaRoot,

    [Parameter(Mandatory = $true)]
    [string]$iOSDestination,

    [Parameter(Mandatory = $true)]
    [string]$macOSDestination,

    [switch]$Clean
)

Set-StrictMode -Version latest
$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true

if (-not (Test-Path (Join-Path $CocoaRoot "Sentry.xcodeproj"))) {
    Write-Error "sentry-cocoa submodule not checked out at: $CocoaRoot`nRun: git submodule update --init modules/sentry-cocoa"
    exit 1
}

# All build artifacts go under XCFrameworkBuildPath/ which is already in sentry-cocoa's .gitignore.
$buildPath = Join-Path $CocoaRoot "XCFrameworkBuildPath"
$iOSXcframeworkPath = Join-Path $buildPath "SentryObjC-Dynamic-iOS.xcframework"
$macOSXcframeworkPath = Join-Path $buildPath "SentryObjC-Dynamic-macOS.xcframework"

if ($Clean -and (Test-Path $buildPath)) {
    Write-Host "Clean build requested — removing $buildPath" -ForegroundColor Yellow
    Remove-Item -Path $buildPath -Recurse -Force
}

Write-Host "Building Cocoa SDK from source..." -ForegroundColor Yellow

Push-Location $CocoaRoot
try {
    ################ Build and set up iOS support ################

    if (-not (Test-Path $iOSXcframeworkPath)) {
        Write-Host "Building iOS xcframework..." -ForegroundColor Yellow
        # SentryObjC is the public ObjC wrapper SDK. The dynamic xcframework embeds the full SDK
        # (SentryObjC + SentryObjCCompat + SentryObjCInternal) into a single self-contained framework,
        # so Unity links/dlopens only one binary. The dynamic slices are arm64-only (no arm64e), so no
        # architecture stripping is needed — unlike the main `Sentry` scheme's Xcode-project build.
        & ./scripts/build-xcframework-sentryobjc.sh --sdks "iphoneos,iphonesimulator" --variant dynamic
        & ./scripts/validate-xcframework-format.sh --xcframework "SentryObjC-Dynamic.xcframework"
        # build-xcframework-sentryobjc.sh outputs to the working directory — move into our build cache
        Move-Item -Path "SentryObjC-Dynamic.xcframework" -Destination $iOSXcframeworkPath -Force
        # Clean up intermediate archives, keep the final xcframework
        $archivePath = Join-Path $buildPath "archive"
        if (Test-Path $archivePath) {
            Remove-Item -Path $archivePath -Recurse -Force
        }
    }

    Write-Host "Setting up iOS frameworks..." -ForegroundColor Yellow

    if (Test-Path $iOSDestination) {
        Remove-Item -Path $iOSDestination -Recurse -Force
    }

    # Copy the xcframework as-is, including dSYMs. Since we build from source, the debug symbols
    # won't be on Sentry's symbol server — they need to ship in the package so the Xcode build phase
    # can upload them via sentry-cli, consistent with how all other native SDKs ship their debug symbols.
    Copy-Item -Path $iOSXcframeworkPath -Destination $iOSDestination -Recurse -Force

    $iOSInfoPlist = Join-Path $iOSDestination "Info.plist"
    if (-not (Test-Path $iOSInfoPlist)) {
        Write-Error "Failed to set up the iOS SDK."
        exit 1
    }
    Write-Host "iOS SDK set up at: $iOSDestination" -ForegroundColor Green

    ################ Build and set up macOS support ################

    if (-not (Test-Path $macOSXcframeworkPath)) {
        Write-Host "Building macOS xcframework..." -ForegroundColor Yellow
        & ./scripts/build-xcframework-sentryobjc.sh --sdks "macosx" --variant dynamic
        & ./scripts/validate-xcframework-format.sh --xcframework "SentryObjC-Dynamic.xcframework"
        Move-Item -Path "SentryObjC-Dynamic.xcframework" -Destination $macOSXcframeworkPath -Force
        # Clean up all remaining build intermediates
        foreach ($dir in @("archive", "DerivedData")) {
            $dirPath = Join-Path $buildPath $dir
            if (Test-Path $dirPath) {
                Remove-Item -Path $dirPath -Recurse -Force
            }
        }
    }

    Write-Host "Setting up macOS support..." -ForegroundColor Yellow

    $macOSSlice = Get-ChildItem -Path $macOSXcframeworkPath -Directory | Where-Object { $_.Name -like "macos-*" } | Select-Object -First 1
    if (-not $macOSSlice) {
        Write-Error "No macOS slice found in xcframework at: $macOSXcframeworkPath"
        exit 1
    }
    $macOSFrameworkPath = Join-Path $macOSSlice.FullName "SentryObjC.framework/Versions/A/SentryObjC"
    $macOSdSYMPath = Join-Path $macOSSlice.FullName "dSYMs/SentryObjC.framework.dSYM/Contents/Resources/DWARF/SentryObjC"

    $macOSDestDir = Split-Path $macOSDestination -Parent
    if (-not (Test-Path $macOSDestDir)) {
        New-Item -ItemType Directory -Path $macOSDestDir -Force | Out-Null
    }

    if (-not (Test-Path $macOSFrameworkPath)) {
        Write-Error "macOS framework not found at: $macOSFrameworkPath"
        exit 1
    }
    Copy-Item -Path $macOSFrameworkPath -Destination $macOSDestination -Force
    Write-Host "Copied macOS dylib to: $macOSDestination" -ForegroundColor Green

    $macOSdSYMDestination = "$macOSDestination.dSYM"
    if (-not (Test-Path $macOSdSYMPath)) {
        Write-Error "macOS dSYM not found at: $macOSdSYMPath"
        exit 1
    }
    Copy-Item -Path $macOSdSYMPath -Destination $macOSdSYMDestination -Force
    Write-Host "Copied macOS dSYM to: $macOSdSYMDestination" -ForegroundColor Green
} finally {
    Pop-Location
}

Write-Host "Cocoa SDK build completed successfully!" -ForegroundColor Green
