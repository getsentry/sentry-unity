# Pins which platforms every plugin and assembly definition in the package targets.
#
# Two kinds of check:
#
# 1. A hardcoded scope for every plugin importer and every .asmdef. Any drift fails, including a
#    platform silently gained or lost. Changing a scope means editing the tables below, which makes
#    the decision deliberate and reviewable. This is the guard against a repeat of the platform list
#    inversion, where an explicit allowlist became an exclude list and quietly widened the set of
#    platforms an assembly ships to.
#
# 2. A rule that holds regardless of the tables: no managed plugin declaring `DllImport("__Internal")`
#    may target a desktop standalone player. `__Internal` binds at link time against a symbol inside
#    the player executable. Unity can only provide that where it compiles native sources into the
#    player, which on desktop it never does, because a native plugin there is always a separate
#    shared library loaded at runtime. Such a build only survives while the UnityLinker strips the
#    unreferenced types, so anything that preserves them turns the mismatch into unresolved externals.
#
# Runs against `package-release.zip` when it exists, which is what CI validates and what ships.
# Without it, validates the `package-dev` and `package` trees instead. Plugin .meta files are tracked
# while the binaries they describe are generated, so scope checks are strict in both modes, while the
# `__Internal` rule only covers the assemblies whose binary is actually present.
#
# Prefer the packed artifact when judging imports. The binaries in `package-dev` are whatever was
# last built there, which need not match the checked-out sources, so their import tables can describe
# a different branch entirely.

$ErrorActionPreference = "Stop"

$projectRoot = (Resolve-Path "$PSScriptRoot/../..").Path
$packageFile = Join-Path $projectRoot "package-release.zip"

# ---------------------------------------------------------------------------------------------------
# Pinned expectations. Paths are relative to the package root. Platform names are Unity's own, sorted.
# ---------------------------------------------------------------------------------------------------

# What the released package must contain. "Any" means the plugin is platform agnostic.
$ExpectedPluginScopes = @{
    "Editor/Sentry.Unity.Editor.dll"              = "Editor"
    "Editor/iOS/Sentry.Unity.Editor.iOS.dll"      = "Editor"
    # vsnprintf_sentry, imported as __Internal only under SENTRY_NATIVE_PLAYSTATION. The Switch gets
    # the same symbol from its own stubs or from sentry-switch, and Xbox goes through msvcrt.
    "Plugins/PS5/sentry_utils.c"                  = "PS5"
    # The shipped default is the stub enabled, so the linker is satisfied even without the native
    # libraries. SwitchNativePluginBuildPreProcess flips this importer at build time in the consumer's
    # project, disabling the stub once Assets/Plugins/Sentry/<target> holds the real libsentry.a, so a
    # local Switch build in this repo can legitimately leave this meta changed.
    "Plugins/Switch/sentry_native_stubs.c"        = "Switch, Switch2"
    "Plugins/iOS/SentryCxaThrowHook.cpp"          = "iOS"
    # The two bridge sources deliberately target nothing, so Unity never copies them into the
    # generated Xcode project. BuildPostProcess copies whichever one applies to
    # Libraries/<package>/SentryNativeBridge.m and AddSentryNativeBridge adds that path to the
    # target, so enabling a platform here would collide with the SDK's own copy. The same intent is
    # recorded in .gitignore, which un-ignores these metas "to control target platforms".
    "Plugins/iOS/SentryNativeBridge.m"            = ""
    "Plugins/iOS/SentryNativeBridgeNoOp.m"        = ""
    "Plugins/macOS/SentryNativeBridge.m"          = "OSXUniversal"
    "Runtime/Sentry.dll"                          = "Any"
    "Runtime/Sentry.Unity.Android.dll"            = "Android"
    "Runtime/Sentry.Unity.MacOS.dll"              = "OSXUniversal"
    "Runtime/Sentry.Unity.Native.PlayStation.dll" = "PS5"
    "Runtime/Sentry.Unity.Native.Switch.dll"      = "Switch, Switch2"
    "Runtime/Sentry.Unity.Native.Xbox.dll"        = "GameCoreScarlett, GameCoreXboxOne"
    # Android binds to "sentry" from the .aar, desktop to "sentry-native", so they are separate
    # builds of the same sources. See SentryNativeLibrary.Name.
    "Runtime/Sentry.Unity.Native.Android.dll"      = "Android"
    "Runtime/Sentry.Unity.Native.dll"             = "Linux64, OSXUniversal, Win, Win64"
    "Runtime/Sentry.Unity.dll"                    = "Any"
    # Holds the Cocoa bridge __Internal declarations, shared by the iOS and macOS integrations.
    "Runtime/Sentry.Unity.iOS.dll"                = "iOS, OSXUniversal"
}

# The samples the release carries under `Samples~`. Demo native sources, not SDK plugins, but they
# ship inside the package, so their scopes are pinned alongside everything else.
$ExpectedSampleScopes = @{
    "Samples~/unity-of-bugs/Scripts/NativeSupport/CPlugin.c"              = "Android, Any, iOS, Linux64, Lumin, OSXUniversal, tvOS, WebGL, Win, Win64"
    "Samples~/unity-of-bugs/Scripts/NativeSupport/CppPlugin.cpp"          = "Android, Any, iOS, Linux64, Lumin, OSXUniversal, tvOS, WebGL, Win, Win64"
    "Samples~/unity-of-bugs/Scripts/NativeSupport/JavaScriptPlugin.jslib" = "WebGL"
    "Samples~/unity-of-bugs/Scripts/NativeSupport/KotlinPlugin.kt"        = "Android"
    "Samples~/unity-of-bugs/Scripts/NativeSupport/ObjectiveCPlugin.m"     = "iOS, tvOS"
}

# The third party assemblies scripts/alias-assemblies.ps1 renames into the `Sentry.` namespace. They
# are managed and platform agnostic, so they carry Unity's folder default: editor only under Editor,
# every platform under Runtime. Matched by pattern rather than by name, because the set turns over
# with every sentry-dotnet dependency bump while the scope never does. The patterns cover only the
# aliased prefixes, so a new first party assembly still has to be pinned by name above.
$AliasedDependencyScopes = @(
    @{ Pattern = '^Editor/Sentry\.(Microsoft|Mono)\..*\.dll$'  ; Scope = "Editor" }
    @{ Pattern = '^Runtime/Sentry\.(Microsoft|System)\..*\.dll$'; Scope = "Any" }
)

# How package-dev deviates. The dev package keeps the test assemblies, which scripts/pack.ps1 excludes
# from the release, and the iOS bridge stays editor-loadable for the editor-only
# Sentry.Unity.iOS.Tests assembly that references it.
$DevOnlyPluginScopes = @{
    "Runtime/Sentry.Unity.iOS.dll"                   = "Editor, iOS, OSXUniversal"
    "Tests/Editor/Sentry.Unity.Editor.Tests.dll"     = "Editor"
    "Tests/Editor/Sentry.Unity.Editor.iOS.Tests.dll" = "Editor"
    "Tests/Runtime/Sentry.Unity.Android.Tests.dll"   = "Editor"
    "Tests/Runtime/Sentry.Unity.Tests.dll"           = "Editor"
    "Tests/Runtime/Sentry.Unity.iOS.Tests.dll"       = "Editor"
}

# Files the package directory is allowed to override in the release, each validated against the
# release table above.
$ExpectedReleaseOverrides = @(
    "Runtime/Sentry.Unity.iOS.dll"
)

# Assembly definitions. An empty include list plus an exclude list means "every platform except
# these", so the exclude list is the thing that must not drift unnoticed.
$ExpectedAsmdefs = @{
    "Runtime/io.sentry.unity.runtime.asmdef" = @{
        include = ""
        exclude = "CloudRendering, EmbeddedLinux, PS4, tvOS, XboxOne"
    }
    "Editor/io.sentry.unity.editor.asmdef"   = @{
        include = "Editor"
        exclude = ""
    }
}

$DevOnlyAsmdefs = @{
    "Runtime/io.sentry.unity.dev.runtime.asmdef" = @{
        include = ""
        exclude = "CloudRendering, EmbeddedLinux, PS4, tvOS, XboxOne"
    }
    "Editor/io.sentry.unity.dev.editor.asmdef"   = @{
        include = "Editor"
        exclude = ""
    }
}

# Platforms where Unity loads native code exclusively as a separate shared library.
$DynamicOnlyPlatforms = @("Win", "Win64", "Linux64")

# ---------------------------------------------------------------------------------------------------
# Parsing
# ---------------------------------------------------------------------------------------------------

# Sorted, comma separated list of the platforms a plugin importer enables. Handles both .meta
# dialects: the legacy "- first:/second:" list and the newer platform-keyed map.
function Get-EnabledPlatforms([string]$metaText) {
    $platforms = @()
    $current = $null
    $expectKey = $false

    foreach ($line in ($metaText -split "`r?`n")) {
        if ($line -match '^\s*-\s*first:\s*$') {
            $expectKey = $true
            continue
        }
        if ($expectKey) {
            # "Standalone: Win64", "iPhone: iOS", "Editor: Editor", ": Any" or "Any:"
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
            if ($Matches[1] -eq '1') { $platforms += $current }
            $current = $null
        }
    }

    return (($platforms | Sort-Object -Unique) -join ", ")
}

# The pinned scope for an aliased third party assembly, or $null when the path is not one.
function Get-AliasedDependencyScope([string]$path) {
    foreach ($rule in $script:AliasedDependencyScopes) {
        if ($path -match $rule.Pattern) { return $rule.Scope }
    }
    return $null
}

function Get-AsmdefPlatforms([string]$asmdefText) {
    $json = $asmdefText | ConvertFrom-Json
    return @{
        include = ((@($json.includePlatforms) | Where-Object { $_ } | Sort-Object) -join ", ")
        exclude = ((@($json.excludePlatforms) | Where-Object { $_ } | Sort-Object) -join ", ")
    }
}

# path -> @{ Text; Bytes } for every .meta, .asmdef and .dll, from the zip or from a directory.
function Get-PackageFiles($source, [bool]$fromZip) {
    $files = @{}

    if ($fromZip) {
        Add-Type -AssemblyName System.IO.Compression.FileSystem
        $zip = [IO.Compression.ZipFile]::OpenRead($source)
        try {
            foreach ($entry in $zip.Entries) {
                if ($entry.FullName -notmatch '\.(meta|asmdef|dll)$') { continue }
                $stream = New-Object IO.MemoryStream
                $entry.Open().CopyTo($stream)
                $bytes = $stream.ToArray()
                $stream.Dispose()
                $files[$entry.FullName.Replace("\", "/")] = @{
                    Text  = [Text.Encoding]::UTF8.GetString($bytes)
                    Bytes = $bytes
                }
            }
        }
        finally {
            $zip.Dispose()
        }
        return $files
    }

    foreach ($file in Get-ChildItem -Path $source -Recurse -File) {
        if ($file.Extension -notin ".meta", ".asmdef", ".dll") { continue }
        $bytes = [IO.File]::ReadAllBytes($file.FullName)
        $files[$file.FullName.Substring($source.Length + 1).Replace("\", "/")] = @{
            Text  = [Text.Encoding]::UTF8.GetString($bytes)
            Bytes = $bytes
        }
    }

    return $files
}

# ---------------------------------------------------------------------------------------------------
# Checks
# ---------------------------------------------------------------------------------------------------

$failures = [Collections.ArrayList]::new()
$scopesChecked = 0
$importsChecked = 0

function Test-Tree($label, $files, $expectedScopes, $expectedAsmdefs) {
    $seen = @{}

    foreach ($path in ($files.Keys | Sort-Object)) {
        $entry = $files[$path]

        if ($path -like "*.meta") {
            if ($entry.Text -notmatch "PluginImporter") { continue }
            $described = $path -replace '\.meta$', ''
            $actual = Get-EnabledPlatforms $entry.Text
            $seen[$described] = $true

            $expected = if ($expectedScopes.ContainsKey($described)) { $expectedScopes[$described] } else { Get-AliasedDependencyScope $described }
            if ($null -eq $expected) {
                [void]$script:failures.Add("$label : '$described' is not in the expected table, it enables '$actual'")
                continue
            }

            $script:scopesChecked++
            if ($actual -ne $expected) {
                [void]$script:failures.Add("$label : '$described' enables '$actual', expected '$expected'")
            }

            # The rule that holds no matter what the table says. Needs the binary, which is present in
            # the packed artifact and in a built working tree.
            $binary = $files[$described]
            if ($binary -and $binary.Bytes.Length -gt 0 -and
                [Text.Encoding]::ASCII.GetString($binary.Bytes).Contains("__Internal")) {
                $script:importsChecked++
                $enabled = $actual -split ",\s*"
                foreach ($platform in $script:DynamicOnlyPlatforms) {
                    if ($enabled -contains $platform) {
                        [void]$script:failures.Add("$label : '$described' declares __Internal imports and targets '$platform', which cannot link them")
                    }
                }
            }
            continue
        }

        if ($path -like "*.asmdef") {
            if (-not $expectedAsmdefs.ContainsKey($path)) {
                [void]$script:failures.Add("$label : assembly definition '$path' is not in the expected table")
                continue
            }
            $actual = Get-AsmdefPlatforms $entry.Text
            $want = $expectedAsmdefs[$path]
            $script:scopesChecked++
            if ($actual.include -ne $want.include) {
                [void]$script:failures.Add("$label : '$path' includePlatforms is '$($actual.include)', expected '$($want.include)'")
            }
            if ($actual.exclude -ne $want.exclude) {
                [void]$script:failures.Add("$label : '$path' excludePlatforms is '$($actual.exclude)', expected '$($want.exclude)'")
            }
        }
    }

    foreach ($expected in ($expectedScopes.Keys | Sort-Object)) {
        if (-not $seen.ContainsKey($expected)) {
            [void]$script:failures.Add("$label : expected plugin '$expected' is missing")
        }
    }

    foreach ($expected in ($expectedAsmdefs.Keys | Sort-Object)) {
        if (-not $files.ContainsKey($expected)) {
            [void]$script:failures.Add("$label : expected assembly definition '$expected' is missing")
        }
    }
}

if (Test-Path -Path $packageFile) {
    Write-Host "Validating $packageFile"
    $files = Get-PackageFiles $packageFile $true
    if ($files.Count -eq 0) {
        Write-Host "No .meta, .asmdef or .dll entries found in the package." -ForegroundColor Yellow
        exit 1
    }
    $releaseScopes = @{}
    foreach ($pair in $ExpectedPluginScopes.GetEnumerator()) { $releaseScopes[$pair.Key] = $pair.Value }
    foreach ($pair in $ExpectedSampleScopes.GetEnumerator()) { $releaseScopes[$pair.Key] = $pair.Value }
    Test-Tree "release" $files $releaseScopes $ExpectedAsmdefs
}
else {
    Write-Host "'$packageFile' not found - validating the package-dev and package trees instead"

    $devRoot = Join-Path $projectRoot "package-dev"
    $overrideRoot = Join-Path $projectRoot "package"
    foreach ($root in @($devRoot, $overrideRoot)) {
        if (-not (Test-Path -Path $root)) {
            Write-Host "'$root' not found." -ForegroundColor Yellow
            exit 1
        }
    }

    # package-dev carries the test assemblies and the editor-loadable iOS bridge.
    $devScopes = @{}
    foreach ($pair in $ExpectedPluginScopes.GetEnumerator()) { $devScopes[$pair.Key] = $pair.Value }
    foreach ($pair in $DevOnlyPluginScopes.GetEnumerator()) { $devScopes[$pair.Key] = $pair.Value }
    Test-Tree "package-dev" (Get-PackageFiles $devRoot $false) $devScopes $DevOnlyAsmdefs

    # The package directory overrides the release, so whatever it holds must match the release table.
    $overrideScopes = @{}
    foreach ($path in $ExpectedReleaseOverrides) { $overrideScopes[$path] = $ExpectedPluginScopes[$path] }
    Test-Tree "package" (Get-PackageFiles $overrideRoot $false) $overrideScopes $ExpectedAsmdefs
}

if ($failures.Count -gt 0) {
    Write-Host "Platform scopes do not match the pinned expectations:" -ForegroundColor Yellow
    foreach ($failure in $failures) { Write-Host "  $failure" -ForegroundColor Red }
    Write-Host ""
    Write-Host "If the change is intended, update the tables at the top of this script in the same" -ForegroundColor Yellow
    Write-Host "commit, so the new scope gets reviewed. A managed plugin declaring __Internal imports" -ForegroundColor Yellow
    Write-Host "can never target Win, Win64 or Linux64, whatever the table says." -ForegroundColor Yellow
    exit 3
}

Write-Host "Pinned $scopesChecked platform scope(s); verified __Internal imports on $importsChecked assembly(ies)." -ForegroundColor Green
exit 0
