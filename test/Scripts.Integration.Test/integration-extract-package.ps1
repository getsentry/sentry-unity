. ./test/Scripts.Integration.Test/IntegrationGlobals.ps1

# Check if SDK is packed.
$packageFile = "package-release.zip"
If (Test-Path -Path "$(ProjectRoot)/$packageFile" )
{
    Write-Host "Found $packageFile"
}
Else
{
    Throw "$packageFile on $(ProjectRoot) but it was not found. Be sure you run ./scripts/pack.ps1"
}

Write-Host -NoNewline "clearing $PackageReleaseOutput and Extracting $packageFile :"
if (Test-Path -Path "$PackageReleaseOutput")
{
    Remove-Item -Path "$PackageReleaseOutput" -Recurse
}

Expand-Archive -LiteralPath "$(ProjectRoot)/$packageFile" -DestinationPath "$PackageReleaseOutput"
Write-Host "OK"

If (-not(Test-Path -Path "$PackageReleaseOutput"))
{
    Throw "Path $PackageReleaseOutput does not exist. Be sure to run ./test/Scripts.Integration.Test/integration-create-project."
}
