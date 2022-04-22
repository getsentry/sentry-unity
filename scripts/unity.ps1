# This script wraps Unity, retrying automatically on licence errors happening in CI when too many jobs run in parallel.
$ErrorActionPreference = "Stop"

. $PSScriptRoot/unity-utils.ps1

# Redirecting the output to $null because RunUnity prints logs using Write-Host AND Write-Output - we don't need both here.
RunUnity $args[0] ($args | Select-Object -Skip 1) > $null