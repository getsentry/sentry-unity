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
        Destination = Join-Path $ArtifactsDestination "Windows"
        CheckFile = "Sentry/sentry.dll"
    },
    @{
        Name = "Linux"
        Destination = Join-Path $ArtifactsDestination "Linux"
        CheckFile = "Sentry/libsentry.so"
    },
    @{
        Name = "Android"
        Destination = Join-Path $ArtifactsDestination "Android"
        CheckDir = "Sentry~"
        ExpectedFileCount = 4
    }
)

function Test-SDKPresent {
    param($SDK)

    if ($SDK.ContainsKey('CheckFile')) {
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

function Download-SDK {
    param(
        [Parameter(Mandatory)]
        [string]$Name,
        [Parameter(Mandatory)]
        [string]$Destination,
        [Parameter(Mandatory)]
        [string]$RunId
    )

    Write-Host "Downloading $Name SDK..." -ForegroundColor Yellow

    # Remove existing directory if present (partial download)
    if (Test-Path $Destination) {
        Write-Host "  Removing existing directory..." -ForegroundColor Gray
        Remove-Item -Path $Destination -Recurse -Force
    }

    $artifactName = "$Name-sdk"
    gh run download $RunId -n $artifactName -D $Destination

    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to download $Name SDK"
        exit 1
    }

    Write-Host "  Downloaded $Name SDK successfully" -ForegroundColor Green
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

# Fetch run ID only if we need to download something
$runId = Get-LatestSuccessfulRunId

foreach ($sdk in $sdksToDownload) {
    Download-SDK -Name $sdk.Name -Destination $sdk.Destination -RunId $runId
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
