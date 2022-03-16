param(
    [string] $UnityPath,
    [switch] $SetupSentry = $false,
    [string] $BuildMethod
)

. ./test/Scripts.Integration.Test/IntegrationGlobals.ps1

$unityPath = FormatUnityPath $UnityPath

ClearUnityLog

if ("$BuildMethod" -eq "")
{
    If ($IsMacOS)
    {
        $BuildMethod = "Builder.BuildMacIl2CPPPlayer"
    }
    ElseIf ($IsWindows)
    {
        $BuildMethod = "Builder.BuildWindowsIl2CPPPlayer"
    }
    ElseIf ($IsLinux) {
        $BuildMethod = "Builder.BuildLinuxIl2CPPPlayer"
    }
    Else
    {
        Throw "Unsupported build"
    }
}

Write-Host "Checking if Project has no errors "
Write-Host -NoNewline "Creating integration project:"
If (-not $SetupSentry)
{
    $UnityProcess = RunUnity $unityPath @("-batchmode", "-projectPath ", "$NewProjectPath", "-logfile", "$NewProjectLogPath", "-executeMethod", $BuildMethod , "-buildPath", "$NewProjectBuildPath/$(GetTestAppName $BuildMethod)", "-quit")
}
Else {
    $UnityProcess = RunUnity $unityPath @("-batchmode", "-projectPath ", "$NewProjectPath", "-logfile", "$NewProjectLogPath", "-executeMethod", $BuildMethod , "-buildPath", "$NewProjectBuildPath/$(GetTestAppName $BuildMethod)", "-sentryOptions.configure", $True, "-sentryOptions.Dsn", "http://publickey@localhost:8000/12345", "-quit")
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
