Set-StrictMode -Version latest
$ErrorActionPreference = "Stop"

Write-Output "Setting up Apple Developer Team ID from environment variable"

if (-not $Env:APPLE_ID)
{
    Write-Error "APPLE_ID environment variable is not set. Skipping..."
    exit
}

$appleId = $Env:APPLE_ID
$projectSettingsPath = "$PSScriptRoot/../samples/unity-of-bugs/ProjectSettings/ProjectSettings.asset"
if (-not (Test-Path -Path $projectSettingsPath)) 
{
    Write-Error "ProjectSettings.asset not found at path: $projectSettingsPath"
    exit
}

$content = Get-Content -Path $projectSettingsPath -Raw
if ($content -match '(\s*)appleDeveloperTeamID:.*') 
{
    $updatedContent = $content -replace '(\s*)appleDeveloperTeamID:.*', "`${1}appleDeveloperTeamID: $appleId"
    Set-Content -Path $projectSettingsPath -Value $updatedContent
    Write-Output "Successfully updated appleDeveloperTeamID in ProjectSettings.asset"
} 
else 
{
    Write-Error "Could not find appleDeveloperTeamID property in ProjectSettings.asset"
    exit
}
