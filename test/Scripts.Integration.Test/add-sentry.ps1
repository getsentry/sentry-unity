param(
    [Parameter(Mandatory = $true)][string] $UnityPath,
    [Parameter(Mandatory = $true)][string] $PackagePath
)

if (-not $Global:NewProjectPathCache)
{
    . $PSScriptRoot/globals.ps1
}

. $PSScriptRoot/common.ps1

if (-not [System.IO.Path]::IsPathRooted($PackagePath))
{
    $PackagePath = "$(ProjectRoot)/$PackagePath"
}

# The path has to resolve for the editor, which in Docker sees the project root at /sentry-unity
If ($UnityPath.StartsWith("docker "))
{
    $PackagePath = $PackagePath.Replace("$(ProjectRoot)", "/sentry-unity")
}

# The next editor launch resolves and imports the package
Write-Log "Adding Sentry package to the project manifest..."
$manifestPath = "$(GetNewProjectPath)/Packages/manifest.json"
$manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json
$manifest.dependencies | Add-Member -NotePropertyName "io.sentry.unity" -NotePropertyValue "file:$PackagePath" -Force
$manifest | ConvertTo-Json -Depth 10 | Set-Content $manifestPath
Write-Detail "io.sentry.unity: $($manifest.dependencies.'io.sentry.unity')"

Write-Log "Copying Integration Test Files..."
New-Item -Path "$(GetNewProjectAssetsPath)" -Name "Scripts" -ItemType "directory" | Out-Null
Copy-Item -Recurse "$IntegrationScriptsPath/Scripts/*" -Destination "$(GetNewProjectAssetsPath)/Scripts/"
