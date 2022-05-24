﻿param(
    [string] $UnityPath,
    [string] $Platform = ""
)

. ./test/Scripts.Integration.Test/IntegrationGlobals.ps1
. ./test/Scripts.Integration.Test/common.ps1

$unityPath = FormatUnityPath $UnityPath
$buildMethod = BuildMethodFor $Platform
$outputPath = "$NewProjectBuildPath/$(GetTestAppName $buildMethod)"

Write-Host -NoNewline "Executing ${buildMethod}:"
$symbolServerOutput = RunWithSymbolServer -Callback {
    RunUnityCustom $unityPath @("-batchmode", "-projectPath ", "$NewProjectPath", `
            "-executeMethod", $buildMethod , "-buildPath", $outputPath, "-quit")
}
CheckSymbolServerOutput $buildMethod $symbolServerOutput
Write-Host "Project built successfully" -ForegroundColor Green
Get-ChildItem $NewProjectBuildPath
