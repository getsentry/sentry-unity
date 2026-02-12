param(
    [string] $UnityPath,
    [string] $Platform = "",
    [Switch] $CheckSymbols,
    [string] $NativeSupportEnabled = ""
)

if (-not $Global:NewProjectPathCache)
{
    . $PSScriptRoot/globals.ps1
}

. $PSScriptRoot/common.ps1

$UnityPath = FormatUnityPath $UnityPath

Write-Log "Configuring Sentry options..."

$unityArgs = @( `
        "-quit", "-batchmode", "-nographics", "-disable-assembly-updater", "-projectPath ", $(GetNewProjectPath), `
        "-executeMethod", "Sentry.Unity.Editor.ConfigurationWindow.SentryEditorWindowInstrumentation.ConfigureOptions", `
        "-optionsScript", "OptionsConfiguration", `
        "-cliOptionsScript", "CliConfiguration", `
        "-cliOptions.UrlOverride", ($CheckSymbols ? (SymbolServerUrlFor $UnityPath $Platform) : "") )

if ($NativeSupportEnabled -ne "") {
    $optionName = switch ($Platform) {
        "Switch" { "SwitchNativeSupportEnabled" }
        default { $null }
    }
    if ($optionName) {
        Write-Log "Setting $optionName to $NativeSupportEnabled"
        $unityArgs += @("-options.$optionName", $NativeSupportEnabled)
    }
}

RunUnityAndExpect $UnityPath "ConfigureSentryOptions" "ConfigureOptions: SUCCESS" $unityArgs


function AssertPathExists([string] $Path)
{
    if (!(Test-Path $Path))
    {
        Write-Error "Path is expected to exist but it doesn't: '$Path'"
    }
}

AssertPathExists "$(GetNewProjectAssetsPath)/Plugins/Sentry/SentryCliOptions.asset"
AssertPathExists "$(GetNewProjectAssetsPath)/Resources/Sentry/SentryOptions.asset"
