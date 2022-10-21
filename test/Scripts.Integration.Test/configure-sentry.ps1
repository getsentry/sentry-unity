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
        "-sentryOptions.Dsn", (TestDsnFor $Platform), `
        "-sentryOptionsScript", "SmokeTestOptions", `
        "-attachScreenshot", "true", `
        "-il2cppLineNumbers", "true", `
        "-diagnosticLevel", "debug",
        "-traceSampleRate", "true",
        "-performanceAutoInstrumentation", "true")

if ($CheckSymbols)
{
    $unityArgs += @( `
            "-cliOptions.UploadSources", "true", `
            "-cliOptions.Org", "sentry-sdks", `
            "-cliOptions.Project", "sentry-unity", `
            "-cliOptions.Auth", "dummy-token", `
            "-cliOptions.UrlOverride", (SymbolServerUrlFor $UnityPath $Platform))
}

RunUnityAndExpect $UnityPath "ConfigureSentryOptions" "ConfigureOptions: SUCCESS" $unityArgs
