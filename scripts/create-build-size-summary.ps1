Set-StrictMode -Version latest
$ErrorActionPreference = "Stop"

$measurements = @()
Get-ChildItem -Path "build-size-measurements" -Recurse -Filter "*.json" | ForEach-Object {
    $data = Get-Content $_.FullName -Raw | ConvertFrom-Json
    $measurements += $data
}

if ($measurements.Count -eq 0) {
    Write-Host "No build size measurements found. Skipping summary."
    exit 0
}

function Format-Size {
    param ([long]$Bytes)

    if ($Bytes -lt 1KB) { return "$Bytes B" }
    if ($Bytes -lt 1MB) { return "{0:N2} KB" -f ($Bytes / 1KB) }
    if ($Bytes -lt 1GB) { return "{0:N2} MB" -f ($Bytes / 1MB) }
    return "{0:N2} GB" -f ($Bytes / 1GB)
}

$summary = @"
## 📊 Build Size

| Platform + Version | Baseline | Sentry Impact |
|--------------------|----------|---------------|
"@

$measurements | Sort-Object Platform, UnityVersion | ForEach-Object {
    $baselineSize = Format-Size $_.WithoutSentry
    $diffSize = Format-Size ([Math]::Abs($_.Difference))

    $sign = if ($_.Difference -gt 0) { "+" } elseif ($_.Difference -lt 0) { "-" } else { "" }

    $summary += "`n| $($_.Platform) $($_.UnityVersion) | $baselineSize | $sign$diffSize |"
}

$summary | Out-File -FilePath $env:GITHUB_STEP_SUMMARY
Write-Host "Build size summary created successfully"
