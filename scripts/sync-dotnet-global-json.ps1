# Post-update script for the sentry-dotnet updater: keeps the root global.json in sync with the submodule's.
$ErrorActionPreference = 'Stop'

$repoRoot = "$PSScriptRoot/.."
Copy-Item "$repoRoot/src/sentry-dotnet/global.json" "$repoRoot/global.json" -Force
