param(
    [string] $UnityPath,
    [string] $Platform = "",
    [Switch] $CheckSymbols
)

if (-not $Global:NewProjectPathCache)
{
    . ./test/Scripts.Integration.Test/globals.ps1
}

. ./test/Scripts.Integration.Test/common.ps1

$UnityPath = FormatUnityPath $UnityPath

$unityArgs = @( `
        "-quit", "-batchmode", "-nographics", "-disable-assembly-updater", "-projectPath ", $(GetNewProjectPath), `
        "-executeMethod", "Sentry.Unity.Editor.ConfigurationWindow.SentryEditorWindowInstrumentation.ConfigureOptions", `
        "-deprecatedBuildTimeOptionsScript", "BuildTimeOptions", `
        "-deprecatedRuntimeOptionsScript", "RuntimeOptions", `
        "-optionsScript", "OptionsConfiguration", `
        "-cliOptionsScript", "CliConfiguration", `
        "-cliOptions.UrlOverride", ($CheckSymbols ? (SymbolServerUrlFor $UnityPath $Platform) : "") )

RunUnityAndExpect $UnityPath "ConfigureSentryOptions" "ConfigureOptions: SUCCESS" $unityArgs

function AssertPathExists([string] $Path)
{
    if (!(Test-Path $Path))
    {
        Write-Error "Path is expected to exist but it doesn't: '$Path'"
    }
}

AssertPathExists "$(GetNewProjectAssetsPath)/Plugins/Sentry/SentryCliOptions.asset"
AssertPathExists "$(GetNewProjectAssetsPath)/Resources/Sentry/BuildTimeOptions.asset"
AssertPathExists "$(GetNewProjectAssetsPath)/Resources/Sentry/SentryOptions.asset"
AssertPathExists "$(GetNewProjectAssetsPath)/Resources/Sentry/RuntimeOptions.asset"
