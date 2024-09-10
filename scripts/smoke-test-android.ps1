param (
    [Parameter(Position = 0)]
    [string] $Action = "",
    [Switch] $IsIntegrationTest,
    [Switch] $WarnIfFlaky,
    [string] $UnityVersion = ""
)

if (-not $Global:NewProjectPathCache)
{
    . ./test/Scripts.Integration.Test/globals.ps1
}

. $PSScriptRoot/../test/Scripts.Integration.Test/common.ps1

# GITHUB_WORKSPACE is the root folder where the project is stored.
Write-Host "#################################################"
Write-Host "#   ANDROID                                     #"
Write-Host "#            VALIDATOR                          #"
Write-Host "#                       SCRIPT                  #"
Write-Host "#################################################"

if ($IsIntegrationTest)
{
    $BuildDir = $(GetNewProjectBuildPath)
    $ApkFileName = "test.apk"
    $ProcessName = "com.DefaultCompany.IntegrationTest"

    if ($Action -eq "Build")
    {
        $buildCallback = {
            Write-Host "::group::Gradle build $BuildDir"
            Push-Location $BuildDir
            try
            {
                MakeExecutable "./gradlew"
                & ./gradlew --info --no-daemon assembleRelease | ForEach-Object {
                    Write-Host "  $_"
                }
                if (-not $?)
                {
                    throw "Gradle execution failed"
                }
                Copy-Item -Path launcher/build/outputs/apk/release/launcher-release.apk -Destination $ApkFileName
            }
            finally
            {
                Pop-Location
                Write-Host "::endgroup::"
            }
        }

        $symbolServerOutput = RunWithSymbolServer -Callback $buildCallback
        CheckSymbolServerOutput 'Android' $symbolServerOutput $UnityVersion
        return;
    }
}
else
{
    $BuildDir = "samples/artifacts/builds/Android"
    $ApkFileName = "IL2CPP_Player.apk"
    $ProcessName = "io.sentry.samples.unityofbugs"
}
$TestActivityName = "$ProcessName/com.unity3d.player.UnityPlayerActivity"

$_ArtifactsPath = ((Test-Path env:ARTIFACTS_PATH) ? $env:ARTIFACTS_PATH : "./$BuildDir/../test-artifacts/") `
    + $(Get-Date -Format "HHmmss")
function ArtifactsPath
{
    if (-not (Test-Path $_ArtifactsPath))
    {
        New-Item $_ArtifactsPath -ItemType Directory | Out-Null
    }
    $_ArtifactsPath.Replace('\', '/')
}

if (Test-Path env:CI)
{
    # Take Screenshot of VM to verify emulator start
    screencapture "$(ArtifactsPath)/host-screenshot.jpg"
}

function TakeScreenshot([string] $deviceId)
{
    $file = "/data/local/tmp/screen$(Get-Date -Format "HHmmss").png"
    adb -s $deviceId shell "screencap -p $file"
    adb -s $deviceId pull $file (ArtifactsPath)
    adb -s $deviceId shell "rm $file"
}

function GetDeviceUiLog([string] $deviceId, [string] $deviceApi)
{
    if ($deviceApi -eq "21")
    {
        $dumpFile = "/data/local/tmp/window_dump.xml"
        adb -s $deviceId exec-out uiautomator dump $dumpFile
        adb -s $deviceId shell cat $dumpFile
    }
    else
    {
        adb -s $deviceId exec-out uiautomator dump /dev/tty
    }
}

function LogCat([string] $deviceId, [string] $appPID)
{
    if ([string]::IsNullOrEmpty($appPID))
    {
        adb -s $device logcat -d
    }
    elseif ($deviceApi -eq "21")
    {
        adb -s $device shell "logcat -d | grep -E '\( *$appPID\)'"
    }
    else
    {
        adb -s $device logcat -d --pid=$appPID
    }
}

function PidOf([string] $deviceId, [string] $processName)
{
    if ($deviceApi -eq "21")
    {
        # `pidof` doesn't exist - take second column from the `ps` output for the given process instead.
        (adb -s $deviceId shell "ps | grep '$processName'") -Split " +" | Select-Object -Skip 1 -First 1
    }
    else
    {
        adb -s $deviceId shell pidof $processName
    }
}

function OnError([string] $deviceId, [string] $deviceApi, [string] $appPID)
{
    Write-Host "Dumping logs for $device"
    Write-Host "::group::logcat"
    LogCat $deviceId $appPID
    Write-Host "::endgroup::"
    LogCat $deviceId $null | Out-File "$(ArtifactsPath)/logcat.txt"
    Write-Host "::group::UI XML Log"
    GetDeviceUiLog $device $deviceApi
    Write-Host "::endgroup::"
    TakeScreenshot $device
}


function CloseSystemAlert([string] $deviceId, [string] $deviceApi, [string] $alert)
{
    if ("$alert" -ne "")
    {
        $splitXml = $alert -split "<node"
        $alertTitle = ""
        $alertOption1Label = $null
        $alertOption1Coord = $null
        $alertOption2Label = $null
        $alertOption2Coord = $null

        if ($splitXml.Count -ne 1)
        {
            # We have a "valid" XML
            # Use Regex to get the message and the options labels + coordinates.
            foreach ($iterator in $splitXml)
            {
                if ($iterator.Contains("alertTitle"))
                {
                    $titleRegex = [regex]::Match($iterator, "text=\`"(?<text>.+)\`" resource-id")
                    $alertTitle = $titleRegex.Groups["text"].Value
                }
                elseif ($iterator.Contains("Button"))
                {
                    $buttonRegex = [regex]::Match($iterator, "text=\`"(?<text>.+)\`" resource-id.* bounds=\`"\[(?<horStart>\d+),(?<verStart>\d+)\]\[(?<horEnd>\d+),(?<verEnd>\d+)\]\`"")
                    if ($null -eq $alertOption1Label)
                    {
                        $alertOption1Label = $buttonRegex.Groups["text"].Value
                        $alertOption1Coord = ($buttonRegex.Groups["horStart"].Value, $buttonRegex.Groups["verStart"].Value, $buttonRegex.Groups["horEnd"].Value, $buttonRegex.Groups["verEnd"].Value)
                    }
                    else
                    {
                        $alertOption2Label = $buttonRegex.Groups["text"].Value
                        $alertOption2Coord = ($buttonRegex.Groups["horStart"].Value, $buttonRegex.Groups["verStart"].Value, $buttonRegex.Groups["horEnd"].Value, $buttonRegex.Groups["verEnd"].Value)
                    }
                }
            }

            if ($null -ne $alertTitle)
            {
                Write-Warning "Found Alert on Screen, TITLE: $alertTitle `n Options: `n $alertOption1Label at $alertOption1Coord `n $alertOption2Label at $alertOption2Coord "

                if ($null -eq $alertOption2Label)
                {
                    $tapX = [int]([int]$alertOption1Coord[0] + [int]$alertOption1Coord[2] ) / 2
                    $tapY = [int]([int]$alertOption1Coord[1] + [int]$alertOption1Coord[3] ) / 2
                    $tapLabel = $alertOption1Label
                }
                else
                {
                    $tapX = [int]([int]$alertOption2Coord[0] + [int]$alertOption2Coord[2] ) / 2
                    $tapY = [int]([int]$alertOption2Coord[1] + [int]$alertOption2Coord[3] ) / 2
                    $tapLabel = $alertOption2Label
                }
                Write-Host "Tapping on $tapLabel at [$tapX, $tapY]"
                adb -s $deviceId shell input tap $tapX $tapY
            }
        }
        else
        {
            # Fallback to the old method of closing Alerts. (Android API 21 to 27)
            Write-Warning "Active system alert found on $deviceId (API $deviceApi). Closing it. The alert was: '$alert'."
            if ($deviceApi -eq "21")
            {
                Write-Warning "Issuing ENTER command twice to close the current window."
                # sends "enter" - the first one focus the OK button, the second one taps it
                adb -s $deviceId shell input keyevent 66
                adb -s $deviceId shell input keyevent 66
            }
            else
            {
                # sends "back" action
                Write-Warning "Issuing BACK command to close the current window."
                adb -s $deviceId shell input keyevent 4
            }
        }
    }
}

function CheckAndCloseActiveSystemAlerts([string] $deviceId, [string] $deviceApi)
{
    $uiInfoXml = GetDeviceUiLog $deviceId $deviceApi
    if ($deviceApi -eq "21")
    {
        CloseSystemAlert $deviceId $deviceApi ($uiInfoXml | Select-String "has stopped")
    }
    else
    {
        CloseSystemAlert $deviceId $deviceApi ($uiInfoXml | Select-String "android:id/alertTitle|has stopped|Close app")
    }
}

function ExitNow([string] $status, [string] $message)
{
    if (Test-Path env:GITHUB_OUTPUT)
    {
        Write-Host "Writing 'status=$status' to env:GITHUB_OUTPUT: ${env:GITHUB_OUTPUT}"
        "status=$status" >> $env:GITHUB_OUTPUT
    }
    else
    {
        Write-Host "status=$status"
    }

    if ($status -ieq "success")
    {
        Write-Host $message -ForegroundColor Green
    }
    elseif ($status -ieq "flaky" -and $WarnIfFlaky)
    {
        Write-Warning $message
    }
    else
    {
        Write-Error $message
        exit 1 # just in case error handling is overriden
    }
    exit 0
}

# Filter device List
$RawAdbDeviceList = adb devices

$DeviceList = @()
foreach ($device in $RawAdbDeviceList)
{
    If ($device.EndsWith("device"))
    {
        $DeviceList += $device.Replace("device", '').Trim()
    }
}
$DeviceCount = $DeviceList.Count

If ($DeviceCount -eq 0)
{
    ExitNow "failed" "It seems like no devices were found $RawAdbDeviceList"
}
Else
{
    Write-Host "Found $DeviceCount devices: $DeviceList"
}

# Check if APK was built.
If (-not (Test-Path -Path "$BuildDir/$ApkFileName" ))
{
    ExitNow "failed" "Expected APK on $BuildDir/$ApkFileName but it was not found."
}

# Test
foreach ($device in $DeviceList)
{
    adb -s $device logcat -c

    $deviceApi = "$(adb -s $device shell getprop ro.build.version.sdk)".Trim()
    $deviceSdk = "$(adb -s $device shell getprop ro.build.version.release)".Trim()
    Write-Host "`nChecking device $device with SDK '$deviceSdk' and API '$deviceApi'"

    if (Test-Path env:CI)
    {
        # Take Screenshot of the device to verify emulator start
        TakeScreenshot $device
    }

    $stdout = adb -s $device shell "pm list packages -f"
    if ($null -ne ($stdout | Select-String $ProcessName))
    {
        Write-Host "Removing previous APP."
        $stdout = adb -s $device uninstall $ProcessName
    }

    # Move device to home screen
    $stdout = adb -s $device shell input keyevent KEYCODE_HOME

    $adbInstallRetry = 5
    do
    {
        Write-Host "Installing test app..."
        $stdout = (adb -s $device install -r $BuildDir/$ApkFileName 2>&1)

        if ($stdout.Contains("Broken pipe"))
        {
            Write-Warning "Failed to comunicate with the Device, retrying..."
            Start-Sleep 3
            $adbInstallRetry--
        }
    } while ($adbInstallRetry -gt 1 -and $stdout.Contains("Broken pipe"))

    If ($stdout -notcontains "Success")
    {
        OnError $device $deviceApi
        ExitNow "failed" "Failed to Install APK: $stdout."
    }

    function RunTest([string] $Name, [string] $SuccessString, [string] $FailureString, [switch] $PreserveLogcat)
    {
        Write-Host "::group::Test: '$name'"

        if (!$PreserveLogcat)
        {
            Write-Host "Clearing logcat from $device."
            adb -s $device logcat -c
        }

        adb -s $device shell am start -n $TestActivityName -e test $Name
        #despite calling start, the app might not be started yet.

        $timedOut = $true
        $appPID = $null
        $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
        While ($stopwatch.Elapsed.TotalSeconds -lt 60)
        {
            # Check if the app started - it's not absolutely necessary to get the PID, just useful to achieve good log output.
            if ($null -eq $appPID)
            {
                $appPID = PidOf $device $ProcessName
                if ($null -eq $appPID)
                {
                    if ($stopwatch.Elapsed.TotalSeconds % 10 -eq 0)
                    {
                        Write-Host "Waiting Process on $device to start, time elapsed already: $($stopwatch.Elapsed.ToString('hh\:mm\:ss\.fff'))"
                    }
                    # No sleep here or we may miss the start. While it's not critical, it's useful to get the PID.
                    continue
                }
            }

            $isRunning = $null -ne (PidOf $device $ProcessName)
            If ($isRunning)
            {
                Write-Host "Waiting Process $appPID on $device to complete, time elapsed already: $($stopwatch.Elapsed.ToString('hh\:mm\:ss\.fff'))"
                Start-Sleep -Seconds 1
                CheckAndCloseActiveSystemAlerts $device $deviceApi
            }
            else
            {
                $timedOut = $false
                break
            }
        }

        if ("$SuccessString" -eq "")
        {
            $SuccessString = "$($Name.ToUpper()) TEST: PASS"
        }

        if ("$FailureString" -eq "")
        {
            $FailureString = "$($Name.ToUpper()) TEST: FAIL"
        }

        $LogcatCache = LogCat $device $appPID
        $lineWithSuccess = $LogcatCache | Select-String $SuccessString
        $lineWithFailure = $LogcatCache | Select-String $FailureString

        if ($lineWithFailure -eq $null)
        {
            $lineWithFailure = $LogcatCache | Select-String "Error: Activity class .* does not exist."
        }

        If ($lineWithFailure -ne $null)
        {
            Write-Host "::endgroup::"
            OnError $device $deviceApi $appPID
            ExitNow "failed" "$Name test: FAIL - $lineWithFailure"
        }
        elseif ($lineWithSuccess -ne $null)
        {
            Write-Host "$lineWithSuccess"
            Write-Host "$Name test: PASS" -ForegroundColor Green
            Write-Host "::endgroup::"
        }
        ElseIf (($LogcatCache | Select-String 'CRASH   :'))
        {
            Write-Host "::endgroup::"
            OnError $device $deviceApi $appPID
            ExitNow "crashed" "$name test app has crashed."
        }
        ElseIf (($LogcatCache | Select-String 'Unity   : Timeout while trying detaching primary window.'))
        {
            Write-Host "::endgroup::"
            OnError $device $deviceApi $appPID
            ExitNow "flaky" "$name test was flaky, unity failed to initialize."
        }
        ElseIf ($timedOut)
        {
            Write-Host "::endgroup::"
            Write-Host "::group::Processes running on device"
            adb -s $device shell ps
            Write-Host "::endgroup::"
            OnError $device $deviceApi $appPID
            ExitNow "timeout" "$name test Timeout, see Logcat info for more info."
        }
        Else
        {
            Write-Host "::endgroup::"
            OnError $device $deviceApi $appPID
            ExitNow "failed" "$name test: failed - process completed but $Name test was not signaled."
        }
    }

    RunTest -Name "smoke"
    RunTest -Name "hasnt-crashed"

    try
    {
        # Note: mobile apps post the crash on the second app launch, so we must run both as part of the "CrashTestWithServer"
        CrashTestWithServer -SuccessString "POST /api/12345/envelope/ HTTP/1.1`" 200 -b'1f8b08000000000000" -CrashTestCallback {
            RunTest -Name "crash" -SuccessString "CRASH TEST: Issuing a native crash" -FailureString "CRASH TEST: FAIL"
            RunTest -Name "has-crashed" -PreserveLogcat
        }
    }
    catch
    {
        Write-Warning "Caught exception: $_"
        Write-Host $_.ScriptStackTrace
        OnError $device $deviceApi
        ExitNow "failed" $_;
    }
}

ExitNow "success" "Tests completed successfully."
