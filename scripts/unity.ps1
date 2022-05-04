# This script wraps Unity, retrying automatically on licence errors happening in CI when too many jobs run in parallel.
$ErrorActionPreference = "Stop"

. $PSScriptRoot/unity-utils.ps1

Log output is written to 'unity.log'
RunUnity $args[0] ($args | Select-Object -Skip 1)