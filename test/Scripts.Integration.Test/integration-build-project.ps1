param(
    [string] $UnityPath,
    [string] $Platform = ""
)

. ./test/Scripts.Integration.Test/IntegrationGlobals.ps1

$unityPath = FormatUnityPath $UnityPath
$buildMethod = BuildMethodFor $Platform
$outputPath = "$NewProjectBuildPath/$(GetTestAppName $buildMethod)"

ClearUnityLog

Write-Host -NoNewline "Executing ${buildMethod}:"
$UnityProcess = RunUnity $unityPath @("-batchmode", "-projectPath ", "$NewProjectPath", "-logfile", "$NewProjectLogPath", `
        "-executeMethod", $buildMethod , "-buildPath", $outputPath, "-quit")
Write-Host " OK"

WaitForLogFile 30
Write-Host "Waiting for Unity to build the project."
SubscribeToUnityLogFile $UnityProcess

if ($UnityProcess.ExitCode -ne 0)
{
    Throw "Unity exited with code $($UnityProcess.ExitCode)"
}

Write-Host "Project built successfully" -ForegroundColor Green
Get-ItemProperty $outputPath
