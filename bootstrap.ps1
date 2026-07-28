Set-StrictMode -Version latest
$ErrorActionPreference = "Stop"
$PSNativeCommandUseErrorActionPreference = $true

$repoRoot = $PSScriptRoot
$results = @()
$hardFailure = $false

function Add-Result {
    param(
        [Parameter(Mandatory)][ValidateSet("PASS", "WARN", "SKIP", "FAIL")][string] $Status,
        [Parameter(Mandatory)][string] $Name,
        [Parameter(Mandatory)][string] $Detail,
        [string] $Fix = ""
    )

    $script:results += [PSCustomObject]@{
        Status = $Status
        Name = $Name
        Detail = $Detail
        Fix = $Fix
    }

    if ($Status -eq "FAIL") {
        $script:hardFailure = $true
    }
}

function Invoke-Stage {
    param(
        [Parameter(Mandatory)][string] $Name,
        [Parameter(Mandatory)][scriptblock] $Action,
        [ValidateSet("WARN", "FAIL")][string] $FailureStatus = "FAIL",
        [string] $Fix = ""
    )

    try {
        $global:LASTEXITCODE = 0
        & $Action
        if ($LASTEXITCODE -ne 0) {
            throw "Command exited with code $LASTEXITCODE."
        }
        Add-Result -Status "PASS" -Name $Name -Detail "Completed."
    }
    catch {
        Add-Result -Status $FailureStatus -Name $Name -Detail $_.Exception.Message -Fix $Fix
    }
}

function Test-Command {
    param([Parameter(Mandatory)][string] $Name)

    return [bool](Get-Command $Name -ErrorAction SilentlyContinue)
}

Write-Host "Bootstrapping Sentry Unity from '$repoRoot'"

if (-not (Test-Path (Join-Path $repoRoot ".git"))) {
    Add-Result -Status "FAIL" -Name "Repository" -Detail "Run this script from a repository checkout." -Fix "Run: pwsh bootstrap.ps1 from the repository root."
}

$hasGit = Test-Command "git"
if ($hasGit) {
    Add-Result -Status "PASS" -Name "Git" -Detail "Found git."
    Invoke-Stage -Name "Submodules" -Action { git -C $repoRoot submodule update --init --recursive } -Fix "Run: git submodule update --init --recursive"
}
else {
    Add-Result -Status "FAIL" -Name "Git" -Detail "git is not available on PATH." -Fix "Install Git, then rerun: pwsh bootstrap.ps1"
}

$hasDotnet = Test-Command "dotnet"
if ($hasDotnet) {
    Add-Result -Status "PASS" -Name "dotnet" -Detail "Found dotnet."
    Invoke-Stage -Name "Workloads" -Action { dotnet workload restore } -FailureStatus "WARN" -Fix "Run: dotnet workload restore. On macOS or Linux, elevate it if dotnet reports inadequate permissions."
}
else {
    Add-Result -Status "FAIL" -Name "dotnet" -Detail "dotnet is not available on PATH." -Fix "Install the SDK pinned in global.json, then rerun: pwsh bootstrap.ps1"
}

if ($hasDotnet -and (Test-Command "gh")) {
    Invoke-Stage -Name "Prebuilt native SDKs" -Action { dotnet msbuild /t:DownloadNativeSDKs src/Sentry.Unity } -FailureStatus "WARN" -Fix "Authenticate with 'gh auth login' and rerun, or build a platform SDK with: dotnet msbuild /t:Build<Platform>SDK src/Sentry.Unity"
}
elseif (-not (Test-Command "gh")) {
    Add-Result -Status "WARN" -Name "Prebuilt native SDKs" -Detail "GitHub CLI is not available." -Fix "Install gh and run 'gh auth login', then rerun: pwsh bootstrap.ps1"
}
else {
    Add-Result -Status "WARN" -Name "Prebuilt native SDKs" -Detail "dotnet is unavailable." -Fix "Install the SDK pinned in global.json, then rerun: pwsh bootstrap.ps1"
}

if (Test-Path (Join-Path $repoRoot "modules/sentry-cli.properties")) {
    Invoke-Stage -Name "Sentry CLI" -Action { & (Join-Path $repoRoot "scripts/download-sentry-cli.ps1") } -FailureStatus "WARN" -Fix "Check network access, then rerun: pwsh bootstrap.ps1"
}
else {
    Add-Result -Status "WARN" -Name "Sentry CLI" -Detail "sentry-cli submodule files are unavailable." -Fix "Run: git submodule update --init --recursive, then rerun: pwsh bootstrap.ps1"
}

if ($hasDotnet) {
    Invoke-Stage -Name "Managed SDK build" -Action { dotnet build } -Fix "Fix the reported build error, then rerun: dotnet build"
}
else {
    Add-Result -Status "FAIL" -Name "Managed SDK build" -Detail "dotnet is unavailable." -Fix "Install the SDK pinned in global.json, then run: dotnet build"
}

$appleSettings = @(
    (Join-Path $repoRoot "samples/unity-of-bugs/ProjectSettings/ProjectSettings.asset"),
    (Join-Path $repoRoot "samples/unity-of-bugs-local/ProjectSettings/ProjectSettings.asset")
)
$missingAppleSettings = @($appleSettings | Where-Object { -not (Test-Path $_) })
if (-not $Env:APPLE_ID) {
    Add-Result -Status "SKIP" -Name "Apple Developer Team ID" -Detail "APPLE_ID is not set." -Fix "Set APPLE_ID, then rerun: pwsh bootstrap.ps1"
}
elseif ($missingAppleSettings.Count -gt 0) {
    Add-Result -Status "WARN" -Name "Apple Developer Team ID" -Detail "Sample project settings are unavailable: $($missingAppleSettings -join ', ')." -Fix "Restore the sample projects, then rerun: pwsh bootstrap.ps1"
}
else {
    Invoke-Stage -Name "Apple Developer Team ID" -Action { & (Join-Path $repoRoot "scripts/samples-setup-apple-id.ps1") } -FailureStatus "WARN" -Fix "Confirm APPLE_ID is a valid Apple Developer Team ID, then rerun: pwsh bootstrap.ps1"
}

$sentryAssemblyMeta = Join-Path $repoRoot "package-dev/Runtime/Sentry.Unity.dll.meta"
if (-not $Env:SENTRY_AUTH_TOKEN) {
    Add-Result -Status "SKIP" -Name "Sentry CLI auth" -Detail "SENTRY_AUTH_TOKEN is not set." -Fix "Set SENTRY_AUTH_TOKEN, then rerun: pwsh bootstrap.ps1"
}
elseif (-not (Test-Path $sentryAssemblyMeta)) {
    Add-Result -Status "WARN" -Name "Sentry CLI auth" -Detail "Managed SDK build output is unavailable." -Fix "Run: dotnet build, then rerun: pwsh bootstrap.ps1"
}
else {
    Invoke-Stage -Name "Sentry CLI auth" -Action { & (Join-Path $repoRoot "scripts/samples-setup-cli-options.ps1") } -FailureStatus "WARN" -Fix "Confirm SENTRY_AUTH_TOKEN is valid, then rerun: pwsh bootstrap.ps1"
}

$localAssets = Join-Path $repoRoot "samples/unity-of-bugs-local/Assets"
if ((Test-Path $localAssets) -and (Get-Item $localAssets).LinkType -eq "SymbolicLink") {
    Add-Result -Status "PASS" -Name "Local sample assets" -Detail "Assets symlink is available."
}
else {
    Add-Result -Status "WARN" -Name "Local sample assets" -Detail "Assets symlink is unavailable." -Fix "Enable Git symlink support and restore samples/unity-of-bugs-local/Assets."
}

Write-Host ""
Write-Host "Bootstrap summary"
foreach ($result in $results) {
    Write-Host "[$($result.Status)] $($result.Name): $($result.Detail)"
    if ($result.Fix) {
        Write-Host "       $($result.Fix)"
    }
}

if ($hardFailure) {
    exit 1
}
