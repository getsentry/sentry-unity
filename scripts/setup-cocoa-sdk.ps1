#!/usr/bin/env pwsh

param(
    [Parameter(Mandatory=$true)]
    [string]$RepoRoot,
    
    [Parameter(Mandatory=$true)]
    [string]$CocoaVersion,
    
    [Parameter(Mandatory=$true)]
    [string]$CocoaCache,
    
    [Parameter(Mandatory=$true)]
    [string]$iOSDestination,
    
    [Parameter(Mandatory=$true)]
    [string]$macOSDestination,
    
    [switch]$iOSOnly
)

# Clean cache if version does not exist to get rid of old versions
$zipFile = Join-Path $CocoaCache "Sentry-Dynamic-$CocoaVersion.xcframework.zip"
if (-not (Test-Path $zipFile)) {
    Write-Host "Cleaning cache directory for new version..." -ForegroundColor Yellow
    if (Test-Path $CocoaCache) {
        Remove-Item -Path $CocoaCache -Recurse -Force
    }
}

if (-not (Test-Path $CocoaCache)) {
    New-Item -ItemType Directory -Path $CocoaCache -Force | Out-Null
}

if (-not (Test-Path $zipFile)) {
    Write-Host "Downloading Cocoa SDK version '$CocoaVersion'..." -ForegroundColor Yellow
    $downloadUrl = "https://github.com/getsentry/sentry-cocoa/releases/download/$CocoaVersion/Sentry-Dynamic.xcframework.zip"
    Invoke-WebRequest -Uri $downloadUrl -OutFile $zipFile
}

$xcframeworkPath = Join-Path $CocoaCache "Sentry-Dynamic.xcframework"
if (-not (Test-Path $xcframeworkPath)) {
    Write-Host "Extracting xcframework..." -ForegroundColor Yellow
    Expand-Archive -Path $zipFile -DestinationPath $CocoaCache -Force
}

################ Set up iOS support ################
# We strip out the iOS frameworks and create a new xcframework out of those.

Write-Host "Setting up iOS frameworks..." -ForegroundColor Yellow

$iOSFrameworks = Get-ChildItem -Path $xcframeworkPath -Directory | Where-Object { $_.Name -like "ios-*" }
if ($iOSFrameworks.Count -eq 0) {
    Write-Error "No iOS frameworks found in xcframework at: $xcframeworkPath"
    exit 1
}

Write-Host "Found $($iOSFrameworks.Count) iOS frameworks:" -ForegroundColor Green
foreach ($framework in $iOSFrameworks) {
    Write-Host "  - $($framework.Name)" -ForegroundColor Cyan
}

$xcodebuildArgs = @("-create-xcframework")

foreach ($framework in $iOSFrameworks) {
    $frameworkPath = Join-Path $framework.FullName "Sentry.framework"
    if (Test-Path $frameworkPath) {
        $xcodebuildArgs += "-framework"
        $xcodebuildArgs += $frameworkPath
        Write-Host "Adding framework: $frameworkPath" -ForegroundColor Cyan
    } else {
        Write-Warning "Framework not found at: $frameworkPath"
    }
}

# Remove the ~ suffix from destination. xcodebuild requires the output path to end with `.xcframework`
$xcframeworkDestination = $iOSDestination.TrimEnd('~', '/')

$xcodebuildArgs += "-output"
$xcodebuildArgs += $xcframeworkDestination

Write-Host "Creating iOS-only xcframework..." -ForegroundColor Yellow
Write-Host "Command: xcodebuild $($xcodebuildArgs -join ' ')" -ForegroundColor Gray

try {
    & xcodebuild @xcodebuildArgs
    if ($LASTEXITCODE -ne 0) {
        Write-Error "xcodebuild failed with exit code: $LASTEXITCODE"
        exit 1
    }
    Write-Host "Successfully created iOS-only xcframework at: $xcframeworkDestination" -ForegroundColor Green
} catch {
    Write-Error "Failed to run xcodebuild: $($_.Exception.Message)"
    exit 1
}

Write-Host "Appending '~' for Unity to ignore the framework"
Move-Item -Path $xcframeworkDestination -Destination $iOSDestination

$iOSInfoPlist = Join-Path $iOSDestination "Info.plist"
if (-not (Test-Path $iOSDestination) -or -not (Test-Path $iOSInfoPlist)) {
    Write-Error "Failed to set up the iOS SDK."
    exit 1
}

################ Set up macOS support ################
# We copy the .dylib and the .dSYM directly into the plugins folder

if (-not $iOSOnly) {
    Write-Host "Setting up macOS support..." -ForegroundColor Yellow
    
    $macOSFrameworkPath = Join-Path $xcframeworkPath "macos-arm64_arm64e_x86_64/Sentry.framework/Sentry"
    $macOSdSYMPath = Join-Path $xcframeworkPath "macos-arm64_arm64e_x86_64/dSYMs/Sentry.framework.dSYM/Contents/Resources/DWARF/Sentry"
    
    $macOSDestDir = Split-Path $macOSDestination -Parent
    if (-not (Test-Path $macOSDestDir)) {
        New-Item -ItemType Directory -Path $macOSDestDir -Force | Out-Null
    }
    
    if (Test-Path $macOSFrameworkPath) {
        Copy-Item -Path $macOSFrameworkPath -Destination $macOSDestination -Force
        Write-Host "Copied macOS dylib to: $macOSDestination" -ForegroundColor Green
    } else {
        Write-Error "macOS framework not found at: $macOSFrameworkPath"
        exit 1
    }
    
    $macOSdSYMDestination = "$macOSDestination.dSYM"
    if (Test-Path $macOSdSYMPath) {
        Copy-Item -Path $macOSdSYMPath -Destination $macOSdSYMDestination -Force
        Write-Host "Copied macOS dSYM to: $macOSdSYMDestination" -ForegroundColor Green
    } else {
        Write-Error "macOS dSYM not found at: $macOSdSYMPath"
        exit 1
    }
    
    if (-not (Test-Path $macOSDestination) -or -not (Test-Path $macOSdSYMDestination)) {
        Write-Error "Failed to set up the macOS SDK."
        exit 1
    }
}

Write-Host "Cocoa SDK setup completed successfully!" -ForegroundColor Green