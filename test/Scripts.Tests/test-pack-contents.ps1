# Verifies the contents of the UPM package against a snapshot file
# 'dotnet build' and 'pack.ps1' must have already been run

# To accept a new snapshot file, run 'pwsh ./test.ps1 accept'

$ErrorActionPreference = "Stop"

$projectRoot = "$PSScriptRoot/../.."
$snapshotFile = "$PSScriptRoot/package-release.zip.snapshot"
$packageFile = "$projectRoot/package-release.zip"

Push-Location $projectRoot

if (-not(Test-Path -Path $packageFile)) {
    Write-Host  "Package '$packageFile' not found.
Run 'scripts/pack.ps1' first."
    exit 1
}

if (-not(Test-Path -Path $snapshotFile)) {
    Write-Host  "Snapshot file '$snapshotFile' not found.
Can't compare package contents against baseline."
    exit 2
}

$zip = [IO.Compression.ZipFile]::OpenRead($packageFile)
try {
    $snapshotContent = $zip.Entries.FullName.Replace("\", "/")
    if ($args.Count -gt 0 -and $args[0] -eq "accept") {
        # Override the snapshot file with the current package contents
        $snapshotContent | Out-File $snapshotFile
    }
    $result = Compare-Object $snapshotContent (Get-Content $snapshotFile)
    Write-Host  $result
    if ($result.count -eq 0)
    {
        Write-Host  "Package contents match snapshot."
    }
    else
    {
        Write-Host  "Package contents do not match snapshot."
        $result | Format-Table -AutoSize
        exit 3
    }
} finally{
    $zip.Dispose()
}