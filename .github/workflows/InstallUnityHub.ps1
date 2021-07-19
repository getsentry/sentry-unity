
Write-Host "Downloading Unity Hub"
Invoke-RestMethod -Uri https://public-cdn.cloud.unity3d.com/hub/prod/UnityHubSetup.exe -OutFile hub_installer.exe

Write-Host "Installing Unity Hub"
./hub_installer.exe /S