#!/usr/bin/env pwsh
#
# Opt-in capture of the raw envelopes and debug files the integration tests produce, so they can be
# replayed against a local Sentry. See docs/envelope-capture.md.
#
# Everything keys off one environment variable: when SENTRY_CAPTURE_PATH points at a directory, the
# integration scripts route the SDK (envelopes, at run time) and sentry-cli (debug files, at build
# time) to a local capture server writing into it. Unset, every function here is a no-op and the
# tests behave exactly as they always have.
#
# Dot-source this file to use it:
#   . $PSScriptRoot/capture-corpus.ps1

$Global:CapturePort = 8787
$Global:CaptureUrl = "http://127.0.0.1:$Global:CapturePort"
# The key is irrelevant - the capture server accepts anything - but the DSN has to parse.
$Global:CaptureDsn = "http://capture@127.0.0.1:$Global:CapturePort/1"

function Test-CaptureEnabled
{
    return -not [string]::IsNullOrEmpty($env:SENTRY_CAPTURE_PATH)
}

# Starts the capture server unless one is already serving. Call this from whatever step actually
# needs it: a server started in an earlier CI step does not reliably survive the gap - the runner
# leaves it suspended, holding the port without answering, which surfaces as an empty reply.
function Start-CaptureServer
{
    if (-not (Test-CaptureEnabled))
    {
        return
    }

    if (Test-CaptureServerHealthy)
    {
        Write-Host "Capture server already running on port $Global:CapturePort"
        return
    }

    Clear-CapturePort

    $python = if (Get-Command python3 -ErrorAction SilentlyContinue) { "python3" } else { "python" }
    $server = Join-Path $PSScriptRoot "envelope-capture-server.py"
    $output = $env:SENTRY_CAPTURE_PATH

    New-Item -ItemType Directory -Force -Path $output | Out-Null
    Start-Process -FilePath $python `
        -ArgumentList @($server, "--output", $output, "--port", $Global:CapturePort,
                        "--platform", (Split-Path $output -Leaf)) `
        -RedirectStandardError (Join-Path $output "capture-server.log") -NoNewWindow

    for ($i = 1; $i -le 30; $i++)
    {
        if (Test-CaptureServerHealthy)
        {
            Write-Host "Capture server is up on port $Global:CapturePort (writing to $output)"
            return
        }
        Start-Sleep -Seconds 1
    }

    Get-Content (Join-Path $output "capture-server.log") -ErrorAction SilentlyContinue | Write-Host
    throw "Capture server did not come up on port $Global:CapturePort"
}

# Tags the files captured next with the test action they belong to, so the corpus is browsable.
function Set-CaptureLabel
{
    param([Parameter(Mandatory = $true)][string] $Label)

    if (-not (Test-CaptureEnabled))
    {
        return
    }

    try
    {
        Invoke-WebRequest -Uri "$Global:CaptureUrl/MARK?label=$Label" -TimeoutSec 5 -UseBasicParsing | Out-Null
    }
    catch
    {
        Write-Host "Failed to mark capture label '$Label': $_"
    }
}

function Stop-CaptureServer
{
    if (-not (Test-CaptureEnabled))
    {
        return
    }

    try
    {
        Invoke-WebRequest -Uri "$Global:CaptureUrl/STOP" -TimeoutSec 5 -UseBasicParsing | Out-Null
    }
    catch
    {
        Write-Host "Capture server already gone"
    }
}

function Test-CaptureServerHealthy
{
    try
    {
        Invoke-WebRequest -Uri "$Global:CaptureUrl/HEALTH" -TimeoutSec 2 -UseBasicParsing | Out-Null
        return $true
    }
    catch
    {
        return $false
    }
}

# A suspended server from an earlier step keeps the port bound, which would make the new one fail
# with "Address already in use".
function Clear-CapturePort
{
    if ($IsWindows)
    {
        Get-NetTCPConnection -LocalPort $Global:CapturePort -State Listen -ErrorAction SilentlyContinue |
            ForEach-Object { Stop-Process -Id $_.OwningProcess -Force -ErrorAction SilentlyContinue }
    }
    else
    {
        foreach ($processId in (& lsof -ti "tcp:$Global:CapturePort" 2>$null))
        {
            Write-Host "Killing stale listener on port $Global:CapturePort (pid $processId)"
            & kill -9 $processId 2>$null
        }
    }
}
