param(
    [string] $UnityPath = "",
    [string] $Platform = "",
    [string] $BuildPath = ""
)

$ErrorActionPreference = "Stop"

. $PSScriptRoot/unity-utils.ps1
. $PSScriptRoot/../test/Scripts.Integration.Test/globals.ps1

if (-not $UnityPath)
{
    $UnityPath = FindNewestUnity
}

$unityPath = FormatUnityPath $UnityPath
$buildMethod = BuildMethodFor $Platform
$projectPath = "$PSScriptRoot/../samples/unity-of-bugs"

if (-not $BuildPath)
{
    $appName = GetTestAppName $buildMethod
    $BuildPath = "$projectPath/Build/$appName"
}

Write-Host "Building unity-of-bugs sample for $Platform"
Write-Host "  Build method: $buildMethod"
Write-Host "  Output path: $BuildPath"

$unityArgs = @(
    "-batchmode",
    "-quit",
    "-projectPath", $projectPath,
    "-executeMethod", $buildMethod,
    "-buildPath", $BuildPath
)

RunUnity $unityPath $unityArgs
