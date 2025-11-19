param(
    [string] $UnityPath,
    [string] $Platform = "",
    [string] $UnityVersion = "",
    [Switch] $CheckSymbols,
    [string] $BuildDirName = "Build"
)

if (-not $Global:NewProjectPathCache)
{
    . ./test/Scripts.Integration.Test/globals.ps1
}

. ./test/Scripts.Integration.Test/common.ps1

$unityPath = FormatUnityPath $UnityPath
$buildMethod = BuildMethodFor $Platform
$buildDirectory = "$(GetNewProjectPath)/$BuildDirName"
$outputPath = "$buildDirectory/$(GetTestAppName $buildMethod)"

Write-Log "Executing ${buildMethod}:"
$unityArgs = @("-batchmode", "-projectPath ", "$(GetNewProjectPath)", "-executeMethod", $buildMethod , "-buildPath", $outputPath, "-quit")

if ($CheckSymbols)
{
    $symbolServerOutput = RunWithSymbolServer -Callback { RunUnityCustom $unityPath $unityArgs }
    CheckSymbolServerOutput $buildMethod $symbolServerOutput $UnityVersion
}
else
{
    RunUnityCustom $unityPath $unityArgs
}

if ($Platform -eq "Android-Export")
{
    # See test/Scripts.Integration.Test/gradle/README.md
    $gradleVersion = $UnityVersion.StartsWith("2019") ? "v5.1.1" : "v6.1.1"
    Copy-Item -Force -Recurse "$IntegrationScriptsPath/gradle/$gradleVersion/*" -Destination $outputPath
}

Write-Log "Project built successfully" -ForegroundColor Green
Get-ChildItem $buildDirectory
