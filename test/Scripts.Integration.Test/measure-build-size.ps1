param(
    [Parameter(Mandatory=$true)]
    [string] $Path1,

    [Parameter(Mandatory=$true)]
    [string] $Path2,

    [string] $Platform = "Build",

    [string] $UnityVersion = "unknown"
)

Set-StrictMode -Version latest
$ErrorActionPreference = "Stop"

function Get-Size {
    param ([string]$Path)

    if (-not (Test-Path $Path)) {
        Write-Error "Path not found: $Path"
        exit 1
    }

    $item = Get-Item $Path
    if ($item.PSIsContainer) {
        # Directory - sum all files
        $size = (Get-ChildItem -Path $Path -Recurse -File | Measure-Object -Property Length -Sum).Sum
    } else {
        # Single file
        $size = $item.Length
    }

    return $size
}

function Format-Size {
    param ([long]$Bytes)

    if ($Bytes -lt 1KB) { return "$Bytes B" }
    if ($Bytes -lt 1MB) { return "{0:N2} KB" -f ($Bytes / 1KB) }
    if ($Bytes -lt 1GB) { return "{0:N2} MB" -f ($Bytes / 1MB) }
    return "{0:N2} GB" -f ($Bytes / 1GB)
}

$size1 = Get-Size $Path1
$size2 = Get-Size $Path2
$diff = $size2 - $size1
$percentChange = if ($size1 -gt 0) { ($diff / $size1) * 100 } else { 0 }

$diffFormatted = "$(if ($diff -gt 0) { '+' })$(Format-Size ([Math]::Abs($diff)))"
$percentFormatted = "$(if ($diff -gt 0) { '+' })$([Math]::Round($percentChange, 2))%"

Write-Host "Without Sentry: $(Format-Size $size1)"
Write-Host "With Sentry:    $(Format-Size $size2)"
Write-Host "Difference:     $diffFormatted ($percentFormatted)"

# Save measurement to artifact for consolidated summary
$measurement = @{
    Platform = $Platform
    UnityVersion = $UnityVersion
    WithoutSentry = $size1
    WithSentry = $size2
    Difference = $diff
    PercentChange = $percentChange
} | ConvertTo-Json

New-Item -Path "build-size-measurements" -ItemType Directory -Force | Out-Null
$fileName = "$Platform-$UnityVersion.json" -replace '\.', '_'
$measurement | Out-File -FilePath "build-size-measurements/$fileName"
