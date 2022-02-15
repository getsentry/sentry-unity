param($arg, $setupSentry, $buildMethod)

. ./test/Scripts.Integration.Test/IntegrationGlobals.ps1

$unityPath = FormatUnityPath $arg

ClearUnityLog

if ($null -eq $buildMethod)
{
    If ($IsMacOS)
    {
        $buildMethod = "Builder.BuildMacIl2CPPPlayer"
    }
    ElseIf ($IsWindows)
    {
        $buildMethod = "Builder.BuildWindowsIl2CPPPlayer"
    }
    Else
    {
        Throw "Unsupported build"
    }
}

Write-Host "Checking if Project has no errors "
Write-Host -NoNewline "Creating integration project:"
If (-not $setupSentry)
{
    $UnityProcess = RunUnity $unityPath @("-batchmode", "-projectPath ", "$NewProjectPath", "-logfile", "$NewProjectLogPath", "-executeMethod", $buildMethod , "-buildPath", "$NewProjectBuildPath/$(GetTestAppName $buildMethod)", "-quit")
}
Else {
    $UnityProcess = RunUnity $unityPath @("-batchmode", "-projectPath ", "$NewProjectPath", "-logfile", "$NewProjectLogPath", "-executeMethod", $buildMethod , "-buildPath", "$NewProjectBuildPath/$(GetTestAppName $buildMethod)", "-sentryOptions.configure", $True, "-sentryOptions.Dsn", "https://94677106febe46b88b9b9ae5efd18a00@o447951.ingest.sentry.io/5439417", "-quit")
}
Write-Host " OK"

WaitForLogFile 30
Write-Host "Waiting for Unity to build the project."
SubscribeToUnityLogFile $UnityProcess

if ($UnityProcess.ExitCode -ne 0)
{
    Throw "Unity exited with code $($UnityProcess.ExitCode)"
}
else
{
    Write-Host "`nProject Built!!"
}
