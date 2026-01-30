# ┌───────────────────────────────────────────────────────────────┐ #
# │    Core integration test script.                              │ #
# │    Can be called by dev-integration-test.ps1 (local) or CI.   │ #
# └───────────────────────────────────────────────────────────────┘ #

param(
    [Parameter(Mandatory = $true)][string] $UnityPath,
    [Parameter(Mandatory = $true)][string] $UnityVersion,
    [Parameter(Mandatory = $true)][string] $Platform,
    [Parameter(Mandatory = $true)][string] $PackagePath,
    [string] $NativeSDKPath,
    [switch] $Recreate,
    [switch] $Rebuild,
    [switch] $SkipTests
)

if (-not $Global:NewProjectPathCache)
{
    . ./test/Scripts.Integration.Test/globals.ps1
}

# Validate package path exists
If (-not (Test-Path -Path $PackagePath))
{
    Throw "Package path does not exist: '$PackagePath'. If running locally, use dev-integration-test.ps1 with -Repack flag."
}

$Global:UnityVersionInUse = $UnityVersion

$UnityPath = FormatUnityPath $UnityPath

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
    ./test/Scripts.Integration.Test/add-sentry.ps1 "$UnityPath" -PackagePath $PackagePath
    Write-Host "Configuring Sentry"
    ./test/Scripts.Integration.Test/configure-sentry.ps1 "$UnityPath" -Platform $Platform -CheckSymbols

    If ($Platform -eq "Switch")
    {
        If (-not $NativeSDKPath -or -not (Test-Path $NativeSDKPath))
        {
            Throw "Switch platform requires -NativeSDKPath parameter pointing to directory containing libsentry.a and libzstd.a"
        }

        Write-Host "Setting up Switch native plugins"
        ./test/Scripts.Integration.Test/copy-native-plugins.ps1 `
            -SourceDirectory $NativeSDKPath `
            -TargetDirectory "$(GetNewProjectAssetsPath)/Plugins/Sentry/Switch" `
            -Platform "Switch"
    }
}

# Support rebuilding the integration test project. I.e. if you make changes to the SmokeTester.cs during
If ($Rebuild -or -not(Test-Path -Path $(GetNewProjectBuildPath)))
{
    Write-Host "Building Project"

    If ("iOS" -eq $Platform)
    {
        # We're exporting an Xcode project and building that in a separate step.
        ./test/Scripts.Integration.Test/build-project.ps1 -UnityPath "$UnityPath" -UnityVersion $UnityVersion -Platform $Platform
        & "./scripts/smoke-test-ios.ps1" Build -IsIntegrationTest -UnityVersion $UnityVersion
    }
    Else
    {
        ./test/Scripts.Integration.Test/build-project.ps1 -UnityPath "$UnityPath" -CheckSymbols -UnityVersion $UnityVersion -Platform $Platform
    }
}

If ($SkipTests)
{
    Write-Host "Skipping tests (-SkipTests flag set)"
}
Else
{
    Write-Host "Running tests"

    Switch -Regex ($Platform)
    {
        "^(Windows|MacOS|Linux)$"
        {
            ./test/Scripts.Integration.Test/run-smoke-test.ps1 -Smoke -Crash
        }
        "^(Android)$"
        {
            ./scripts/smoke-test-android.ps1
        }
        "^iOS$"
        {
            ./scripts/smoke-test-ios.ps1 Test "latest" -IsIntegrationTest
        }
        "^WebGL$"
        {
            python3 scripts/smoke-test-webgl.py $buildDir
        }
        "^Switch$"
        {
            Write-Host "Switch build completed successfully - no automated test execution available"
        }
        Default { Write-Warning "No test run for platform: '$platform'" }
    }
}
