#!/usr/bin/env pwsh
# Locate a Unity installation and emit the paths the build needs.
#
# Output (stdout, one per line):
#   UnityRoot=<dir containing the Unity executable, trailing slash>
#   UnityDataPath=<dir containing the Editor data, trailing slash>
#   UnityManagedPath=<dir containing UnityEngine.dll, trailing slash>
#
# Warnings + the "candidates tried" error go to stderr. Exits non-zero on failure.

[CmdletBinding()]
param (
    [string] $UnityVersion = '',
    [string] $HubInstallDir = '',
    [string] $HubDefaultEditor = ''
)

$ErrorActionPreference = 'Stop'

function Read-HubFile([string] $path) {
    if (-not (Test-Path $path)) { return '' }
    # Unity Hub writes a single quoted string per file (not strict JSON).
    return (Get-Content -Raw $path).Trim().Trim('"')
}

# --- Resolve Hub install dir + default editor -------------------------------

# AppData is Windows-only; on macOS/Linux we just rely on the platform default below.
$hubConfigDir = if ($env:APPDATA) { Join-Path $env:APPDATA 'UnityHub' } else { '' }

if (-not $HubInstallDir -and $hubConfigDir) {
    $HubInstallDir = Read-HubFile (Join-Path $hubConfigDir 'secondaryInstallPath.json')
}
if (-not $HubInstallDir) {
    $HubInstallDir = if ($IsWindows) { 'C:/Program Files/Unity/Hub/Editor' }
                     elseif ($IsMacOS) { '/Applications/Unity/Hub/Editor' }
                     elseif ($IsLinux) { Join-Path $env:HOME 'Unity/Hub/Editor' }
}
if (-not (Test-Path $HubInstallDir)) { $HubInstallDir = '' }

if (-not $HubDefaultEditor -and $hubConfigDir) {
    $HubDefaultEditor = Read-HubFile (Join-Path $hubConfigDir 'defaultEditor.json')
    if ($HubDefaultEditor -and -not (Test-Path (Join-Path $HubInstallDir $HubDefaultEditor))) {
        $HubDefaultEditor = ''
    }
}
# Fallback: pick the highest version-named dir under the hub install root.
if (-not $HubDefaultEditor -and $HubInstallDir) {
    $HubDefaultEditor = Get-ChildItem -Path $HubInstallDir -Directory -ErrorAction SilentlyContinue |
        Where-Object Name -Match '^\d{4}' |
        Sort-Object Name -Descending |
        Select-Object -First 1 -ExpandProperty Name
}

# --- Build the probe list ---------------------------------------------------

# Layout pieces. macOS Unity ships as a .app bundle so the inner path differs.
$inner    = if ($IsMacOS) { 'Unity.app/Contents' } else { 'Editor/Data' }
$rootName = if ($IsMacOS) { 'Unity.app' }          else { 'Editor' }

# Each "install" candidate is a directory like <hub>/<version>/ that contains
# either Editor/ (win/linux) or Unity.app/ (mac).
$installs = [System.Collections.Generic.List[string]]::new()
if ($IsLinux -and $env:UNITY_PATH) { $installs.Add($env:UNITY_PATH) }
if ($HubInstallDir -and $UnityVersion) { $installs.Add((Join-Path $HubInstallDir $UnityVersion)) }
if ($HubInstallDir -and $HubDefaultEditor -and $UnityVersion -ne $HubDefaultEditor) {
    $installs.Add((Join-Path $HubInstallDir $HubDefaultEditor))
}
if ($IsWindows) { $installs.Add('C:/Program Files/Unity') }
if ($IsMacOS)   { $installs.Add('/Applications/Unity') }

# Unity 6000.3+ moved managed assemblies under Resources/Scripting/Managed.
# Probe the new layout first within each install.
$dllRelatives = @('Resources/Scripting/Managed/UnityEngine.dll', 'Managed/UnityEngine.dll')

function Find-UnityDll([string] $install) {
    foreach ($rel in $dllRelatives) {
        $dll = Join-Path $install (Join-Path $inner $rel)
        if (Test-Path $dll) { return $dll }
    }
    return $null
}

# Did the requested-version install yield a DLL? Drives the fallback warning below.
$expected = if ($UnityVersion -and $HubInstallDir) { Join-Path $HubInstallDir $UnityVersion } else { '' }
$shouldWarnFallback = $expected -and -not (Find-UnityDll $expected)

$tried = [System.Collections.Generic.List[string]]::new()

foreach ($install in $installs) {
    $dll = Find-UnityDll $install
    if (-not $dll) {
        $dllRelatives | ForEach-Object { $tried.Add((Join-Path $install (Join-Path $inner $_))) }
        continue
    }

    if ($shouldWarnFallback) {
        Write-Warning "Unity version $UnityVersion is not installed. Falling back to default Unity installation."
    }

    $unityRoot        = (Join-Path $install $rootName) + '/'
    $unityDataPath    = (Join-Path $install $inner)    + '/'
    $unityManagedPath = (Split-Path $dll -Parent)      + '/'

    # Normalize to forward slashes — MSBuild accepts both, and it keeps the output diff-clean across platforms.
    Write-Output ("UnityRoot="        + ($unityRoot        -replace '\\', '/'))
    Write-Output ("UnityDataPath="    + ($unityDataPath    -replace '\\', '/'))
    Write-Output ("UnityManagedPath=" + ($unityManagedPath -replace '\\', '/'))
    exit 0
}

$msg = @"
Unity installation not found. See CONTRIBUTING.md.
UnityVersion: '$UnityVersion'
Expected one of:
  * $($tried -join "`n  * ")
"@
Write-Error $msg
exit 1
