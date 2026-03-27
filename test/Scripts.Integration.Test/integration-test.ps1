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

if (-not $Global:NewProjectPathCache) {
    . $PSScriptRoot/globals.ps1
}

. $PSScriptRoot/common.ps1

# Validate package path exists
If (-not (Test-Path -Path $PackagePath)) {
    Throw "Package path does not exist: '$PackagePath'. If running locally, use dev-integration-test.ps1 with -Repack flag."
}

$Global:UnityVersionInUse = $UnityVersion

$UnityPath = FormatUnityPath $UnityPath

# Support recreating the integration test project without cleaning the SDK build (and repackaging).
if ($Recreate -and (Test-Path -Path $(GetNewProjectPath))) {
    Remove-Item -Path $(GetNewProjectPath) -Recurse -Force -Confirm:$false
}

If (-not(Test-Path -Path "$(GetNewProjectPath)")) {
    Write-PhaseHeader "Creating Project"
    Write-Log "Project path: $(GetNewProjectPath)"
    ./test/Scripts.Integration.Test/create-project.ps1 "$UnityPath"
    Write-PhaseSuccess "Project created"

    Write-PhaseHeader "Adding Sentry"
    Write-Log "Package path: $PackagePath"
    ./test/Scripts.Integration.Test/add-sentry.ps1 "$UnityPath" -PackagePath $PackagePath
    Write-PhaseSuccess "Sentry added"

    Write-PhaseHeader "Configuring Sentry"
    ./test/Scripts.Integration.Test/configure-sentry.ps1 "$UnityPath" -Platform $Platform
    Write-PhaseSuccess "Sentry configured"
}

# Copying the native SDK over to the expected directory.
If ($NativeSDKPath -and (Test-Path $NativeSDKPath)) {
    Write-PhaseHeader "Setting up the native plugin for $Platform"
    ./test/Scripts.Integration.Test/copy-native-plugins.ps1 `
        -SourceDirectory $NativeSDKPath `
        -TargetDirectory "$(GetNewProjectAssetsPath)/Plugins/Sentry/$Platform" `
        -Platform $Platform
    Write-PhaseSuccess "Native plugins copied"
}
Else {
    Write-Log "No NativeSDKPath provided (native features disabled)" -ForegroundColor Yellow
}

# Support rebuilding the integration test project.
If ($Rebuild -or -not(Test-Path -Path $(GetNewProjectBuildPath))) {
    Write-PhaseHeader "Building Project"

    If ("iOS" -eq $Platform) {
        # We're exporting an Xcode project and building that in a separate step.
        ./test/Scripts.Integration.Test/build-project.ps1 -UnityPath "$UnityPath" -UnityVersion $UnityVersion -Platform $Platform
        & "./scripts/compile-xcode-project.ps1"
    }
    Else {
        ./test/Scripts.Integration.Test/build-project.ps1 -UnityPath "$UnityPath" -UnityVersion $UnityVersion -Platform $Platform
    }
    Write-PhaseSuccess "Project built"
}

If ($SkipTests) {
    Write-Log "Skipping tests (-SkipTests flag set)" -ForegroundColor Yellow
}
Else {
    Write-PhaseHeader "Running Tests"

    Switch -Regex ($Platform) {
        "^(Windows|MacOS|Linux)$" {
            $env:SENTRY_TEST_PLATFORM = "Desktop"
            $env:SENTRY_TEST_APP = GetNewProjectBuildPath
            Invoke-Pester -Path test/IntegrationTest/Integration.Tests.ps1 -CI
        }
        "^(Android)$" {
            $env:SENTRY_TEST_PLATFORM = "Android"
            $env:SENTRY_TEST_APP = "$(GetNewProjectBuildPath)/test.apk"
            Invoke-Pester -Path test/IntegrationTest/Integration.Tests.ps1 -CI
        }
        "^iOS$" {
            $env:SENTRY_TEST_PLATFORM = "iOS"
            $env:SENTRY_TEST_APP = "$(GetNewProjectBuildPath)/IntegrationTest.app"
            Invoke-Pester -Path test/IntegrationTest/Integration.Tests.ps1 -CI
        }
        "^WebGL$" {
            $env:SENTRY_TEST_PLATFORM = "WebGL"
            $env:SENTRY_TEST_APP = GetNewProjectBuildPath
            Invoke-Pester -Path test/IntegrationTest/Integration.Tests.ps1 -CI
        }
        "^Switch$" {
            Write-PhaseSuccess "Switch build completed - no automated test execution available"
        }
        "^(XSX|XB1)$" {
            $env:SENTRY_TEST_PLATFORM = "Xbox"
            $env:SENTRY_TEST_APP = GetNewProjectBuildPath
            Invoke-Pester -Path test/IntegrationTest/Integration.Tests.ps1 -CI
        }
        "^PS5$"
        {
            Write-PhaseSuccess "PS5 build completed - no automated test execution available"
        }
        Default { Write-Warning "No test run for platform: '$platform'" }
    }
}
