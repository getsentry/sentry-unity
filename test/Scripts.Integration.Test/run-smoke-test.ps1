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

if (-not $Global:NewProjectPathCache)
{
    . ./test/Scripts.Integration.Test/globals.ps1
}

. $PSScriptRoot/common.ps1

Write-Host "Given parameters:"
Write-Host "  TestAppPath: $TestAppPath"
Write-Host "   AppDataDir: $AppDataDir"
Write-Host "        Smoke: $Smoke"
Write-Host "        Crash: $Crash"

If (!$Smoke -and !$Crash)
{
    Write-Error "Select one of the following tests (or both): -Smoke or -Crash"
}

if ("$TestAppPath" -eq "")
{
    If ($IsMacOS)
    {
        $TestAppPath = "$(GetNewProjectBuildPath)/test.app/Contents/MacOS/$(GetNewProjectName)"
        if ("$AppDataDir" -eq "")
        {
            $AppDataDir = "$env:HOME/Library/Logs/DefaultCompany/$(GetNewProjectName)/"
        }
    }
    ElseIf ($IsWindows)
    {
        $TestAppPath = "$(GetNewProjectBuildPath)/test.exe"
        if ("$AppDataDir" -eq "")
        {
            $AppDataDir = "$env:UserProfile\AppData\LocalLow\DefaultCompany\$(GetNewProjectName)\"
        }
    }
    ElseIf ($IsLinux)
    {
        $TestAppPath = "$(GetNewProjectBuildPath)/test"
        chmod +x $TestAppPath
        if ("$AppDataDir" -eq "")
        {
            $AppDataDir = "$env:HOME/.config/unity3d/DefaultCompany/$(GetNewProjectName)/"
        }
    }
    Else
    {
        Write-Error "Unsupported build"
    }
}

Write-Host "Resolved parameters:"
Write-Host "  TestAppPath: $TestAppPath"
Write-Host "   AppDataDir: $AppDataDir"

if ("$AppDataDir" -ne "")
{
    if (Test-Path $AppDataDir)
    {
        Write-Warning "Removing AppDataDir '$AppDataDir'"
        Remove-Item -Force -Recurse $AppDataDir
    }
}
else
{
    Write-Warning "No AppDataDir param given - if you're running this after a previous crash test, the smoke test will fail."
    Write-Warning "You can provide AppDataDir and it will be deleted before running the test."
    Write-Warning 'On windows, this would normally be: -AppDataDir "$env:UserProfile\AppData\LocalLow\DefaultCompany\unity-of-bugs\"'
}

function RunTest([string] $type)
{
    Write-Host "::group::Test: '$type'"
    try
    {
        if ($IsLinux -and "$env:XDG_CURRENT_DESKTOP" -eq "" -and (Get-Command "xvfb-run" -ErrorAction SilentlyContinue))
        {
            Write-Host "Running xvfb-run -ae /dev/stdout $TestAppPath --test $type"
            $process = Start-Process "xvfb-run" -ArgumentList "-ae", "/dev/stdout", "$TestAppPath", "--test", $type -PassThru
        }
        else
        {
            Write-Host "Running $TestAppPath --test $type"
            $process = Start-Process "$TestAppPath" -ArgumentList "--test", $type -PassThru
        }

        If ($null -eq $process)
        {
            Throw "Process not found."
        }

        # Wait for the test to finish
        $timedOut = $null # reset any previously set timeout
        $process | Wait-Process -Timeout 180 -ErrorAction SilentlyContinue -ErrorVariable timedOut

        $appLog = ""
        if ("$AppDataDir" -ne "")
        {
            Write-Host "$type test: Player.log contents:" -ForegroundColor Yellow
            $appLog = Get-Content "$AppDataDir/Player.log"
            $appLog
            Write-Host "================================================================================" -ForegroundColor Yellow
            Write-Host "$type test: Player.log contents END" -ForegroundColor Yellow
        }

        # Relying on ExitCode does not seem reliable. We're looking for the line "SmokeTester is quitting." instead to indicate 
        # a successful shut-down.
        If ($appLog | Select-String "SmokeTester is quitting.")
        {
            Write-Host "$type test: PASSED" -ForegroundColor Green
        }
        ElseIf ($timedOut)
        {
            $process | Stop-Process
            Throw "Test process timed out."
        }
        Else
        {
            $lineWithFailure = $appLog | Select-String "$($type.ToUpper()) TEST: FAIL"
            $info = "Test process finished with status code $($process.ExitCode). $lineWithFailure"
            If ($type -ne "crash")
            {
                throw $info
            }
            Write-Host $info
        }
    }
    finally
    {
        Write-Host "::endgroup::"
    }
}

# Simple smoke test
if ($Smoke)
{
    RunTest "smoke"
    RunTest "hasnt-crashed"
}

# Native crash test
if ($Crash)
{
    # Note: macOS & Linux apps post the crash on the next app launch so we must run both as part of the "CrashTestWithServer"
    #       Windows posts the crash immediately because the handler runs as a standalone process.
    if ($IsMacOS -or $IsLinux)
    {
        $expectedFragment = $IsMacOS ? '1f8b08000000000000' : '7b2264736e223a2268'
        CrashTestWithServer -SuccessString "POST /api/12345/envelope/ HTTP/1.1`" 200 -b'$expectedFragment" -CrashTestCallback {
            RunTest "crash" "CRASH TEST: Issuing a native crash"
            RunTest "has-crashed"
        }
    }
    else
    {
        CrashTestWithServer -SuccessString "POST /api/12345/minidump/" -CrashTestCallback { RunTest "crash" }
        RunTest "has-crashed"
    }
}
