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
        Destination = Join-Path $ArtifactsDestination "Linux"
        CheckFile = "Sentry/libsentry.so"
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
            "iOS/Sentry.xcframework~/Info.plist",
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

function Get-RunArtifactNames {
    param([Parameter(Mandatory)][string]$RunId)

    try {
        $json = gh api "repos/{owner}/{repo}/actions/runs/$RunId/artifacts" --paginate --jq '[.artifacts[].name]'
    }
    catch {
        return @()
    }
    if (-not $json) {
        return @()
    }
    return @($json | ConvertFrom-Json)
}

function Get-RecentBranchRunIds {
    param(
        [Parameter(Mandatory)][string]$Branch,
        [int]$Limit = 10
    )

    try {
        $json = gh run list --branch $Branch --workflow CI --limit $Limit --json databaseId --jq '[.[].databaseId]'
    }
    catch {
        return @()
    }
    if (-not $json) {
        return @()
    }
    # Branch runs may be in any conclusion state — the SDK build job uploads
    # its artifact before downstream tests run, so failed runs often still
    # contain the artifact we need.
    return @($json | ConvertFrom-Json)
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

    $artifactName = "$Name-sdk"

    # Download to a temp directory, then move contents into destination
    $tempDir = Join-Path ([System.IO.Path]::GetTempPath()) "sentry-$Name-sdk-download"
    if (Test-Path $tempDir) {
        Remove-Item -Path $tempDir -Recurse -Force
    }

    gh run download $RunId -n $artifactName -D $tempDir

    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to download $Name SDK"
        exit 1
    }

    # Move downloaded contents into the destination
    if (-not (Test-Path $Destination)) {
        New-Item -ItemType Directory -Path $Destination -Force | Out-Null
    }
    Copy-Item -Path (Join-Path $tempDir "*") -Destination $Destination -Recurse -Force
    Remove-Item -Path $tempDir -Recurse -Force -ErrorAction SilentlyContinue

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

# Resolve the source CI run for each SDK. Primary source is main's latest
# successful run; for artifacts not yet on main (e.g. new native backends
# under development on a feature branch), fall back to the most recent run
# on the current branch that has the artifact.
$primaryRunId = Get-LatestSuccessfulRunId
$artifactsByRun = @{}
$artifactsByRun[$primaryRunId] = Get-RunArtifactNames -RunId $primaryRunId

$currentBranch = (git -C $RepoRoot rev-parse --abbrev-ref HEAD).Trim()
$branchRunIds = @()
if ($currentBranch -and $currentBranch -ne 'main' -and $currentBranch -ne 'HEAD') {
    $branchRunIds = Get-RecentBranchRunIds -Branch $currentBranch
}

$resolved = @()
$unresolved = @()
foreach ($sdk in $sdksToDownload) {
    $artifactName = "$($sdk.Name)-sdk"
    $sourceRunId = $null

    if ($artifactsByRun[$primaryRunId] -contains $artifactName) {
        $sourceRunId = $primaryRunId
    }
    else {
        foreach ($runId in $branchRunIds) {
            if (-not $artifactsByRun.ContainsKey($runId)) {
                $artifactsByRun[$runId] = Get-RunArtifactNames -RunId $runId
            }
            if ($artifactsByRun[$runId] -contains $artifactName) {
                $sourceRunId = $runId
                Write-Host "$($sdk.Name) SDK not on main run $primaryRunId; using branch '$currentBranch' run $runId" -ForegroundColor Yellow
                break
            }
        }
    }

    if ($sourceRunId) {
        $resolved += [pscustomobject]@{ Sdk = $sdk; RunId = $sourceRunId }
    }
    else {
        $unresolved += $sdk.Name
    }
}

if ($unresolved.Count -gt 0) {
    $names = $unresolved -join ', '
    Write-Error "Could not locate these SDK artifacts on main or recent CI runs of branch '$currentBranch': $names. Push the branch so CI publishes the artifact, or build locally with Build<Name>SDK."
    exit 1
}

foreach ($entry in $resolved) {
    Download-SDK -Name $entry.Sdk.Name -Destination $entry.Sdk.Destination -RunId $entry.RunId
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
