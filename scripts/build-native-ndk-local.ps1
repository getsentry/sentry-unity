<#
.SYNOPSIS
    Builds modules/sentry-native (NDK) and publishes the artifact to the local
    Maven repo so sentry-java can resolve it via mavenLocal().

.DESCRIPTION
    Runs :sentry-native-ndk:publishToMavenLocal in modules/sentry-native/ndk,
    producing io.sentry:sentry-native-ndk:<version> at ~/.m2.

    sentry-java's settings.gradle.kts already lists mavenLocal() in
    dependencyResolutionManagement. For Gradle to pick the locally-published
    artifact over mavenCentral, the local build's version must be unique
    (i.e., not on Maven Central) — bump the version in both the NDK source
    and modules/sentry-java/gradle/libs.versions.toml before iterating.

.PARAMETER PurgeCache
    Delete sentry-native-ndk from the Gradle module cache and the related
    transform directories, then stop the Gradle daemon. Use when Gradle is
    holding a stale cached copy (e.g., when iterating on NDK source without
    bumping the version).

.PARAMETER BuildJava
    After publishing, run :sentry-android-ndk:assembleRelease in
    modules/sentry-java to consume the freshly published artifact.

.EXAMPLE
    pwsh scripts/build-native-ndk-local.ps1
    # Publish ndk to ~/.m2.

.EXAMPLE
    pwsh scripts/build-native-ndk-local.ps1 -PurgeCache -BuildJava
    # Wipe stale caches, publish, then rebuild sentry-android-ndk against
    # the local artifact.
#>

param(
    [switch] $PurgeCache,
    [switch] $BuildJava
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
$ndkDir = Join-Path $repoRoot 'modules/sentry-native/ndk'
$javaDir = Join-Path $repoRoot 'modules/sentry-java'

if (-not (Test-Path $ndkDir)) {
    throw "sentry-native NDK module not found at $ndkDir. Did you check out the submodule?"
}

if ($PurgeCache) {
    Write-Host '==> Purging Gradle caches for sentry-native-ndk'
    $gradleCaches = Join-Path $HOME '.gradle/caches'
    $moduleCache = Join-Path $gradleCaches 'modules-2/files-2.1/io.sentry/sentry-native-ndk'
    if (Test-Path $moduleCache) {
        Remove-Item -Recurse -Force $moduleCache
        Write-Host "    removed $moduleCache"
    }

    if (Test-Path $gradleCaches) {
        $transformRoots = Get-ChildItem -Path $gradleCaches -Recurse -Force -ErrorAction SilentlyContinue `
            | Where-Object { $_.FullName -like '*sentry-native-ndk*' } `
            | ForEach-Object {
                $idx = $_.FullName.IndexOf('/transformed/')
                if ($idx -lt 0) { $idx = $_.FullName.IndexOf([IO.Path]::DirectorySeparatorChar + 'transformed' + [IO.Path]::DirectorySeparatorChar) }
                if ($idx -ge 0) { $_.FullName.Substring(0, $idx) } else { $null }
            } `
            | Where-Object { $_ } `
            | Sort-Object -Unique
        foreach ($dir in $transformRoots) {
            if (Test-Path $dir) {
                Remove-Item -Recurse -Force $dir
                Write-Host "    removed $dir"
            }
        }
    }

    Write-Host '==> Stopping Gradle daemon to clear in-memory transform registry'
    Push-Location $ndkDir
    try { & ./gradlew --stop | Out-Null } finally { Pop-Location }
}

Write-Host '==> Publishing sentry-native-ndk to mavenLocal'
Push-Location $ndkDir
try {
    & ./gradlew :sentry-native-ndk:publishToMavenLocal
    if ($LASTEXITCODE -ne 0) { throw "publishToMavenLocal failed (exit $LASTEXITCODE)" }
} finally { Pop-Location }

if ($BuildJava) {
    Write-Host '==> Building :sentry-android-ndk:assembleRelease against mavenLocal'
    Push-Location $javaDir
    try {
        & ./gradlew :sentry-android-ndk:assembleRelease
        if ($LASTEXITCODE -ne 0) { throw "sentry-android-ndk assembleRelease failed (exit $LASTEXITCODE)" }
    } finally { Pop-Location }
}

Write-Host '==> Done.'
