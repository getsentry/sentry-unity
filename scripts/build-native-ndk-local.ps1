<#
.SYNOPSIS
    Builds modules/sentry-native (NDK) and publishes the artifact to the local
    Maven repo so modules/sentry-java consumes it instead of mavenCentral.

.DESCRIPTION
    Runs :sentry-native-ndk:publishToMavenLocal in modules/sentry-native/ndk,
    producing io.sentry:sentry-native-ndk:<version> at ~/.m2.

    Requires mavenLocal() to be listed before mavenCentral() in
    modules/sentry-java/settings.gradle.kts. The script verifies this and
    aborts otherwise.

    Because both repos publish the same version coordinate, Gradle's module
    and transform caches can hold a previously-resolved mavenCentral copy.
    The first time you switch to local (or when the module cache holds a
    stale build), pass -PurgeCache to wipe sentry-native-ndk caches and
    stop the Gradle daemon so the next build re-resolves from mavenLocal.

.PARAMETER PurgeCache
    Delete sentry-native-ndk from the Gradle module cache and the related
    transform directories, then stop the Gradle daemon. Use when switching
    from mavenCentral resolution or when the consumed artifact looks stale.

.PARAMETER BuildJava
    After publishing, run :sentry-android-ndk:assembleRelease in
    modules/sentry-java to consume the freshly published artifact.

.EXAMPLE
    pwsh scripts/build-native-ndk-local.ps1
    # Publish ndk to ~/.m2 (assumes caches are already clean).

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
$javaSettings = Join-Path $javaDir 'settings.gradle.kts'

if (-not (Test-Path $ndkDir)) {
    throw "sentry-native NDK module not found at $ndkDir. Did you check out the submodule?"
}
if (-not (Test-Path $javaSettings)) {
    throw "sentry-java settings.gradle.kts not found at $javaSettings."
}

$settingsContent = Get-Content $javaSettings -Raw
$drmMatch = [regex]::Match($settingsContent, 'dependencyResolutionManagement\s*\{[^}]*repositories\s*\{(?<repos>[^}]*)\}')
if (-not $drmMatch.Success) {
    throw "Could not locate dependencyResolutionManagement.repositories block in $javaSettings."
}
$reposBlock = $drmMatch.Groups['repos'].Value
$localIdx = $reposBlock.IndexOf('mavenLocal()')
$centralIdx = $reposBlock.IndexOf('mavenCentral()')
if ($localIdx -lt 0 -or $centralIdx -lt 0 -or $localIdx -gt $centralIdx) {
    throw @"
mavenLocal() must appear before mavenCentral() in
$javaSettings (dependencyResolutionManagement block) so sentry-java
resolves the locally-published sentry-native-ndk artifact. Reorder the
repositories and re-run this script.
"@
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
