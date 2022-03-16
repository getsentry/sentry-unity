param (
    [Parameter(Position = 0)]
    [string] $TestAppPath = "",

    [Parameter()]
    [string] $AppDataDir = "",

    [Parameter()]
    [switch] $Smoke,

    [Parameter()]
    [switch] $Crash
)
. $PSScriptRoot/IntegrationGlobals.ps1

Write-Host "Given parameters:"
Write-Host "  TestAppPath: $TestAppPath"
Write-Host "   AppDataDir: $AppDataDir"
Write-Host "        Smoke: $Smoke"
Write-Host "        Crash: $Crash"

If (!$Smoke -and !$Crash) {
    Write-Error "Select one of the following tests (or both): -Smoke or -Crash"
}

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
        Write-Error "Unsupported build"
    }
}

if ("$AppDataDir" -ne "") {
    if (Test-Path $AppDataDir) {
        Write-Warning "Removing AppDataDir '$AppDataDir'"
        Remove-Item  -Recurse $AppDataDir
    }
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
            throw $info
        }
        Write-Host $info
    }
}

function RunApiServer() {
    $result = "" | Select-Object -Property process, outFile, errFile
    Write-Host "Starting the HTTP server (dummy API server)"
    $result.outFile = New-TemporaryFile
    $result.errFile = New-TemporaryFile

    $result.process = Start-Process "python3" -ArgumentList "$PSScriptRoot/crash-test-server.py" -NoNewWindow -PassThru -RedirectStandardOutput $result.outFile -RedirectStandardError $result.errFile

    # The process shouldn't finish by itself, if it did, there was an error, so let's check that
    Start-Sleep -Second 1
    if ($result.process.HasExited) {
        Write-Host "Couldn't start the HTTP server" -ForegroundColor Red
        Write-Host "Standard Output:" -ForegroundColor Yellow
        Get-Content $result.outFile
        Write-Host "Standard Error:" -ForegroundColor Yellow
        Get-Content $result.errFile
        Remove-Item $result.outFile
        Remove-Item $result.errFile
        exit 1
    }

    return $result
}

# Simple smoke test
if ($Smoke) {
    RunTest "smoke"
}

# Native crash test
if ($Crash) {
    # You can increase this to retry multiple times. Seems a bit flaky at the moment in CI.
    $runs = 5
    for ($run = 1; $run -le $runs; $run++) {
        $httpServer = RunApiServer
        RunTest "smoke-crash"

        $httpServerUri = "http://localhost:8000"
        $successMessage = "POST /api/12345/minidump/"

        Write-Host "Waiting for the expected message to appear in the server output logs ..."
        # Wait for 30 seconds (300 * 100 milliseconds) until the expected message comes in
        for ($i = 0; $i -lt 300; $i++) {
            $output = (Get-Content $httpServer.outFile -Raw) + (Get-Content $httpServer.errFile -Raw)
            if ("$output".Contains($successMessage)) {
                break
            }
            Start-Sleep -Milliseconds 100
        }

        # Stop the HTTP server
        Write-Host "Stopping the dummy API server ... " -NoNewline
        try {
            (Invoke-WebRequest -URI "$httpServerUri/STOP").StatusDescription
        }
        catch {
            Write-Host "/STOP request failed, killing the server process"
            $httpServer.process | Stop-Process -Force -ErrorAction SilentlyContinue
        }
        $httpServer.process | Wait-Process -Timeout 10 -ErrorAction Continue

        Write-Host "Server stdout:" -ForegroundColor Yellow
        Get-Content $httpServer.outFile -Raw

        Write-Host "Server stderr:" -ForegroundColor Yellow
        Get-Content $httpServer.errFile -Raw

        $output = (Get-Content $httpServer.outFile -Raw) + (Get-Content $httpServer.errFile -Raw)
        Remove-Item $httpServer.outFile -ErrorAction Continue
        Remove-Item $httpServer.errFile -ErrorAction Continue

        if ($output.Contains($successMessage)) {
            Write-Host "smoke-crash test $run/$runs : PASSED" -ForegroundColor Green
            break
        }
        elseif ($run -ne $runs) {
            Write-Error "smoke-crash test $run/$runs : FAILED"
        }
    }

    RunTest "post-crash"
}
