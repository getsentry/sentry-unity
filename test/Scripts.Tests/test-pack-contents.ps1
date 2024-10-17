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
    # Override the generated GUID as part of the .bcsymbolmap
    $snapshotContent = $snapshotContent -replace "BCSymbolMaps\/.+?(\.)","BCSymbolMaps/*." 

    if ($args.Count -gt 0 -and $args[0] -eq "accept") {
        Write-Host "Writing snapshot to file"
        # Override the snapshot file with the current package contents
        $snapshotContent | Out-File $snapshotFile
    }
    
    # Compare the contents and only output differences
    $result = Compare-Object $snapshotContent (Get-Content $snapshotFile) | Where-Object { $_.SideIndicator -ne '==' }
    
    if ($result.Count -eq 0) {
        Write-Host  "Package contents match snapshot."
    } else {
        Write-Host  "Package contents do not match snapshot."
        $result | Format-Table -Property InputObject, SideIndicator -AutoSize
        exit 3
    }
} finally {
    $zip.Dispose()
}

$androidLibsDir = "$projectRoot/modules/sentry-java/sentry-android-ndk/build/intermediates/merged_native_libs/release/out/lib/"
if (-not(Test-Path -Path $androidLibsDir)) {
    Write-Host  "Android native libs not found in: '$androidLibsDir'"
    exit 1
}

$androidLibs = Get-ChildItem -Recurse $androidLibsDir | ForEach-Object { $_.Directory.Name + "/" + $_.Name }
$result = Compare-Object $androidLibs (Get-Content "$PSScriptRoot/android-libs.snapshot") | Where-Object { $_.SideIndicator -ne '==' }

if ($result.Count -eq 0) {
    Write-Host  "Android native libs match snapshot."
} else {
    Write-Host  "Android native libs do not match snapshot."
    $result | Format-Table -Property InputObject, SideIndicator -AutoSize
    exit 3
}
