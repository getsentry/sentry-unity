<#
.SYNOPSIS
    Builds a player from the local Unity 6 sample through Unity CLI.
#>

param(
    [Parameter(Mandatory)]
    [ValidateSet("StandaloneWindows64", "StandaloneOSX", "StandaloneLinux64", "Android", "iOS", "WebGL")]
    [string] $Target
)

Set-StrictMode -Version latest
$ErrorActionPreference = "Stop"

if (-not (Get-Command unity -ErrorAction SilentlyContinue)) {
    throw "Unity CLI executable 'unity' was not found on PATH."
}

$repoRoot = (Resolve-Path "$PSScriptRoot/..").Path
$projectPath = Join-Path $repoRoot "samples/unity-of-bugs-local"
if (-not (Test-Path $projectPath -PathType Container)) {
    throw "Local sample project was not found at $projectPath."
}

$buildDirectory = Join-Path $projectPath "Builds/$Target"
$outputPath = switch ($Target) {
    "StandaloneWindows64" { Join-Path $buildDirectory "unity-of-bugs-local.exe" }
    "StandaloneOSX" { Join-Path $buildDirectory "unity-of-bugs-local.app" }
    "StandaloneLinux64" { Join-Path $buildDirectory "unity-of-bugs-local" }
    "Android" { Join-Path $buildDirectory "unity-of-bugs-local.apk" }
    default { $buildDirectory }
}

New-Item -ItemType Directory -Force -Path $buildDirectory | Out-Null

$arguments = @(
    "build", $projectPath,
    "--target", $Target,
    "--execute-method", "Editor.SampleBuilder.Build",
    "--output-path", $outputPath
)

& unity --non-interactive @arguments
if ($LASTEXITCODE -ne 0) {
    throw "Unity CLI build for $Target failed with exit code $LASTEXITCODE."
}

if (-not (Test-Path $outputPath)) {
    throw "Unity CLI build for $Target completed without producing $outputPath."
}

Write-Host "Build artifact: $outputPath"
