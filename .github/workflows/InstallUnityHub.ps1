
Write-Host "Downloading Unity Hub"
Invoke-RestMethod -Uri https://public-cdn.cloud.unity3d.com/hub/prod/UnityHubSetup.exe -OutFile hub_installer.exe

Write-Host "Installing Unity Hub"
./hub_installer.exe /S

$hubPath = &("C:\Program Files\Unity Hub\Unity Hub.exe")

Write-Host "Checking for $hubPath."

if (Test-Path -Path $hubPath) {
    Write-Host "Hub found"
    exit 0
}
else {
    Write-Host "Hub not found"
    exit 1
}