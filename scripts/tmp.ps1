Write-Host "Retrieving list of available simulators" -ForegroundColor Green
$deviceListRaw = xcrun simctl list devices
Write-Host "Available simulators:" -ForegroundColor Green
$deviceListRaw | Write-Host