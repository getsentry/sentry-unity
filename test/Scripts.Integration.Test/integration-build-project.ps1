param(
    [string] $UnityPath,
    [string] $Platform = "",
    [Switch] $CheckSymbols
)

. ./test/Scripts.Integration.Test/IntegrationGlobals.ps1
. ./test/Scripts.Integration.Test/common.ps1

$unityPath = FormatUnityPath $UnityPath
$buildMethod = BuildMethodFor $Platform
$outputPath = "$NewProjectBuildPath/$(GetTestAppName $buildMethod)"

Write-Host -NoNewline "Executing ${buildMethod}:"
$unityArgs = @("-batchmode", "-projectPath ", "$NewProjectPath", "-executeMethod", $buildMethod , "-buildPath", $outputPath, "-quit")

if ($CheckSymbols)
{
    $symbolServerOutput = RunWithSymbolServer -Callback { RunUnityCustom $unityPath $unityArgs }
    CheckSymbolServerOutput $buildMethod $symbolServerOutput
}
else
{
    RunUnityCustom $unityPath $unityArgs
}
Write-Host "Project built successfully" -ForegroundColor Green
Get-ChildItem $NewProjectBuildPath
