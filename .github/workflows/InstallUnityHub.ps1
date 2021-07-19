
Write-Host "Downloading Unity Hub"
Invoke-RestMethod -Uri ${{ env.UNITY_HUB_URL }} -OutFile hub_installer.exe

Write-Host "Installing Unity Hub"
./hub_installer.exe /S