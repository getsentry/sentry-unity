param(
    # Path to the prebuilt DependencyConflict UPM package (the `package/` folder,
    # with its Runtime/*.dll populated). Defaults to the in-repo location, which
    # is correct for local runs where the DLLs were built via
    # `dotnet build test/Scripts.Integration.Test/DependencyConflictPackage`. In CI the
    # DLLs are built in build.yml and downloaded as an artifact, so the caller
    # points this at the downloaded copy.
    [string] $PackagePath,

    # Skip installing the package and instead define SENTRY_DISABLE_DEPENDENCY_CONFLICT
    # so IntegrationTester.cs compiles out its call into the package.
    [switch] $Disable
)

if (-not $Global:NewProjectPathCache)
{
    . $PSScriptRoot/globals.ps1
}

. $PSScriptRoot/common.ps1

if ($Disable)
{
    $cscRspPath = "$(GetNewProjectAssetsPath)/csc.rsp"
    Write-Log "Defining SENTRY_DISABLE_DEPENDENCY_CONFLICT via '$cscRspPath'..."
    Add-Content -Path $cscRspPath -Value "-define:SENTRY_DISABLE_DEPENDENCY_CONFLICT"
    return
}

# The DependencyConflict package ships plain, UNALIASED System.*/Microsoft.*
# assemblies at versions that differ from the ones the Sentry SDK ships aliased.
# Dropping it into the test project as an embedded package - alongside Sentry -
# means the project's build only succeeds while the SDK's assembly aliasing
# keeps the two dependency sets from colliding. A green build is the regression
# signal.

if (-not $PackagePath)
{
    $PackagePath = $DependencyConflictPackagePath
}

if (-not [System.IO.Path]::IsPathRooted($PackagePath))
{
    $PackagePath = "$(ProjectRoot)/$PackagePath"
}

if (-not (Test-Path -Path "$PackagePath/Runtime/DependencyConflictPackage.dll"))
{
    Write-Error "DependencyConflict package not found at '$PackagePath'. Build it with 'dotnet build test/Scripts.Integration.Test/DependencyConflictPackage' or download the 'dependency-conflict-package' artifact first."
}

$embeddedPackagePath = "$(GetNewProjectPath)/Packages/io.sentry.dependency-conflict"

Write-Log "Copying DependencyConflict package into '$embeddedPackagePath'..."
if (Test-Path -Path $embeddedPackagePath)
{
    Remove-Item -LiteralPath $embeddedPackagePath -Force -Recurse
}
New-Item -Path $embeddedPackagePath -ItemType "directory" | Out-Null
Copy-Item -Recurse "$PackagePath/*" -Destination $embeddedPackagePath

Write-Log "DependencyConflict package installed."
