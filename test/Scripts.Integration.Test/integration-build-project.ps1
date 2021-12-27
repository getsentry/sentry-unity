param($arg)

. ./test/Scripts.Integration.Test/IntegrationGlobals.ps1

$unityPath = FormatUnityPath $arg

ClearUnityLog

$buildMethod = $null
If ($IsMacOS)
{
    $buildMethod = "Builder.BuildMacIl2CPPPlayer"
    $buildPath
}
ElseIf($IsWindows)
{
    $buildMethod = "Builder.BuildWindowsIl2CPPPlayer"
}
Else
{
    Throw "Unsupported build"
}

Write-Output "Checking if Project has no errors "
Write-Host -NoNewline "Creating integration project:"
$UnityProcess = Start-Process -FilePath $unityPath -ArgumentList "-batchmode", "-projectPath ", "$NewProjectPath", "-logfile", "$NewProjectLogPath",  "-executeMethod", $buildMethod , "-buildPath", "$NewProjectBuildPath/$(GetTestAppName)", "-quit" -PassThru
Write-Output " OK"

WaitForLogFile 30
Write-Output "Waiting for Unity to build the project."
SubscribeToUnityLogFile $UnityProcess

if ($UnityProcess.ExitCode -ne 0)
{
    Throw "Unity exited with code $($UnityProcess.ExitCode)"
}
else
{
    Write-Output "`nProject Built!!"
}