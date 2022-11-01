# ┌───────────────────────────────────────────────────┐ #
# │    This script is for local use only,             │ #
# │    utilizing the scripts locally we use in CI.    │ #
# └───────────────────────────────────────────────────┘ #

param(
	[string] $UnityVersion,
	[string] $Platform,
	[switch] $Clean,
	[switch] $Repack,
	[switch] $Recreate,
	[switch] $Rebuild
)

. ./test/Scripts.Integration.Test/globals.ps1

$UnityPath = $null

If ($IsMacOS) {
	$UnityPath = "/Applications/Unity/Hub/Editor/$UnityVersion*/Unity.app/"
} elseif ($IsWindows) {
	$UnityPath = "C:/Program Files/Unity/Hub/Editor/$UnityVersion/Editor/Unity.exe"
}

If (-not(Test-Path -Path $UnityPath)) {
	Throw "Failed to find Unity at '$UnityPath'"
}

If($Clean) {
	Write-Host "Cleanup"
	If(Test-Path -Path "package-release.zip") {
		Remove-Item -Path "package-release.zip" -Recurse -Force -Confirm:$false
	}
	If(Test-Path -Path "package-release") {
		Remove-Item -Path "package-release" -Recurse -Force -Confirm:$false
	}
	If(Test-Path -Path $PackageReleaseOutput) {
		Remove-Item -Path $PackageReleaseOutput -Recurse -Force -Confirm:$false
	}
	If(Test-Path -Path $NewProjectPath) {
		Remove-Item -Path $NewProjectPath -Recurse -Force -Confirm:$false
	}
}

If (-not(Test-Path -Path $PackageReleaseOutput) -Or $Repack) {
	Write-Host "Creating Package"
	./scripts/pack.ps1
	Write-Host "Extracting Package"
	./test/Scripts.Integration.Test/extract-package.ps1
}

If (-not(Test-Path -Path "$NewProjectPath") -Or $Recreate) {
	Write-Host "Creating Project"
	./test/Scripts.Integration.Test/create-project.ps1 "$UnityPath"
	Write-Host "Adding Sentry"
	./test/Scripts.Integration.Test/add-sentry.ps1 "$UnityPath"
	Write-Host "Configuring Sentry"
	./test/Scripts.Integration.Test/configure-sentry.ps1 "$UnityPath" -Platform $Platform
}

# If ($Platform -eq "Android") {
# 	./test/Scripts.Integration.Test/build-project.ps1 "$UnityPath" -Platform "Android"
# 	./scripts/smoke-test-droid.ps1 -IsIntegrationTest
# }

# If ($Platform -eq "iOS") {
# 	./test/Scripts.Integration.Test/build-project.ps1 "$UnityPath" -Platform "iOS"
# 	./Scripts/smoke-test-ios.ps1 Build -UnityVersion "2022"
# 	./Scripts/smoke-test-ios.ps1 Test "iOS 12.4" -IsIntegrationTest
# }

If(-not(Test-Path -Path "Samples/IntegrationTest/Build") -Or $Rebuild) {
	Write-Host "Building Project"
	./test/Scripts.Integration.Test/build-project.ps1 -UnityPath "$UnityPath" -UnityVersion $UnityVersion -Platform $Platform
}

Write-Host "Running Smoke Test"
If ($Platform -eq "macOS") {
	./test/Scripts.Integration.Test/run-smoke-test.ps1 -Smoke
}

If ($Platform -eq "WebGL") {
	Start-Process "python3" -ArgumentList @("./Scripts/smoke-test-webgl.py", "Samples/IntegrationTest/Build")
}
