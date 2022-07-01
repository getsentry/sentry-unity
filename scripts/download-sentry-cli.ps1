Set-StrictMode -Version latest

$conf = Get-Content "$PSScriptRoot/../modules/sentry-cli.properties" -Raw | ConvertFrom-StringData
$platforms = @('Darwin-universal', 'Linux-x86_64', 'Windows-x86_64')
$targetDir = "$PSScriptRoot/../package-dev/Editor/sentry-cli"
$baseUrl = "$($conf.repo)/releases/download/$($conf.version)/sentry-cli-"

if (Test-Path $targetDir)
{
    Remove-Item -r $targetDir
}
New-Item -Path $targetDir -ItemType Directory > $null

foreach ($name in $platforms)
{
    if ($name.StartsWith('Windows'))
    {
        $name += '.exe';
    }

    $targetFile = "$targetDir/sentry-cli-$name"
    Write-Host "Downloading $targetFile"
    Invoke-WebRequest -Uri "$baseUrl$name" -OutFile $targetFile

    if (Get-Command 'chmod' -ErrorAction SilentlyContinue)
    {
        chmod +x $targetFile
    }
}
