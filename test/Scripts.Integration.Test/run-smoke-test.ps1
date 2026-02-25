param (
    [Parameter(Position = 0)]
    [string] $TestAppPath = "",

    [Parameter()]
    [string] $AppDataDir = "",

    [Parameter()]
    [switch] $Smoke,

    [Parameter()]
    [switch] $Crash,

    [Parameter()]
    [int] $MaxRetries = 3
)

if (-not $Global:NewProjectPathCache)
{
    . $PSScriptRoot/globals.ps1
}

. $PSScriptRoot/common.ps1

Write-Log "Given parameters:"
Write-Log "  TestAppPath: $TestAppPath"
Write-Log "   AppDataDir: $AppDataDir"
Write-Log "        Smoke: $Smoke"
Write-Log "        Crash: $Crash"
Write-Log "   MaxRetries: $MaxRetries"

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

Write-Log "Resolved parameters:"
Write-Log "  TestAppPath: $TestAppPath"
Write-Log "   AppDataDir: $AppDataDir"

function RunTest([string] $type)
{
    Write-Host "::group::Test: '$type'"
    try
    {
        if ($IsLinux -and "$env:XDG_CURRENT_DESKTOP" -eq "" -and (Get-Command "xvfb-run" -ErrorAction SilentlyContinue))
        {
            Write-Log "Running xvfb-run -ae /dev/stdout $TestAppPath --test $type"
            $process = Start-Process "xvfb-run" -ArgumentList "-ae", "/dev/stdout", "$TestAppPath", "--test", $type -PassThru
        }
        else
        {
            Write-Log "Running $TestAppPath --test $type"
            $process = Start-Process "$TestAppPath" -ArgumentList "--test", $type -PassThru
        }

        If ($null -eq $process)
        {
            Throw "Process not found."
        }

        # Wait for the test to finish
        $timedOut = $null # reset any previously set timeout
        $process | Wait-Process -Timeout 60 -ErrorAction SilentlyContinue -ErrorVariable timedOut

        $appLog = ""
        if ("$AppDataDir" -ne "")
        {
            Write-Log "$type test: Player.log contents:" -ForegroundColor Yellow
            $appLog = Get-Content "$AppDataDir/Player.log"
            $appLog
            Write-Log "================================================================================" -ForegroundColor Yellow
            Write-Log "$type test: Player.log contents END" -ForegroundColor Yellow
        }

        # Check for test failures first - a graceful shutdown doesn't mean tests passed.
        $lineWithFailure = $appLog | Select-String "$($type.ToUpper()) TEST: FAIL"
        If ($lineWithFailure)
        {
            $info = "Test process finished with status code $($process.ExitCode). $lineWithFailure"
            If ($type -ne "crash")
            {
                throw $info
            }
            Write-Log $info
        }
        # Relying on ExitCode does not seem reliable. We're looking for the line "SmokeTester is quitting." instead to indicate
        # a successful shut-down.
        ElseIf ($appLog | Select-String "SmokeTester is quitting.")
        {
            Write-Log "$type test: PASSED" -ForegroundColor Green
        }
        ElseIf ($timedOut)
        {
            $process | Stop-Process -Force
            Throw "Test process timed out."
        }
        Else
        {
            $info = "Test process finished with status code $($process.ExitCode). No completion marker found in Player.log"
            If ($type -ne "crash")
            {
                throw $info
            }
            Write-Log $info
        }
    }
    finally
    {
        if ($null -ne $process -and !$process.HasExited)
        {
            Write-Warning "Process still running - forcing termination."
            $process | Stop-Process -Force
        }
        Write-Host "::endgroup::"
    }
}

for ($attempt = 1; $attempt -le $MaxRetries; $attempt++)
{
    Write-Log "Test suite attempt $attempt/$MaxRetries"

    if ("$AppDataDir" -ne "" -and (Test-Path $AppDataDir))
    {
        Write-Warning "Removing AppDataDir '$AppDataDir'"
        Remove-Item -Force -Recurse $AppDataDir
    }

    try
    {
        if ($Smoke)
        {
            RunTest "smoke"
            RunTest "hasnt-crashed"
        }

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

        Write-Log "All tests passed on attempt $attempt/$MaxRetries"
        return
    }
    catch
    {
        Write-Log "Test suite attempt $attempt failed: $_"
        if ($attempt -lt $MaxRetries)
        {
            Write-Log "Will retry..."
            continue
        }
        throw
    }
}
