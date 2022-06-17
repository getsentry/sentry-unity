﻿param(
    [string] $UnityPath,
    [string] $Platform = "",
    [string] $UnityVersion = "",
    [Switch] $CheckSymbols
)

. ./test/Scripts.Integration.Test/globals.ps1
. ./test/Scripts.Integration.Test/common.ps1

$unityPath = FormatUnityPath $UnityPath
$buildMethod = BuildMethodFor $Platform
$outputPath = "$NewProjectBuildPath/$(GetTestAppName $buildMethod)"

Write-Host "Executing ${buildMethod}:"
$unityArgs = @("-batchmode", "-projectPath ", "$NewProjectPath", "-executeMethod", $buildMethod , "-buildPath", $outputPath, "-quit")

if ($CheckSymbols)
{
    $symbolServerOutput = RunWithSymbolServer -Callback { RunUnityCustom $unityPath $unityArgs }
    CheckSymbolServerOutput $buildMethod $symbolServerOutput $UnityVersion
}
else
{
    RunUnityCustom $unityPath $unityArgs
}

Write-Host "Project built successfully" -ForegroundColor Green
Get-ChildItem $NewProjectBuildPath
