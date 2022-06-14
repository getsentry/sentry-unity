param(
    [string] $UnityPath,
    [string] $Platform = ""
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

Write-Host -NoNewline "Copying Setup Script"
New-Item -Path "$NewProjectAssetsPath" -Name "Editor" -ItemType "directory"
Copy-Item "$IntegrationScriptsPath/SentrySetup.cs" -Destination "$NewProjectAssetsPath/Editor/SentrySetup.cs"
Write-Host " OK"

RunUnityAndExpect "AddSentryPackage" "Sentry Package Installation:" "Sentry setup: FAILED" @( `
        "-batchmode", "-projectPath ", "$NewProjectPath", "-installSentry", "Disk")

Write-Host -NoNewline "Copying Test Files"
# TODO: Replace copying from sample project with actually importing the package samples
New-Item -Path "$NewProjectAssetsPath" -Name "Scripts" -ItemType "directory"
New-Item -Path "$NewProjectAssetsPath" -Name "Scenes" -ItemType "directory"
Copy-Item -Recurse "$IntegrationScriptsPath/Editor/*" -Destination "$NewProjectAssetsPath/Editor/"
Copy-Item -Recurse "$UnityOfBugsPath/Assets/Scripts/*" -Destination "$NewProjectAssetsPath/Scripts/"
Copy-Item -Recurse "$IntegrationScriptsPath/Scripts/*" -Destination "$NewProjectAssetsPath/Scripts/"
Copy-Item -Recurse "$UnityOfBugsPath/Assets/Scenes/*" -Destination "$NewProjectAssetsPath/Scenes/"
Write-Host " OK"

RunUnityAndExpect "ConfigureSentryOptions" "ConfigureOptions: Sentry options Configured" "ConfigureOptions failed" @( `
        "-quit", "-batchmode", "-nographics", "-projectPath ", $NewProjectPath, `
        "-executeMethod", "Sentry.Unity.Editor.ConfigurationWindow.SentryEditorWindowInstrumentation.ConfigureOptions", `
        "-sentryOptions.Dsn", (TestDsnFor $Platform), `
        "-sentryOptionsScript", "SmokeTestOptions", `
        "-attachScreenshot", "true", `
        "-diagnosticLevel", "debug", `
        "-cliOptions.UploadSources", "true", `
        "-cliOptions.Org", "sentry-sdks", `
        "-cliOptions.Project", "sentry-unity", `
        "-cliOptions.Auth", "dummy-token", `
        "-cliOptions.UrlOverride", (SymbolServerUrlFor $UnityPath $Platform))

Write-Host " Unity configuration finished successfully" -ForegroundColor Green
