# Verifies that no managed plugin declaring `DllImport("__Internal")` is marked compatible with a
# desktop standalone player.
#
# `__Internal` tells IL2CPP the native symbol is linked into the player executable. Unity can do that
# on iOS and macOS, where it compiles the Objective-C sources under `Plugins/iOS` and `Plugins/macOS`
# into the player, and on the consoles. On Windows and Linux native plugins are always separate
# shared libraries loaded at runtime, so there is no way for those symbols to exist. The build only
# survives today because the UnityLinker strips the unreferenced bridge types; anything that keeps
# them alive (a `link.xml` from another package, a lower stripping level) turns the mismatch into
# unresolved externals at link time.
#
# Runs against `package-release.zip` when present, so it validates what actually ships, and falls
# back to `package-dev` for a local run before packing.

$ErrorActionPreference = "Stop"

$projectRoot = Resolve-Path "$PSScriptRoot/../.."
$packageFile = Join-Path $projectRoot "package-release.zip"

# Unity platform names that load native code exclusively as a separate shared library.
$dynamicOnlyPlatforms = @("Win", "Win64", "Linux64")

# Returns a hashtable of Unity platform name -> enabled flag. Handles both .meta dialects: the
# legacy `- first:/second:` list and the newer platform-keyed map.
function Get-EnabledPlatforms([string]$metaText) {
    $platforms = @{}
    $current = $null
    $expectKey = $false

    foreach ($line in $metaText -split "`r?`n") {
        if ($line -match '^\s*-\s*first:\s*$') {
            $expectKey = $true
            continue
        }
        if ($expectKey) {
            # `Standalone: Win64`, `iPhone: iOS`, `Editor: Editor`, `: Any` or `Any:`
            if ($line -match '^\s*(.*?):\s*(\S*)\s*$') {
                $current = if ($Matches[2]) { $Matches[2] } else { $Matches[1] }
            }
            $expectKey = $false
            continue
        }
        if ($line -match '^\s*second:\s*$') { continue }
        if ($line -match '^\s{4}(\S[^:]*):\s*$') {
            $current = $Matches[1]
            continue
        }
        if ($line -match '^\s*enabled:\s*(\d)\s*$' -and $current) {
            $platforms[$current] = [int]$Matches[1]
            $current = $null
        }
    }

    return $platforms
}

# name -> @{ Bytes; Meta } for every managed plugin in the package, from the zip or from disk.
function Get-ManagedPlugins {
    $plugins = @{}

    if (Test-Path -Path $packageFile) {
        Write-Host "Validating $packageFile"
        Add-Type -AssemblyName System.IO.Compression.FileSystem
        $zip = [IO.Compression.ZipFile]::OpenRead($packageFile)
        try {
            foreach ($entry in $zip.Entries) {
                $path = $entry.FullName.Replace("\", "/")
                if ($path -notmatch '\.dll$') { continue }

                $metaEntry = $zip.GetEntry("$($entry.FullName).meta")
                if (-not $metaEntry) { continue }

                $dllStream = New-Object IO.MemoryStream
                $entry.Open().CopyTo($dllStream)
                $metaReader = New-Object IO.StreamReader($metaEntry.Open())
                try {
                    $plugins[$path] = @{ Bytes = $dllStream.ToArray(); Meta = $metaReader.ReadToEnd() }
                } finally {
                    $metaReader.Dispose()
                    $dllStream.Dispose()
                }
            }
        } finally {
            $zip.Dispose()
        }
        return $plugins
    }

    $packageDev = Join-Path $projectRoot "package-dev"
    if (-not (Test-Path -Path $packageDev)) {
        Write-Host "Neither '$packageFile' nor '$packageDev' found. Run 'scripts/pack.ps1' first."
        exit 1
    }

    Write-Host "'$packageFile' not found - validating $packageDev instead"
    foreach ($dll in Get-ChildItem -Path $packageDev -Recurse -Filter "*.dll") {
        $meta = "$($dll.FullName).meta"
        if (-not (Test-Path -Path $meta)) { continue }
        $relative = $dll.FullName.Substring($packageDev.Length + 1).Replace("\", "/")
        $plugins[$relative] = @{
            Bytes = [IO.File]::ReadAllBytes($dll.FullName)
            Meta = [IO.File]::ReadAllText($meta)
        }
    }

    return $plugins
}

$plugins = Get-ManagedPlugins
if ($plugins.Count -eq 0) {
    Write-Host "No managed plugins with .meta files found - nothing to validate." -ForegroundColor Yellow
    exit 1
}

$failures = @()
$checked = 0

foreach ($path in $plugins.Keys | Sort-Object) {
    $ascii = [Text.Encoding]::ASCII.GetString($plugins[$path].Bytes)
    if (-not $ascii.Contains("__Internal")) { continue }

    $checked++
    $enabled = Get-EnabledPlatforms $plugins[$path].Meta

    foreach ($platform in $dynamicOnlyPlatforms) {
        if ($enabled[$platform] -eq 1) {
            $failures += "$path is enabled for '$platform'"
        }
        # The legacy `Any` block only governs when `Any` itself is enabled.
        if ($enabled["Any"] -eq 1 -and $plugins[$path].Meta -match "Exclude $($platform): 0") {
            $failures += "$path is enabled for 'Any' without excluding '$platform'"
        }
    }
}

if ($failures) {
    Write-Host "Managed plugins declaring __Internal imports must not target desktop standalone players:" -ForegroundColor Yellow
    foreach ($failure in $failures) {
        Write-Host "  $failure" -ForegroundColor Red
    }
    Write-Host "Disable Standalone Win, Win64 and Linux64 in the plugin's .meta under package-dev." -ForegroundColor Yellow
    exit 3
}

Write-Host "Checked $checked managed plugin(s) with '__Internal' imports - all correctly scoped." -ForegroundColor Green
exit 0
