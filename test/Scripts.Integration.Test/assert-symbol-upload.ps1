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

$retries = [regex]::Matches($log, "retry number \d+ failed due to request failed with retryable status code (\d+)")
if ($retries.Count -gt 0)
{
    $codes = ($retries | ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique) -join ', '
    Write-Host "::warning::sentry-cli retried $($retries.Count) upload request(s) after HTTP $codes (signature: sentry-cli-http-retry)."
}

Write-Host "Symbol-upload assertion passed: sentry-cli uploaded debug symbols and sources ('$LogPath')." -ForegroundColor Green

# sentry-cli returns before sentry.io has processed the upload, and a large debug companion can take
# minutes. An event that arrives earlier is symbolicated without it and never reprocessed, so wait
# until the server lists each uploaded debug companion, IL2CPP mapping and source bundle.
$pending = [regex]::Matches($log, "UPLOADED (\S+) \([^;)]+;\s*(?:\w+ )?(debug companion|il2cpp|sources)\)") |
    ForEach-Object { "$($_.Groups[1].Value) $($_.Groups[2].Value)" } | Sort-Object -Unique
function Test-ProcessedFile($files, [string] $kind)
{
    foreach ($file in $files)
    {
        $processed = switch ($kind)
        {
            'debug companion' { ($file.data | Select-Object -ExpandProperty features -ErrorAction Ignore) -contains 'debug' }
            'il2cpp' { $file.symbolType -eq 'il2cpp' }
            # A portable PDB embeds its sources instead of getting a source bundle.
            'sources' { $file.symbolType -eq 'sourcebundle' -or ($file.data | Select-Object -ExpandProperty features -ErrorAction Ignore) -contains 'sources' }
        }
        if ($processed) { return $true }
    }
    return $false
}

if ($pending -and -not $env:SENTRY_AUTH_TOKEN)
{
    Write-Host "::warning::Not waiting for sentry.io to process the debug files: SENTRY_AUTH_TOKEN is not set (signature: symbols-processing-skipped)."
}
elseif ($pending)
{
    # The project CliConfiguration.cs uploads to.
    $uriBase = "https://sentry.io/api/0/projects/sentry-sdks/sentry-unity-integration-tests/files/dsyms/"
    $timeoutSeconds = 300
    $headers = @{ Authorization = "Bearer $env:SENTRY_AUTH_TOKEN" }
    # Deterministic builds reuse debug IDs, so only a file the server processed for this build counts.
    $since = [DateTime]::UtcNow.AddMinutes(-30)
    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    foreach ($debugId in ($pending | ForEach-Object { $_.Split(' ')[0] } | Sort-Object -Unique))
    {
        $kinds = $pending | Where-Object { $_.StartsWith("$debugId ") } | ForEach-Object { $_.Substring($debugId.Length + 1) }
        while ($true)
        {
            try
            {
                $files = Invoke-RestMethod -Uri "$uriBase`?debug_id=$debugId" -Headers $headers -TimeoutSec 30
            }
            catch
            {
                $status = if ($_.Exception -is [Microsoft.PowerShell.Commands.HttpResponseException]) { [int]$_.Exception.Response.StatusCode } else { 0 }
                if ($status -in 401, 403)
                {
                    Write-Host "::warning::Not waiting for sentry.io to process the debug files: the token can't list them (HTTP $status, signature: symbols-processing-skipped)."
                    return
                }
                Write-Host "Querying debug files of $debugId failed: $_"
                $files = @()
            }
            $files = @($files | Where-Object { ([DateTime]$_.dateCreated).ToUniversalTime() -ge $since })
            $missing = $kinds | Where-Object { -not (Test-ProcessedFile $files $_) }
            if (-not $missing)
            {
                break
            }
            if ($stopwatch.Elapsed.TotalSeconds -gt $timeoutSeconds)
            {
                Write-Host "::warning::sentry.io has not processed $($missing -join ', ') of $debugId after $timeoutSeconds s (signature: symbols-processing-timeout). Events may stay unsymbolicated."
                return
            }
            Start-Sleep -Seconds 10
        }
    }
    Write-Host "sentry.io processed the uploaded debug files after $([int]$stopwatch.Elapsed.TotalSeconds) s."
}
