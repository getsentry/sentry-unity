
Write-Host "Downloading Unity Hub"
Invoke-RestMethod -Uri https://public-cdn.cloud.unity3d.com/hub/prod/UnityHubSetup.exe -OutFile hub_installer.exe

Write-Host "Installing Unity Hub"
./hub_installer.exe /S

$hubPath = C:\Program` Files\Unity` Hub\Unity` Hub.exe

if(-not(Test-Path -Path $hubPath -PathType Leaf)) {
    Write-Host "Hub found"
}
else {
    Write-Host "Hub not found"
}