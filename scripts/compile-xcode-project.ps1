param (
    [string] $iOSMinVersion = ""
)

. $PSScriptRoot/../test/Scripts.Integration.Test/common.ps1

$ProjectName = "Unity-iPhone"
$XcodeArtifactPath = "samples/IntegrationTest/Build"
$ArchivePath = "$XcodeArtifactPath/archive"

MakeExecutable "$XcodeArtifactPath/MapFileParser.sh"
MakeExecutable "$XcodeArtifactPath/sentry-cli-Darwin-universal"

If (-not $IsMacOS)
{
    Write-Log "This script should only be run on a MacOS." -ForegroundColor Yellow
}

Write-Host "::group::Building iOS project"
try
{
    xcodebuild `
        -project "$XcodeArtifactPath/$ProjectName.xcodeproj" `
        -scheme "Unity-iPhone" `
        -configuration "Release" `
        -sdk "iphonesimulator" `
        -destination "platform=iOS Simulator,OS=$iOSMinVersion" `
        -destination "platform=iOS Simulator,OS=latest" `
        -parallel-testing-enabled YES `
        -derivedDataPath "$ArchivePath/$ProjectName" `
    | Write-Host
}
finally
{
    Write-Host "::endgroup::"
}
