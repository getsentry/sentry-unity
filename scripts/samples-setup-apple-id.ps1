Set-StrictMode -Version latest
$ErrorActionPreference = "Stop"

Write-Output "Setting up Apple Developer Team ID from environment variable"

$appleId = $Env:APPLE_ID

if (-not $appleId)
{
    # Check if running on macOS
    if ($IsOSX -or $IsMacOS -or $PSVersionTable.OS -like "*Darwin*")
    {
        Write-Output "APPLE_ID not set, attempting to detect from macOS system..."
        
        try {
            # Try to get Team ID from code signing identities
            $codesigningOutput = & xcrun security find-identity -v -p codesigning 2>$null
            if ($codesigningOutput) {
                # Extract Team ID from output using regex pattern matching
                $match = [regex]::Match($codesigningOutput, '\(([A-Z0-9]{10})\)')
                if ($match.Success) {
                    $appleId = $match.Groups[1].Value
                    Write-Output "Found Apple Developer Team ID: $appleId from code signing identity"
                }
            }
            
            # If first method failed, try another approach with certificates
            if (-not $appleId) {
                $certOutput = & security find-certificate -a -c "Apple Development" -p 2>$null | grep "OU=" 
                if ($certOutput) {
                    $match = [regex]::Match($certOutput, 'OU=([A-Z0-9]{10})')
                    if ($match.Success) {
                        $appleId = $match.Groups[1].Value
                        Write-Output "Found Apple Developer Team ID: $appleId from certificate"
                    }
                }
            }
        }
        catch {
            Write-Output "Error attempting to detect Apple Developer Team ID: $_"
        }
    }
    
    if (-not $appleId)
    {
        Write-Error "APPLE_ID environment variable is not set and couldn't be detected automatically. Skipping..."
        exit
    }
}

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
