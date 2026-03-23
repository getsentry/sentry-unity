#!/usr/bin/env pwsh

param(
    [Parameter(Mandatory = $true)]
    [string]$CocoaRoot,

    [Parameter(Mandatory = $true)]
    [string]$iOSDestination,

    [Parameter(Mandatory = $true)]
    [string]$macOSDestination
)

Set-StrictMode -Version latest
$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true

if (-not (Test-Path (Join-Path $CocoaRoot "Sentry.xcodeproj"))) {
    Write-Error "sentry-cocoa submodule not checked out at: $CocoaRoot`nRun: git submodule update --init modules/sentry-cocoa"
    exit 1
}

Write-Host "Building Cocoa SDK from source..." -ForegroundColor Yellow

Push-Location $CocoaRoot
try {
    ################ Build and set up iOS support ################

    $iOSXcframeworkPath = Join-Path $CocoaRoot "Sentry-Dynamic-iOS.xcframework"

    if (-not (Test-Path $iOSXcframeworkPath)) {
        Write-Host "Building iOS xcframework..." -ForegroundColor Yellow
        & ./scripts/build-xcframework-variant.sh "Sentry" "-Dynamic" "mh_dylib" "" "iOSOnly" "arm64e"
        if ($LASTEXITCODE -ne 0) {
            Write-Error "Failed to build iOS xcframework"
            exit 1
        }
        # build-xcframework-variant.sh produces Sentry-Dynamic.xcframework — rename to keep iOS and macOS separate
        Move-Item -Path "Sentry-Dynamic.xcframework" -Destination $iOSXcframeworkPath -Force
        if (Test-Path "XCFrameworkBuildPath/archive") {
            Remove-Item -Path "XCFrameworkBuildPath/archive" -Recurse -Force
        }
    }

    Write-Host "Setting up iOS frameworks..." -ForegroundColor Yellow

    $iOSFrameworks = Get-ChildItem -Path $iOSXcframeworkPath -Directory | Where-Object { $_.Name -like "ios-*" -and $_.Name -notlike "*maccatalyst*" }
    if ($iOSFrameworks.Count -eq 0) {
        Write-Error "No iOS frameworks found in xcframework at: $iOSXcframeworkPath"
        exit 1
    }

    if (Test-Path $iOSDestination) {
        Remove-Item -Path $iOSDestination -Recurse -Force
    }
    Copy-Item -Path $iOSXcframeworkPath -Destination $iOSDestination -Recurse -Force

    $iOSInfoPlist = Join-Path $iOSDestination "Info.plist"
    if (-not (Test-Path $iOSInfoPlist)) {
        Write-Error "Failed to set up the iOS SDK."
        exit 1
    }
    Write-Host "iOS SDK set up at: $iOSDestination" -ForegroundColor Green

    ################ Build and set up macOS support ################

    $macOSXcframeworkPath = Join-Path $CocoaRoot "Sentry-Dynamic-macOS.xcframework"

    if (-not (Test-Path $macOSXcframeworkPath)) {
        Write-Host "Building macOS xcframework..." -ForegroundColor Yellow
        & ./scripts/build-xcframework-variant.sh "Sentry" "-Dynamic" "mh_dylib" "" "macOSOnly" ""
        if ($LASTEXITCODE -ne 0) {
            Write-Error "Failed to build macOS xcframework"
            exit 1
        }
        Move-Item -Path "Sentry-Dynamic.xcframework" -Destination $macOSXcframeworkPath -Force
        if (Test-Path "XCFrameworkBuildPath") {
            Remove-Item -Path "XCFrameworkBuildPath" -Recurse -Force
        }
    }

    Write-Host "Setting up macOS support..." -ForegroundColor Yellow

    $macOSFrameworkPath = Join-Path $macOSXcframeworkPath "macos-arm64_x86_64/Sentry.framework/Versions/A/Sentry"
    $macOSdSYMPath = Join-Path $macOSXcframeworkPath "macos-arm64_x86_64/dSYMs/Sentry.framework.dSYM/Contents/Resources/DWARF/Sentry"

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
