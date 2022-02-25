param (
    [Parameter(Position = 0)]
    [string] $TestAppPath = "",

    [Parameter()]
    [string] $AppDataDir = ""
)
. ./test/Scripts.Integration.Test/IntegrationGlobals.ps1

if ("$TestAppPath" -eq "") {
    If ($IsMacOS) {
        $TestAppPath = "$NewProjectBuildPath/test.app/Contents/MacOS/$NewProjectName"
    }
    ElseIf ($IsWindows) {
        $TestAppPath = "$NewProjectBuildPath/test.exe"
        if ("$AppDataDir" -eq "") {
            $AppDataDir = "$env:UserProfile\AppData\LocalLow\DefaultCompany\$NewProjectName\"
        }
    }
    ElseIf ($IsLinux) {
        $TestAppPath = "$NewProjectBuildPath/test"
    }
    Else {
        Throw "Unsupported build"
    }
}

if ("$AppDataDir" -ne "") {
    Write-Warning "Removing AppDataDir '$AppDataDir'"
    Remove-Item  -Recurse $AppDataDir
}
else {
    Write-Warning "No AppDataDir param given - if you're running this after a previous smoke-crash test, the smoke test will fail."
    Write-Warning "You can provide AppDataDir and it will be deleted before running the test."
    Write-Warning 'On windows, this would normally be: -AppDataDir "$env:UserProfile\AppData\LocalLow\DefaultCompany\unity-of-bugs\"'
}

Set-Strictmode -Version latest

function RunTest([string] $type) {
    Write-Host "Running $TestAppPath --test $type"

    $process = Start-Process "$TestAppPath"  -ArgumentList "--test", $type -PassThru
    If ($null -eq $process) {
        Throw "Process not found."
    }

    # Wait for the test to finish
    $timedOut = $null # reset any previously set timeout
    $process | Wait-Process -Timeout 60  -ErrorAction SilentlyContinue -ErrorVariable timedOut

    if ("$AppDataDir" -ne "") {
        Write-Host "$type test: Player.log contents:" -ForegroundColor Yellow
        Get-Content "$AppDataDir/Player.log"
        Write-Host "================================================================================" -ForegroundColor Yellow
        Write-Host "$type test: Player.log contents END" -ForegroundColor Yellow
    }

    # ExitCode 200 is the status code indicating success inside SmokeTest.cs
    If ($process.ExitCode -eq 200) {
        Write-Host "$type test: PASSED" -ForegroundColor Green
    }
    ElseIf ($timedOut) {
        $process | Stop-Process
        Throw "Test process timed out."
    }
    Else {
        $info = "Test process finished with status code $($process.ExitCode)."
        If ($type -ne "smoke-crash") {
            if ("$AppDataDir" -ne "") {
                Get-Content "$AppDataDir/Player.log"
            }
            throw $info
        }
        Write-Host $info
    }
}

function RunApiServer() {
    $result = "" | Select-Object -Property process, outFile
    Write-Host "Starting the HTTP server (dummy API server)"
    $result.outFile = New-TemporaryFile
    $errFile = New-TemporaryFile

    $result.process = Start-Process "powershell" -ArgumentList "-command", "$PSScriptRoot/crash-test-server.ps1" -NoNewWindow -PassThru -RedirectStandardOutput $result.outFile -RedirectStandardError $errFile

    # The process shouldn't finish by itself, if it did, there was an error, so let's check that
    Start-Sleep -Second 1
    if ($result.process.HasExited) {
        Write-Host "Couldn't start the HTTP server" -ForegroundColor Red
        Write-Host "Standard Output:" -ForegroundColor Yellow
        Get-Content $result.outFile
        Write-Host "Standard Error:" -ForegroundColor Yellow
        Get-Content $errFile
        Remove-Item $result.outFile
        Remove-Item $errFile
        exit 1
    }

    return $result
}

# Simple smoke test
RunTest "smoke"

# Native crash test
$httpServer = RunApiServer
RunTest "smoke-crash"
$httpServer.process | Stop-Process -Force
$output = Get-Content $httpServer.outFile -Raw
Remove-Item $httpServer.outFile -ErrorAction Continue
Write-Host "Standard Output:" -ForegroundColor Yellow
$output

if ($output.Contains("POST http://localhost:8000/api/12345/minidump/")) {
    Write-Host "smoke-crash test: PASSED" -ForegroundColor Green
}
else {
    Write-Error "smoke-crash test: FAILED"
}