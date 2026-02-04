# ┌───────────────────────────────────────────────────────────────┐ #
# │    Local development wrapper for integration testing.         │ #
# │    Handles SDK packaging and Unity path detection,            │ #
# │    then calls integration-test.ps1 for the actual test.       │ #
# └───────────────────────────────────────────────────────────────┘ #

param(
    [Parameter(Mandatory = $true)][string] $UnityVersion,
    [Parameter(Mandatory = $true)][string] $Platform,
    [string] $NativeSDKPath,
    [switch] $Clean,
    [switch] $Repack,
    [switch] $Recreate,
    [switch] $Rebuild,
    [switch] $SkipTests
)

if (-not $Global:NewProjectPathCache)
{
    . ./test/Scripts.Integration.Test/globals.ps1
}

. ./test/Scripts.Integration.Test/common.ps1

$Global:UnityVersionInUse = $UnityVersion

# Detect local Unity installation
$UnityPath = $null

If ($IsMacOS)
{
    $UnityPath = "/Applications/Unity/Hub/Editor/$UnityVersion*/Unity.app/"
    $UnityPath = (Resolve-Path $UnityPath | Select-Object -First 1).Path
}
Elseif ($IsWindows)
{
    $UnityPath = "C:/Program Files/Unity/Hub/Editor/$UnityVersion/Editor/Unity.exe"
}

If (-not(Test-Path -Path $UnityPath))
{
    Throw "Failed to find Unity at '$UnityPath'"
}

# Handle cleanup
If ($Clean)
{
    Write-PhaseHeader "Cleanup"
    If (Test-Path -Path "package-release.zip")
    {
        Remove-Item -Path "package-release.zip" -Recurse -Force -Confirm:$false
    }
    If (Test-Path -Path "package-release")
    {
        Remove-Item -Path "package-release" -Recurse -Force -Confirm:$false
    }
    If (Test-Path -Path $PackageReleaseOutput)
    {
        Remove-Item -Path $PackageReleaseOutput -Recurse -Force -Confirm:$false
    }
    If (Test-Path -Path $(GetNewProjectPath))
    {
        Remove-Item -Path $(GetNewProjectPath) -Recurse -Force -Confirm:$false
    }
    Write-PhaseSuccess "Cleanup complete"
}

# Build and package the SDK
If ($Repack -Or -not(Test-Path -Path $PackageReleaseOutput))
{
    dotnet build
    Write-PhaseHeader "Creating Package"
    ./scripts/pack.ps1
    Write-Log "Extracting Package..."
    ./test/Scripts.Integration.Test/extract-package.ps1
    Write-PhaseSuccess "Package created"
}

# Call the core integration test script
$integrationTestArgs = @{
    UnityPath    = $UnityPath
    UnityVersion = $UnityVersion
    Platform     = $Platform
    PackagePath  = $PackageReleaseOutput
    Recreate     = $Recreate
    Rebuild      = $Rebuild
    SkipTests    = $SkipTests
}

if ($NativeSDKPath)
{
    $integrationTestArgs.NativeSDKPath = $NativeSDKPath
}

Write-PhaseHeader "Running Integration Test"
./test/Scripts.Integration.Test/integration-test.ps1 @integrationTestArgs
