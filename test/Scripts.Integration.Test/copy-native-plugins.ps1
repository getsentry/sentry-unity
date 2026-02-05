param(
    [Parameter(Mandatory = $true)][string] $SourceDirectory,
    [Parameter(Mandatory = $true)][string] $TargetDirectory,
    [Parameter(Mandatory = $true)][string] $Platform
)

$ErrorActionPreference = "Stop"

. $PSScriptRoot/common.ps1

if (-not (Test-Path $SourceDirectory))
{
    throw "Source directory does not exist: $SourceDirectory"
}

Write-Log "Copying native plugins for platform '$Platform'"
Write-Detail "Source: $SourceDirectory"
Write-Detail "Target: $TargetDirectory"

if (-not (Test-Path $TargetDirectory))
{
    New-Item -ItemType Directory -Path $TargetDirectory -Force | Out-Null
}

$files = Get-ChildItem -Path $SourceDirectory -File -Recurse

foreach ($file in $files)
{
    $relativePath = $file.FullName.Substring($SourceDirectory.Length).TrimStart([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
    $targetPath = Join-Path $TargetDirectory $relativePath
    $targetDir = Split-Path $targetPath -Parent

    if (-not (Test-Path $targetDir))
    {
        New-Item -ItemType Directory -Path $targetDir -Force | Out-Null
    }

    Write-Detail "Copying: $relativePath"
    Copy-Item -Path $file.FullName -Destination $targetPath -Force

    $metaPath = "$targetPath.meta"
    $guid = [guid]::NewGuid().ToString("N").ToLower()

    $excludeGameCoreScarlett = if ($Platform -eq "GameCoreScarlett") { 0 } else { 1 }
    $excludeGameCoreXboxOne = if ($Platform -eq "GameCoreXboxOne") { 0 } else { 1 }
    $excludePS5 = if ($Platform -eq "PS5") { 0 } else { 1 }
    $excludeSwitch = if ($Platform -eq "Switch") { 0 } else { 1 }

    $metaContent = @"
fileFormatVersion: 2
guid: $guid
PluginImporter:
  externalObjects: {}
  serializedVersion: 3
  iconMap: {}
  executionOrder: {}
  defineConstraints: []
  isPreloaded: 0
  isOverridable: 0
  isExplicitlyReferenced: 0
  validateReferences: 1
  platformData:
    Android:
      enabled: 0
      settings:
        AndroidLibraryDependee: UnityLibrary
        AndroidSharedLibraryType: Executable
        CPU: ARMv7
    Any:
      enabled: 0
      settings:
        Exclude Android: 1
        Exclude Editor: 1
        Exclude GameCoreScarlett: $excludeGameCoreScarlett
        Exclude GameCoreXboxOne: $excludeGameCoreXboxOne
        Exclude Linux64: 1
        Exclude OSXUniversal: 1
        Exclude PS5: $excludePS5
        Exclude Switch: $excludeSwitch
        Exclude Win: 1
        Exclude Win64: 1
        Exclude iOS: 1
    Editor:
      enabled: 0
      settings:
        CPU: AnyCPU
        DefaultValueInitialized: true
        OS: AnyOS
    Linux64:
      enabled: 0
      settings:
        CPU: None
    OSXUniversal:
      enabled: 0
      settings:
        CPU: None
    ${Platform}:
      enabled: 1
      settings: {}
    Win:
      enabled: 0
      settings:
        CPU: None
    Win64:
      enabled: 0
      settings:
        CPU: None
    iOS:
      enabled: 0
      settings:
        AddToEmbeddedBinaries: false
        CPU: AnyCPU
        CompileFlags:
        FrameworkDependencies:
  userData:
  assetBundleName:
  assetBundleVariant:
"@

    Write-Detail "Creating meta: $relativePath.meta"
    Set-Content -Path $metaPath -Value $metaContent
}

Write-Log "Copied $($files.Count) file(s) with meta files"
