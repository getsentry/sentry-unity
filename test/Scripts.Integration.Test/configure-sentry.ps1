param(
    [string] $UnityPath,
    [string] $Platform = "",
    [Switch] $CheckSymbols
)

. ./test/Scripts.Integration.Test/globals.ps1
. ./test/Scripts.Integration.Test/common.ps1

$UnityPath = FormatUnityPath $UnityPath

$unityArgs = @( `
        "-quit", "-batchmode", "-nographics", "-projectPath ", $NewProjectPath, `
        "-executeMethod", "Sentry.Unity.Editor.ConfigurationWindow.SentryEditorWindowInstrumentation.ConfigureOptions", `
        "-sentryOptionsScript", "SmokeTestOptions", `
        "-cliOptions.UrlOverride", ($CheckSymbols ? (SymbolServerUrlFor $UnityPath $Platform) : "") )

RunUnityAndExpect $UnityPath "ConfigureSentryOptions" "ConfigureOptions: SUCCESS" $unityArgs
