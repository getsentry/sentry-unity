param(
    [string] $UnityPath,
    [string] $Platform = "",
    [Switch] $CheckSymbols
)

. ./test/Scripts.Integration.Test/globals.ps1
. ./test/Scripts.Integration.Test/common.ps1

$UnityPath = FormatUnityPath $UnityPath

function RunUnityAndExpect([string] $name, [string] $successMessage, [string] $failMessage, [string[]] $arguments)
{
    $stdout = RunUnityCustom $UnityPath $arguments -ReturnLogOutput
    If ($null -ne ($stdout | Select-String $successMessage))
    {
        Write-Host "`n$name | SUCCESS" -ForegroundColor Green
    }
    Else
    {
        Write-Error "$name | Unity exited without an error but the successMessage was not found in the output ('$successMessage')"
    }
}

RunUnityAndExpect "AddSentryPackage" "Sentry Package Installation:" "Sentry setup: FAILED" @( `
        "-batchmode", "-projectPath ", "$NewProjectPath", "-installSentry", "Disk")

Write-Host -NoNewline "Copying Integration Test Files"
New-Item -Path "$NewProjectAssetsPath" -Name "Scripts" -ItemType "directory"
Copy-Item -Recurse "$IntegrationScriptsPath/Scripts/*" -Destination "$NewProjectAssetsPath/Scripts/"

Write-Host " OK"

$unityArgs = @( `
        "-quit", "-batchmode", "-nographics", "-projectPath ", $NewProjectPath, `
        "-executeMethod", "Sentry.Unity.Editor.ConfigurationWindow.SentryEditorWindowInstrumentation.ConfigureOptions", `
        "-sentryOptions.Dsn", (TestDsnFor $Platform), `
        "-sentryOptionsScript", "SmokeTestOptions", `
        "-attachScreenshot", "true", `
        "-il2cppLineNumbers", "true", `
        "-diagnosticLevel", "debug")

if ($CheckSymbols)
{
    $unityArgs += @( `
            "-cliOptions.UploadSources", "true", `
            "-cliOptions.Org", "sentry-sdks", `
            "-cliOptions.Project", "sentry-unity", `
            "-cliOptions.Auth", "dummy-token", `
            "-cliOptions.UrlOverride", (SymbolServerUrlFor $UnityPath $Platform))
}

RunUnityAndExpect "ConfigureSentryOptions" "ConfigureOptions: Sentry options Configured" "ConfigureOptions failed" $unityArgs

Write-Host " Unity configuration finished successfully" -ForegroundColor Green
