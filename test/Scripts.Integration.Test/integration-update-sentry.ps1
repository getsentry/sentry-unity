param($arg)

. ./test/Scripts.Integration.Test/IntegrationGlobals.ps1

$unityPath = FormatUnityPath $arg
$packageReleaseZip = "package-release.zip"

# Check if SDK is packed.
$packageFile = "package-release.zip"
If (Test-Path -Path "$(ProjectRoot)/$packageFile" ) {
    Write-Host "Found $packageFile"
}
Else {
    Throw "$packageFile on $(ProjectRoot) but it was not found. Be sure you run ./scripts/pack.ps1"
}

Write-Host -NoNewline "clearing $PackageReleaseOutput and Extracting $packageReleaseZip :"
if (Test-Path -Path "$PackageReleaseOutput") {
    Remove-Item -Path "$PackageReleaseOutput" -Recurse
}

Expand-Archive -LiteralPath "$(ProjectRoot)/$packageReleaseZip" -DestinationPath "$PackageReleaseOutput"
Write-Host "OK"

If (-not(Test-Path -Path "$PackageReleaseOutput")) {
    Throw "Path $PackageReleaseOutput does not exist. Be sure to run ./test/Scripts.Integration.Test/integration-create-project."
}

function RunUnityAndExpect([string] $name, [string] $successMessage, [string] $failMessage, [string[]] $arguments) {
    ClearUnityLog

    Write-Host -NoNewline "$name | Starting Unity process:"
    $UnityProcess = RunUnity $unityPath $arguments
    Write-Host " OK"

    WaitForLogFile 30

    Write-Host "$name | Waiting for Unity to finish."
    $stdout = SubscribeToUnityLogFile $UnityProcess $successMessage $failMessage

    Write-Host $stdout
    If ($UnityProcess.ExitCode -ne 0) {
        $exitCode = $UnityProcess.ExitCode
        Write-Error "$name | Unity exited with code $exitCode"
    }
    ElseIf ($null -ne ($stdout | select-string $successMessage)) {
        Write-Host "`n$name | SUCCESS" -ForegroundColor Green
    }
    Else {
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
        "-sentryOptions.Dsn", "http://publickey@localhost:8000/12345", `
        "-sentryOptionsScript", "SmokeTestOptions")

Write-Host " Unity configuration finished successfully" -ForegroundColor Green
