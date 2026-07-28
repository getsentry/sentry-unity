Set-StrictMode -Version latest
$ErrorActionPreference = "Stop"

Write-Output "Setting up Apple Developer Team ID from environment variable"

if (-not $Env:APPLE_ID)
{
    throw "APPLE_ID environment variable is not set."
}

$appleId = $Env:APPLE_ID
$projectSettingsPaths = @(
    "$PSScriptRoot/../samples/unity-of-bugs/ProjectSettings/ProjectSettings.asset",
    "$PSScriptRoot/../samples/unity-of-bugs-local/ProjectSettings/ProjectSettings.asset"
)

foreach ($projectSettingsPath in $projectSettingsPaths) {
    if (-not (Test-Path -Path $projectSettingsPath)) {
        throw "ProjectSettings.asset not found at path: $projectSettingsPath"
    }

    $content = Get-Content -Path $projectSettingsPath -Raw
    if ($content -match '(\s*)appleDeveloperTeamID:.*') {
        $updatedContent = $content -replace '(\s*)appleDeveloperTeamID:.*', "`${1}appleDeveloperTeamID: $appleId"
        Set-Content -Path $projectSettingsPath -Value $updatedContent
        Write-Output "Successfully updated appleDeveloperTeamID in '$projectSettingsPath'"
    }
    else {
        throw "Could not find appleDeveloperTeamID property in '$projectSettingsPath'"
    }
}
