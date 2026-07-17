#!/usr/bin/env pwsh

param(
    [Parameter()]
    [string]$RepoRoot = "$PSScriptRoot/.."
)

Set-StrictMode -Version latest
$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true

$ArtifactsDestination = Join-Path $RepoRoot "package-dev/Plugins"

# SDK definitions with their existence checks
$SDKs = @(
    @{
        Name = "Windows"
        Destination = Join-Path $ArtifactsDestination "Windows/Sentry~"
        CheckFile = "sentry.dll"
    },
    @{
        Name = "WindowsNative"
        Destination = Join-Path $ArtifactsDestination "Windows/SentryNative~"
        CheckFile = "sentry.dll"
    },
    @{
        Name = "Linux"
        Destination = Join-Path $ArtifactsDestination "Linux/Sentry~"
        CheckFile = "libsentry.so"
    },
    @{
        Name = "LinuxNative"
        Destination = Join-Path $ArtifactsDestination "Linux/SentryNative~"
        CheckFile = "libsentry.so"
    },
    @{
        Name = "Android"
        Destination = Join-Path $ArtifactsDestination "Android"
        CheckDir = "Sentry~"
        ExpectedFileCount = 4
    },
    @{
        Name = "Cocoa"
        Destination = $ArtifactsDestination
        CheckFiles = @(
            "iOS/SentryObjC.xcframework~/Info.plist",
            "macOS/Sentry~/Sentry.dylib"
        )
    },
    @{
        Name = "MacOSNative"
        Destination = Join-Path $ArtifactsDestination "macOS/SentryNative~"
        CheckFile = "libsentry.dylib"
    }
)

function Test-SDKPresent {
    param($SDK)

    if ($SDK.ContainsKey('CheckFiles')) {
        foreach ($file in $SDK.CheckFiles) {
            $checkPath = Join-Path $SDK.Destination $file
            if (-not (Test-Path $checkPath)) {
                return $false
            }
        }
        return $true
    }
    elseif ($SDK.ContainsKey('CheckFile')) {
        $checkPath = Join-Path $SDK.Destination $SDK.CheckFile
        return Test-Path $checkPath
    }
    elseif ($SDK.ContainsKey('CheckDir')) {
        $checkPath = Join-Path $SDK.Destination $SDK.CheckDir
        if (-not (Test-Path $checkPath)) {
            return $false
        }
        $fileCount = (Get-ChildItem -Path $checkPath -File).Count
        return $fileCount -ge $SDK.ExpectedFileCount
    }
    return $false
}

function Get-LatestSuccessfulRunId {
    Write-Host "Fetching latest successful CI run ID..." -ForegroundColor Yellow

    $result = gh run list --branch main --workflow CI --json "conclusion,databaseId" --jq 'first(.[] | select(.conclusion == "success") | .databaseId)'

    if (-not $result -or $result -eq "null") {
        Write-Error "Failed to find a successful CI run on main branch"
        exit 1
    }

    Write-Host "Found run ID: $result" -ForegroundColor Green
    return $result
}

function Get-LatestBranchRunId {
    param([Parameter(Mandatory)][string]$Branch)

    # Any conclusion — the SDK build job uploads its artifact before downstream
    # jobs run, so a run that overall failed often still has the SDK artifact.
    $result = gh run list --branch $Branch --workflow CI --limit 1 --json databaseId --jq '.[0].databaseId'
    if (-not $result -or $result -eq 'null') {
        return $null
    }
    return $result
}

function Try-DownloadSDK {
    param(
        [Parameter(Mandatory)]
        [string]$Name,
        [Parameter(Mandatory)]
        [string]$Destination,
        [Parameter(Mandatory)]
        [string]$RunId
    )

    $artifactName = "$Name-sdk"

    $tempDir = Join-Path ([System.IO.Path]::GetTempPath()) "sentry-$Name-sdk-download"
    if (Test-Path $tempDir) {
        Remove-Item -Path $tempDir -Recurse -Force
    }

    # `gh run download` exits non-zero when the artifact doesn't exist in the
    # run; we want to handle that as "not available here" rather than abort.
    $previousEAP = $PSNativeCommandUseErrorActionPreference
    $PSNativeCommandUseErrorActionPreference = $false
    try {
        gh run download $RunId -n $artifactName -D $tempDir 2>$null
        $downloadExit = $LASTEXITCODE
    }
    finally {
        $PSNativeCommandUseErrorActionPreference = $previousEAP
    }
    if ($downloadExit -ne 0) {
        return $false
    }

    if (-not (Test-Path $Destination)) {
        New-Item -ItemType Directory -Path $Destination -Force | Out-Null
    }
    Copy-Item -Path (Join-Path $tempDir "*") -Destination $Destination -Recurse -Force
    Remove-Item -Path $tempDir -Recurse -Force -ErrorAction SilentlyContinue

    return $true
}

# Main logic
Write-Host "Checking native SDK status..." -ForegroundColor Cyan
Write-Host ""

$sdksToDownload = @()

foreach ($sdk in $SDKs) {
    if (Test-SDKPresent $sdk) {
        Write-Host "$($sdk.Name) SDK already present, skipping download." -ForegroundColor Green
    }
    else {
        Write-Host "$($sdk.Name) SDK not found, will download." -ForegroundColor Yellow
        $sdksToDownload += $sdk
    }
}

Write-Host ""

if ($sdksToDownload.Count -eq 0) {
    Write-Host "All native SDKs are already present." -ForegroundColor Green
    exit 0
}

# Primary source is main's latest successful run. For artifacts not yet on
# main (e.g. new native backends under development), fall back to the latest
# CI run on the current branch.
$mainRunId = Get-LatestSuccessfulRunId

$branchRunId = $null
$currentBranch = (git -C $RepoRoot rev-parse --abbrev-ref HEAD).Trim()
if ($currentBranch -and $currentBranch -ne 'main' -and $currentBranch -ne 'HEAD') {
    $branchRunId = Get-LatestBranchRunId -Branch $currentBranch
}

$failed = @()
foreach ($sdk in $sdksToDownload) {
    Write-Host "Downloading $($sdk.Name) SDK..." -ForegroundColor Yellow
    if (Try-DownloadSDK -Name $sdk.Name -Destination $sdk.Destination -RunId $mainRunId) {
        Write-Host "  Downloaded from main run $mainRunId" -ForegroundColor Green
        continue
    }
    if ($branchRunId -and (Try-DownloadSDK -Name $sdk.Name -Destination $sdk.Destination -RunId $branchRunId)) {
        Write-Host "  Not on main; downloaded from branch '$currentBranch' run $branchRunId" -ForegroundColor Yellow
        continue
    }
    $failed += $sdk.Name
}

if ($failed.Count -gt 0) {
    $names = $failed -join ', '
    Write-Error "Could not download these SDK artifacts: $names. Push the branch so CI publishes the artifact, or build locally with Build<Name>SDK."
    exit 1
}

Write-Host ""
Write-Host "Restoring package-dev/Plugins to latest git commit..." -ForegroundColor Yellow
Push-Location $RepoRoot
try {
    git restore package-dev/Plugins
}
finally {
    Pop-Location
}

Write-Host ""
Write-Host "Native SDK download completed successfully!" -ForegroundColor Green
