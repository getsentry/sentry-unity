if (-not $Global:NewProjectPathCache)
{
    . ./test/Scripts.Integration.Test/globals.ps1
}

. ./test/Scripts.Integration.Test/common.ps1

# Check if SDK is packed.
$packageFile = "package-release.zip"
If (Test-Path -Path "$(ProjectRoot)/$packageFile" )
{
    Write-Log "Found $packageFile"
}
Else
{
    Throw "$packageFile on $(ProjectRoot) but it was not found. Be sure you run ./scripts/pack.ps1"
}

Write-Log -NoNewline "clearing $PackageReleaseOutput and Extracting $packageFile :"
if (Test-Path -Path "$PackageReleaseOutput")
{
    Remove-Item -Path "$PackageReleaseOutput" -Recurse
}

Expand-Archive -LiteralPath "$(ProjectRoot)/$packageFile" -DestinationPath "$PackageReleaseOutput"
Write-Log "OK"

If (-not(Test-Path -Path "$PackageReleaseOutput"))
{
    Throw "Path $PackageReleaseOutput does not exist. Be sure to run ./test/Scripts.Integration.Test/create-project."
}
