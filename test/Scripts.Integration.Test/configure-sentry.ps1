param(
    [string] $UnityPath,
    [string] $Platform = "",
    [Switch] $CheckSymbols
)

. ./test/Scripts.Integration.Test/globals.ps1
. ./test/Scripts.Integration.Test/common.ps1

$UnityPath = FormatUnityPath $UnityPath

$unityArgs = @( `
        "-quit", "-batchmode", "-nographics", "-disable-assembly-updater", "-projectPath ", $NewProjectPath, `
        "-executeMethod", "Sentry.Unity.Editor.ConfigurationWindow.SentryEditorWindowInstrumentation.ConfigureOptions", `
        "-buildTimeOptionsScript", "BuildTimeOptions", `
        "-runtimeOptionsScript", "RuntimeOptions", `
        "-cliOptions.UrlOverride", ($CheckSymbols ? (SymbolServerUrlFor $UnityPath $Platform) : "") )

RunUnityAndExpect $UnityPath "ConfigureSentryOptions" "ConfigureOptions: SUCCESS" $unityArgs

function AssertPathExists([string] $Path)
{
    if (!(Test-Path $Path))
    {
        Write-Error "Path is expected to exist but it doesn't: '$Path'"
    }
}

AssertPathExists "$NewProjectAssetsPath/Plugins/Sentry/SentryCliOptions.asset"
AssertPathExists "$NewProjectAssetsPath/Resources/Sentry/BuildTimeOptions.asset"
AssertPathExists "$NewProjectAssetsPath/Resources/Sentry/SentryOptions.asset"
AssertPathExists "$NewProjectAssetsPath/Resources/Sentry/RuntimeOptions.asset"
