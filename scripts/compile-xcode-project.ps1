param (
    [string] $iOSMinVersion = ""
)

. $PSScriptRoot/../test/Scripts.Integration.Test/common.ps1
. $PSScriptRoot/../test/Scripts.Integration.Test/capture-corpus.ps1

# Capture mode: sentry-cli refuses to combine a URL from the environment with the auth token
# baked into sentry.properties, so both come from here. The capture server ignores the token.
if (Test-CaptureEnabled)
{
    Start-CaptureServer
    $env:SENTRY_URL = $Global:CaptureUrl
    $env:SENTRY_AUTH_TOKEN = "capture-mode"
}

$ProjectName = "Unity-iPhone"
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$XcodeArtifactPath = Join-Path $repoRoot "samples/IntegrationTest/Build"
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
