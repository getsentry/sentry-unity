param(
    [Parameter(Mandatory = $true)][string] $UnityPath,
    [Parameter(Mandatory = $true)][string] $PackagePath
)

if (-not $Global:NewProjectPathCache)
{
    . $PSScriptRoot/globals.ps1
}

. $PSScriptRoot/common.ps1

# The next editor launch resolves and imports the package
Write-Log "Adding Sentry package to the project manifest..."
$manifestPath = "$(GetNewProjectPath)/Packages/manifest.json"
# Relative to Packages/, so the path resolves on the host and in Docker alike
$PackagePath = [System.IO.Path]::GetRelativePath("$(GetNewProjectPath)/Packages", [System.IO.Path]::GetFullPath($PackagePath, "$(ProjectRoot)")).Replace("\", "/")
$manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json
$manifest.dependencies | Add-Member -NotePropertyName "io.sentry.unity" -NotePropertyValue "file:$PackagePath" -Force
$manifest | ConvertTo-Json -Depth 10 | Set-Content $manifestPath
Write-Detail "io.sentry.unity: $($manifest.dependencies.'io.sentry.unity')"

Write-Log "Copying Integration Test Files..."
New-Item -Path "$(GetNewProjectAssetsPath)" -Name "Scripts" -ItemType "directory" | Out-Null
Copy-Item -Recurse "$IntegrationScriptsPath/Scripts/*" -Destination "$(GetNewProjectAssetsPath)/Scripts/"
