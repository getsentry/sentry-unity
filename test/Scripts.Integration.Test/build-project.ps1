param(
    [string] $UnityPath,
    [string] $Platform = "",
    [string] $UnityVersion = "",
    [string] $BuildDirName = "Build",
    [switch] $ConfigureSentry
)

if (-not $Global:NewProjectPathCache)
{
    . $PSScriptRoot/globals.ps1
}

. $PSScriptRoot/common.ps1

$unityPath = FormatUnityPath $UnityPath
$buildMethod = BuildMethodFor $Platform
$buildDirectory = "$(GetNewProjectPath)/$BuildDirName"
$outputPath = "$buildDirectory/$(GetTestAppName $buildMethod)"

Write-Log "Build method: $buildMethod"
Write-Detail "Output path: $outputPath"
$unityArgs = @("-batchmode", "-projectPath ", "$(GetNewProjectPath)", "-executeMethod", $buildMethod , "-buildPath", $outputPath, "-quit")

# Unity kills the process in Library/burst.pid at startup. In a project made in another container that PID names
# an unrelated process here, typically the xvfb-run wrapping the editor.
Remove-Item "$(GetNewProjectPath)/Library/*.pid" -ErrorAction SilentlyContinue

if ($Platform -eq "Switch")
{
    $unityArgs += @("-buildTarget", "Switch")
}

if ($ConfigureSentry)
{
    Write-Log "Configuring Sentry options..."
    $unityArgs += @("-optionsScript", "IntegrationOptionsConfiguration", "-cliOptionsScript", "CliConfiguration")
    if ($env:SENTRY_DSN)
    {
        $unityArgs += @("-dsn", $env:SENTRY_DSN)
    }

    RunUnityAndExpect $unityPath "ConfigureSentryOptions" "ConfigureOptions: SUCCESS" $unityArgs

    foreach ($path in @("$(GetNewProjectAssetsPath)/Plugins/Sentry/SentryCliOptions.asset", "$(GetNewProjectAssetsPath)/Resources/Sentry/SentryOptions.asset"))
    {
        if (!(Test-Path $path))
        {
            Write-Error "Path is expected to exist but it doesn't: '$path'"
        }
    }
}
else
{
    RunUnityCustom $unityPath $unityArgs
}

if ($Platform -eq "Android-Export")
{
    # See test/Scripts.Integration.Test/gradle/README.md
    $gradleVersion = "v6.1.1"
    Copy-Item -Force -Recurse "$IntegrationScriptsPath/gradle/$gradleVersion/*" -Destination $outputPath
}


Write-Log "Build output:"
Get-ChildItem $buildDirectory | ForEach-Object { Write-Detail $_.Name }
