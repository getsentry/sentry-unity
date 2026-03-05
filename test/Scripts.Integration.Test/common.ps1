# Note: this is currently used by integration test scripts as well as "smoke-test-*.ps1" scripts.
# If/when those are merged to some extent, maybe this file could be merged into `globals.ps1`.

Set-StrictMode -Version latest
$ErrorActionPreference = "Stop"

function Write-Log
{
    param(
        [Parameter(Mandatory = $true, ValueFromPipeline = $true)]
        [string]$Message,
        [string]$ForegroundColor = "White",
        [switch]$NoNewline
    )

    $timestamp = Get-Date -Format "HH:mm:ss.fff"
    $output = "$timestamp | $Message"

    if ($NoNewline)
    {
        Write-Host $output -ForegroundColor $ForegroundColor -NoNewline
    }
    else
    {
        Write-Host $output -ForegroundColor $ForegroundColor
    }
}

function Write-Detail
{
    param(
        [Parameter(Mandatory = $true)]
        [string]$Message
    )

    $timestamp = Get-Date -Format "HH:mm:ss.fff"
    Write-Host "$timestamp |   $Message" -ForegroundColor Gray
}

function Write-PhaseHeader
{
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name
    )

    $line = "=" * 64
    Write-Host ""
    Write-Host $line -ForegroundColor Cyan
    Write-Host "  $($Name.ToUpper())" -ForegroundColor Cyan
    Write-Host $line -ForegroundColor Cyan
}

function Write-PhaseSuccess
{
    param(
        [Parameter(Mandatory = $true)]
        [string]$Message
    )

    $timestamp = Get-Date -Format "HH:mm:ss.fff"
    Write-Host "$timestamp | [OK] $Message" -ForegroundColor Green
}

function Write-PhaseFailed
{
    param(
        [Parameter(Mandatory = $true)]
        [string]$Message
    )

    $timestamp = Get-Date -Format "HH:mm:ss.fff"
    Write-Host "$timestamp | [FAILED] $Message" -ForegroundColor Red
}

function RunUnityAndExpect([string] $UnityPath, [string] $name, [string] $successMessage, [string[]] $arguments)
{
    $stdout = RunUnityCustom $UnityPath $arguments -ReturnLogOutput
    $lineWithSuccess = $stdout | Select-String $successMessage
    If ($null -ne $lineWithSuccess)
    {
        Write-Log "`n$name | SUCCESS because the following text was found: '$lineWithSuccess'" -ForegroundColor Green
    }
    Else
    {
        Write-Error "$name | Unity exited without an error but the successMessage was not found in the output ('$successMessage')"
    }
}

function MakeExecutable([string] $file)
{
    If ((Test-Path -Path $file) -and (Get-Command 'chmod' -ErrorAction SilentlyContinue))
    {
        Write-Log -NoNewline "Fixing permission for $file : "
        chmod +x $file
        Write-Log "OK" -ForegroundColor Green
    }
}
