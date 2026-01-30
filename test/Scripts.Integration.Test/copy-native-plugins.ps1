param(
    [Parameter(Mandatory = $true)][string] $SourceDirectory,
    [Parameter(Mandatory = $true)][string] $TargetDirectory,
    [Parameter(Mandatory = $true)][string] $Platform
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $SourceDirectory))
{
    throw "Source directory does not exist: $SourceDirectory"
}

Write-Host "Copying native plugins from '$SourceDirectory' to '$TargetDirectory' for platform '$Platform'"

# Create target directory if it doesn't exist
if (-not (Test-Path $TargetDirectory))
{
    New-Item -ItemType Directory -Path $TargetDirectory -Force | Out-Null
}

# Get all files recursively from source directory
$files = Get-ChildItem -Path $SourceDirectory -File -Recurse

foreach ($file in $files)
{
    # Calculate relative path from source directory
    $relativePath = $file.FullName.Substring($SourceDirectory.Length).TrimStart([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
    $targetPath = Join-Path $TargetDirectory $relativePath
    $targetDir = Split-Path $targetPath -Parent

    # Create target subdirectory if needed
    if (-not (Test-Path $targetDir))
    {
        New-Item -ItemType Directory -Path $targetDir -Force | Out-Null
    }

    # Copy the file
    Write-Host "  Copying: $relativePath"
    Copy-Item -Path $file.FullName -Destination $targetPath -Force

    # Generate meta file
    $metaPath = "$targetPath.meta"
    $guid = [guid]::NewGuid().ToString("N")

    $metaContent = @"
fileFormatVersion: 2
guid: $guid
PluginImporter:
  externalObjects: {}
  serializedVersion: 2
  iconMap: {}
  executionOrder: {}
  defineConstraints: []
  isPreloaded: 0
  isOverridable: 1
  isExplicitlyReferenced: 0
  validateReferences: 1
  platformData:
  - first:
      : Any
    second:
      enabled: 0
      settings:
        Exclude Android: 1
        Exclude Editor: 1
        Exclude Linux64: 1
        Exclude OSXUniversal: 1
        Exclude WebGL: 1
        Exclude Win: 1
        Exclude Win64: 1
        Exclude iOS: 1
  - first:
      Any:
    second:
      enabled: 0
      settings: {}
  - first:
      ${Platform}: ${Platform}
    second:
      enabled: 1
      settings: {}
  userData:
  assetBundleName:
  assetBundleVariant:
"@

    Write-Host "  Creating meta: $relativePath.meta"
    Set-Content -Path $metaPath -Value $metaContent -NoNewline
}

Write-Host "Successfully copied $($files.Count) file(s) with meta files for platform '$Platform'"
