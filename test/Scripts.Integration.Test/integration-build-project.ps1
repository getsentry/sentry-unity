param(
    [string] $UnityPath,
    [string] $Platform = ""
)

. ./test/Scripts.Integration.Test/IntegrationGlobals.ps1

$unityPath = FormatUnityPath $UnityPath
$buildMethod = BuildMethodFor $Platform
$outputPath = "$NewProjectBuildPath/$(GetTestAppName $buildMethod)"

Write-Host -NoNewline "Executing ${buildMethod}:"
RunUnityCustom $unityPath @("-batchmode", "-projectPath ", "$NewProjectPath", `
        "-executeMethod", $buildMethod , "-buildPath", $outputPath, "-quit")
Write-Host "Project built successfully" -ForegroundColor Green
Get-ChildItem $NewProjectBuildPath
