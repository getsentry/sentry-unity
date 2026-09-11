param(
    [string] $UnityPath,
    [string] $Platform = "",
    [string] $UnityVersion = "",
    [string] $BuildDirName = "Build"
)

if (-not $Global:NewProjectPathCache)
{
    . $PSScriptRoot/globals.ps1
}

. $PSScriptRoot/common.ps1
. $PSScriptRoot/capture-corpus.ps1

# Capture mode: sentry-cli uploads the debug files during the build, so the server has to be up
# for its duration. SENTRY_URL is what redirects it - sentry-cli 3.x ignores the `defaults.url`
# the SDK writes into sentry.properties.
if (Test-CaptureEnabled)
{
    Start-CaptureServer
    $env:SENTRY_URL = $Global:CaptureUrl
}

$unityPath = FormatUnityPath $UnityPath
$buildMethod = BuildMethodFor $Platform
$buildDirectory = "$(GetNewProjectPath)/$BuildDirName"
$outputPath = "$buildDirectory/$(GetTestAppName $buildMethod)"

Write-Log "Build method: $buildMethod"
Write-Detail "Output path: $outputPath"
$unityArgs = @("-batchmode", "-projectPath ", "$(GetNewProjectPath)", "-executeMethod", $buildMethod , "-buildPath", $outputPath, "-quit")

RunUnityCustom $unityPath $unityArgs

if ($Platform -eq "Android-Export")
{
    # See test/Scripts.Integration.Test/gradle/README.md
    $gradleVersion = "v6.1.1"
    Copy-Item -Force -Recurse "$IntegrationScriptsPath/gradle/$gradleVersion/*" -Destination $outputPath
}


# Not for the size-comparison build: it has no SDK, so it uploads nothing and would warn spuriously.
if ($BuildDirName -eq "Build")
{
    Show-CaptureSummary
}

Write-Log "Build output:"
Get-ChildItem $buildDirectory | ForEach-Object { Write-Detail $_.Name }
