param(
    [string] $UnityPath,
    [string] $Platform = ""
)

. ./test/Scripts.Integration.Test/IntegrationGlobals.ps1

$UnityPath = FormatUnityPath $UnityPath

function RunUnityAndExpect([string] $name, [string] $successMessage, [string] $failMessage, [string[]] $arguments)
{
    $stdout = RunUnity $UnityPath $arguments
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
        "-batchmode", "-projectPath ", "$NewProjectPath", "-logfile", "$NewProjectLogPath", "-installSentry", "Disk")

Write-Host -NoNewline "Updating test files "
# We were previously using an empty SmokeTester to not generate Build errors.
# It was only required to not cause build errors since the new project did't have Sentry installed.
Remove-Item -Path "$NewProjectAssetsPath/Scripts/SmokeTester.cs"
Remove-Item -Path "$NewProjectAssetsPath/Scripts/SmokeTester.cs.meta"
Copy-Item "$UnityOfBugsPath/Assets/Scripts/SmokeTester.cs"              -Destination "$NewProjectAssetsPath/Scripts/"
Copy-Item "$UnityOfBugsPath/Assets/Scripts/SmokeTester.cs.meta"         -Destination "$NewProjectAssetsPath/Scripts/"
Copy-Item "$UnityOfBugsPath/Assets/Scripts/SmokeTestOptions.cs"         -Destination "$NewProjectAssetsPath/Scripts/"
Copy-Item "$UnityOfBugsPath/Assets/Scripts/SmokeTestOptions.cs.meta"    -Destination "$NewProjectAssetsPath/Scripts/"
Copy-Item "$PackageReleaseAssetsPath/Scripts/NativeSupport/CppPlugin.*" -Destination "$NewProjectAssetsPath/Scripts/"

RunUnityAndExpect "ConfigureSentryOptions" "ConfigureOptions: Sentry options Configured" "ConfigureOptions failed" @( `
        "-quit", "-batchmode", "-nographics", "-projectPath ", "$NewProjectPath", "-logfile", "$NewProjectLogPath", `
        "-executeMethod", "Sentry.Unity.Editor.ConfigurationWindow.SentryEditorWindowInstrumentation.ConfigureOptions", `
        "-sentryOptions.Dsn", "$(TestDsnFor $Platform)", `
        "-sentryOptionsScript", "SmokeTestOptions", `
        "-attachScreenshot", "true")

Write-Host " Unity configuration finished successfully" -ForegroundColor Green
