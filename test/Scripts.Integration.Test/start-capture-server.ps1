#!/usr/bin/env pwsh
#
# Starts envelope-capture-server.py in the background and waits until it serves.
# Used by the integration test workflows when SENTRY_DSN points at the capture host.

param(
    [string] $Platform = "unknown",
    [int] $Port = 8000,
    [string] $Output = ""
)

$ErrorActionPreference = "Stop"

# One directory per platform so the per-job artifacts can be merged into a single corpus
# without index.jsonl and capture-server.log colliding.
if ([string]::IsNullOrEmpty($Output)) {
    $Output = "test/IntegrationTest/envelopes/$Platform"
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
