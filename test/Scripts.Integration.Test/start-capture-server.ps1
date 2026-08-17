#!/usr/bin/env pwsh
#
# Starts envelope-capture-server.py in the background and waits until it serves.
# Used by the integration test workflows when SENTRY_DSN points at the capture host.

param(
    [string] $Platform = "unknown",
    [int] $Port = 8787,
    [string] $Output = ""
)

$ErrorActionPreference = "Stop"

# One directory per platform so the per-job artifacts can be merged into a single corpus
# without index.jsonl and capture-server.log colliding.
if ([string]::IsNullOrEmpty($Output)) {
    $Output = "test/IntegrationTest/envelopes/$Platform"
}

# Build jobs call this from every build step, because a detached server does not reliably survive
# the gap between steps. Reuse the running one instead of fighting over the port and the log file.
try {
    Invoke-WebRequest -Uri "http://127.0.0.1:$Port/HEALTH" -TimeoutSec 2 -UseBasicParsing | Out-Null
    Write-Host "Envelope capture server already running on port $Port"
    exit 0
}
catch {
    # Nothing answered. A server from a previous step may still be holding the port without
    # serving (the runner suspends leftovers between steps), which would make the new one fail
    # with "Address already in use" - so clear the port before starting.
    if ($IsWindows) {
        Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue |
            ForEach-Object { Stop-Process -Id $_.OwningProcess -Force -ErrorAction SilentlyContinue }
    }
    else {
        $stale = & lsof -ti "tcp:$Port" 2>$null
        foreach ($processId in $stale) {
            Write-Host "Killing stale listener on port $Port (pid $processId)"
            & kill -9 $processId 2>$null
        }
    }
}

$python = if (Get-Command python3 -ErrorAction SilentlyContinue) { "python3" } else { "python" }
$server = Join-Path $PSScriptRoot "envelope-capture-server.py"

New-Item -ItemType Directory -Force -Path $Output | Out-Null
$logPath = Join-Path $Output "capture-server.log"

Start-Process -FilePath $python `
    -ArgumentList @($server, "--output", $Output, "--port", $Port, "--platform", $Platform) `
    -RedirectStandardError $logPath -NoNewWindow

for ($i = 1; $i -le 30; $i++) {
    try {
        Invoke-WebRequest -Uri "http://127.0.0.1:$Port/HEALTH" -TimeoutSec 2 -UseBasicParsing | Out-Null
        Write-Host "Envelope capture server is up on port $Port (writing to $Output)"
        exit 0
    }
    catch {
        Start-Sleep -Seconds 1
    }
}

Get-Content $logPath -ErrorAction SilentlyContinue | Write-Host
throw "Envelope capture server did not come up on port $Port"
