<#
.SYNOPSIS
    Builds a player from the local Unity 6 sample through Unity CLI.

.DESCRIPTION
    Uses an open Unity Pipeline Editor when available. Otherwise, Unity CLI
    launches a batch-mode Editor to run the sample builder.
#>

param(
    [Parameter(Mandatory)]
    [ValidateSet("StandaloneWindows64", "StandaloneOSX", "StandaloneLinux64", "Android", "iOS", "WebGL")]
    [string] $Target
)

Set-StrictMode -Version latest
$ErrorActionPreference = "Stop"

$buildTimeout = 1800
# Pipeline helpers share the test harness timeout variable.
$testTimeout = $buildTimeout

. $PSScriptRoot/unity-test-cli.ps1
. $PSScriptRoot/unity-test-pipeline.ps1

if (-not (Get-Command unity -ErrorAction SilentlyContinue)) {
    throw "Unity CLI executable 'unity' was not found on PATH."
}

$repoRoot = (Resolve-Path "$PSScriptRoot/..").Path
$ProjectPath = Join-Path $repoRoot "samples/unity-of-bugs-local"
if (-not (Test-Path $ProjectPath -PathType Container)) {
    throw "Local sample project was not found at $ProjectPath."
}

$ProjectPath = (Resolve-Path $ProjectPath).Path
$buildDirectory = Join-Path $ProjectPath "Builds/$Target"
$outputPath = switch ($Target) {
    "StandaloneWindows64" { Join-Path $buildDirectory "unity-of-bugs-local.exe" }
    "StandaloneOSX" { Join-Path $buildDirectory "unity-of-bugs-local.app" }
    "StandaloneLinux64" { Join-Path $buildDirectory "unity-of-bugs-local" }
    "Android" { Join-Path $buildDirectory "unity-of-bugs-local.apk" }
    default { $buildDirectory }
}

New-Item -ItemType Directory -Force -Path $buildDirectory | Out-Null

function Wait-ForUnityPipelineBuild {
    $deadline = (Get-Date).AddSeconds($buildTimeout)

    while ((Get-Date) -lt $deadline) {
        $status = Invoke-UnityCommand "build_status"
        if ($status -is [string]) {
            $status = $status | ConvertFrom-Json
        }

        if ($status.status -eq "completed") {
            if ($status.result -ne "Succeeded") {
                $errorsProperty = $status.PSObject.Properties["errors"]
                $errors = if ($errorsProperty) { @($errorsProperty.Value) } else { @() }
                $details = if ($errors.Count) { "`n$($errors.message -join "`n")" } else { "" }
                throw "Unity Pipeline build failed with result '$($status.result)': $($status.totalErrors) error(s).$details"
            }

            return
        }

        if ($status.status -notin @("queued", "building")) {
            throw "Unity Pipeline build ended with status '$($status.status)'."
        }

        Start-Sleep -Seconds 1
    }

    throw "Unity Pipeline build did not finish within $buildTimeout seconds."
}

$executionMode = Get-TestExecutionMode
if ($executionMode -eq "Pipeline") {
    Wait-ForEditorReady
    Invoke-UnityCommand "set_autotick" @("--enable", "true") | Out-Null
    Wait-ForRecompile
    $started = Invoke-UnityCommand "build" @("--target", $Target, "--outputPath", $outputPath, "--confirm", "true") 30
    if ($started.status -ne "queued") {
        $errorsProperty = $started.PSObject.Properties["validationErrors"]
        $errors = if ($errorsProperty) { @($errorsProperty.Value) } else { @() }
        $details = if ($errors.Count) { "`n$($errors.message -join "`n")" } else { "" }
        throw "Unity Pipeline build could not start: $($started.message)$details"
    }

    Wait-ForUnityPipelineBuild
}
else {
    $arguments = @(
        "build", $ProjectPath,
        "--target", $Target,
        "--execute-method", "Editor.SampleBuilder.Build",
        "--output-path", $outputPath,
        "--args", "-quit -batchmode -nographics"
    )

    & unity --non-interactive @arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Unity CLI build for $Target failed with exit code $LASTEXITCODE."
    }
}

if (-not (Test-Path $outputPath)) {
    throw "Unity build for $Target completed without producing $outputPath."
}

Write-Host "Build artifact: $outputPath"
