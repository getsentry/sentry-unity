# ┌───────────────────────────────────────────────────┐ #
# │    This script is for local use only,             │ #
# │    utilizing the scripts locally we use in CI.    │ #
# └───────────────────────────────────────────────────┘ #

param(
    [Parameter(Mandatory = $true)][string] $UnityVersion,
    [Parameter(Mandatory = $true)][string] $Platform,
    [switch] $Clean,
    [switch] $Repack,
    [switch] $Recreate,
    [switch] $Rebuild
)

if (-not $Global:NewProjectPathCache)
{
    . ./test/Scripts.Integration.Test/globals.ps1
}

$Global:UnityVersionInUse = $UnityVersion

$UnityPath = $null

If ($IsMacOS)
{
    $UnityPath = "/Applications/Unity/Hub/Editor/$UnityVersion*/Unity.app/"
}
Elseif ($IsWindows)
{
    $UnityPath = "C:/Program Files/Unity/Hub/Editor/$UnityVersion/Editor/Unity.exe"
}

If (-not(Test-Path -Path $UnityPath))
{
    Throw "Failed to find Unity at '$UnityPath'"
}

If ($Clean)
{
    Write-Host "Cleanup"
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
}

# Repackaging the SDK
If ($Repack -Or -not(Test-Path -Path $PackageReleaseOutput))
{
    dotnet build
    Write-Host "Creating Package"
    ./scripts/pack.ps1
    Write-Host "Extracting Package"
    ./test/Scripts.Integration.Test/extract-package.ps1
}

# Support recreating the integration test project without cleaning the SDK build (and repackaging).
if ($Recreate -and (Test-Path -Path $(GetNewProjectPath)))
{
    Remove-Item -Path $(GetNewProjectPath) -Recurse -Force -Confirm:$false
}

If (-not(Test-Path -Path "$(GetNewProjectPath)"))
{
    Write-Host "Creating Project at '$(GetNewProjectPath)'"
    ./test/Scripts.Integration.Test/create-project.ps1 "$UnityPath"
    Write-Host "Adding Sentry"
    ./test/Scripts.Integration.Test/add-sentry.ps1 "$UnityPath"
    Write-Host "Configuring Sentry"
    ./test/Scripts.Integration.Test/configure-sentry.ps1 "$UnityPath" -Platform $Platform -CheckSymbols
}

If ($Rebuild -or -not(Test-Path -Path $(GetNewProjectBuildPath)))
{
    Write-Host "Building Project"

    If (("iOS", "Android-Export") -contains $Platform)
    {
        # Workaround for having `exportAsGoogleAndroidProject` remain `false` in Unity 6 on first build
        ./test/Scripts.Integration.Test/build-project.ps1 -UnityPath "$UnityPath" -UnityVersion $UnityVersion -Platform $Platform
        Remove-Item -Path $(GetNewProjectBuildPath) -Recurse -Force -Confirm:$false

        ./test/Scripts.Integration.Test/build-project.ps1 -UnityPath "$UnityPath" -UnityVersion $UnityVersion -Platform $Platform
        & "./scripts/smoke-test-$($Platform -eq 'iOS' ? 'ios' : 'android').ps1" Build -IsIntegrationTest -UnityVersion $UnityVersion
    }
    Else
    {
        ./test/Scripts.Integration.Test/build-project.ps1 -UnityPath "$UnityPath" -CheckSymbols -UnityVersion $UnityVersion -Platform $Platform
    }
}

Write-Host "Running tests"

Switch -Regex ($Platform)
{
    "^(Windows|MacOS|Linux)$"
    {
        ./test/Scripts.Integration.Test/run-smoke-test.ps1 -Smoke -Crash
    }
    "^(Android|Android-Export)$"
    {
        ./scripts/smoke-test-android.ps1 -IsIntegrationTest
    }
    "^iOS$"
    {
        ./scripts/smoke-test-ios.ps1 Test "latest" -IsIntegrationTest
    }
    "^WebGL$"
    {
        python3 scripts/smoke-test-webgl.py $buildDir
    }
    Default { Write-Warning "No test run for platform: '$platform'" }
}
