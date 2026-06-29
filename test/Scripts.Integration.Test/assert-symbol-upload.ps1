# Asserts that the build actually invoked sentry-cli to upload debug symbols AND
# sources.
#
# Symbol/source upload is silent on failure: BuildPostProcess just logs a line and
# moves on, so a misconfigured (or missing) auth token produces a green build with
# unsymbolicated events. This turns that into a hard CI failure.
#
# We assert on the sentry-cli *invocation* rather than on uploaded-file counts:
# uploads are deduplicated server-side by debug-id, so a re-run of the same build
# legitimately reports "Nothing to upload" with zero uploaded files. The invocation
# carrying '--include-sources' plus a clean terminal state is the reliable signal.
#
# Pass the Unity build log (desktop), or the gradle/Xcode sentry-symbols-upload.log.

param(
    [Parameter(Mandatory = $true)][string] $LogPath
)

Set-StrictMode -Version latest
$ErrorActionPreference = "Stop"

# A missing log is itself a failure: the iOS Xcode phase only writes the log when
# upload is enabled, and the desktop/gradle paths always produce a log when they run.
if (-not (Test-Path $LogPath))
{
    throw "Symbol-upload assertion failed: log file not found at '$LogPath'. " +
        "Symbol upload likely did not run - check that SENTRY_AUTH_TOKEN is set on the build step."
}

$log = Get-Content $LogPath -Raw

# Bail-out markers, in priority order:
#   - "Automated symbols upload has been disabled" : SentryCliOptions.IsValid (desktop/Android)
#   - "Sentry symbol upload has been disabled via build setting" : iOS Xcode phase
# Both mean upload was turned off during the build - almost always a missing token.
foreach ($disabledMarker in @(
        "Automated symbols upload has been disabled",
        "symbol upload has been disabled via build setting"))
{
    if ($log -match $disabledMarker)
    {
        throw "Symbol-upload assertion failed: '$LogPath' contains '$disabledMarker'. " +
            "Symbol upload was turned off during the build - check that SENTRY_AUTH_TOKEN is set on the build step."
    }
}

# sentry-cli must have been invoked for a debug-files upload that includes sources.
# '--include-sources' is added only when SentryCliOptions.UploadSources is true, so
# its presence in the invocation proves both symbol and source upload are configured.
if ($log -notmatch "debug-files.{0,40}upload")
{
    throw "Symbol-upload assertion failed: no 'debug-files upload' sentry-cli invocation found in '$LogPath'."
}

if ($log -notmatch "--include-sources")
{
    throw "Symbol-upload assertion failed: sentry-cli was invoked without '--include-sources' in '$LogPath'. " +
        "Source upload (UploadSources) is not enabled."
}

# Confirm the upload reached a terminal success state rather than erroring out.
# "Uploaded N missing debug information files" on first upload; "Nothing to upload"
# when the artifacts are already on the server (idempotent re-run / cache hit).
$uploadSucceeded = $log -match "Uploaded \d+ missing debug information file" `
    -or $log -match "Nothing to upload"
if (-not $uploadSucceeded)
{
    throw "Symbol-upload assertion failed: sentry-cli upload did not reach a success state in '$LogPath'. " +
        "Expected 'Uploaded N missing debug information files' or 'Nothing to upload'."
}

Write-Host "Symbol-upload assertion passed: sentry-cli uploaded debug symbols and sources ('$LogPath')." -ForegroundColor Green
