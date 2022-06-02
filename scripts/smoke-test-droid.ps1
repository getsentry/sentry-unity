param (
    [Parameter(Position = 0)]
    [Switch] $IsCI,
    [Switch] $IsIntegrationTest
)

Set-StrictMode -Version latest

# GITHUB_WORKSPACE is the root folder where the project is stored.
Write-Host "#################################################"
Write-Host "#   ANDROID                                     #"
Write-Host "#            VALIDATOR                          #"
Write-Host "#                       SCRIPT                  #"
Write-Host "#################################################"

# When launched from the CI android-emulator-runner the usual GH-actions' CI variable is missing.
# We set it here manually because CrashTestWithServer uses it to determine some configuration.
if ($IsCI)
{
    $env:CI = "true"
}


if ($IsIntegrationTest)
{
    Set-Variable -Name "ApkPath" -Value "samples/IntegrationTest/Build"
    Set-Variable -Name "ApkFileName" -Value "test.apk"
    Set-Variable -Name "ProcessName" -Value "com.DefaultCompany.IntegrationTest"
}
else
{

    Set-Variable -Name "ApkPath" -Value "samples/artifacts/builds/Android"
    Set-Variable -Name "ApkFileName" -Value "IL2CPP_Player.apk"
    Set-Variable -Name "ProcessName" -Value "io.sentry.samples.unityofbugs"
}
Set-Variable -Name "TestActivityName" -Value "$ProcessName/com.unity3d.player.UnityPlayerActivity"

. $PSScriptRoot/../test/Scripts.Integration.Test/common.ps1

function TakeScreenshot
{
    param ( $deviceId )
    $file = "/data/local/tmp/screen.png"
    adb -s $deviceId shell "screencap -p $file"
    adb pull $file "$ApkPath"
    adb shell "rm $file"
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

function OnError([string] $deviceId, [string] $deviceApi)
{
    Write-Host "Dumping logs for $device"
    adb -s $deviceId logcat -d
    Write-Host "UI XML Log"
    GetDeviceUiLog $device $deviceApi
    TakeScreenshot $device
}

function DateTimeNow
{
    return Get-Date -UFormat "%T %Z"
}

function CloseSystemAlert([string] $deviceId, [string] $deviceApi, [string] $alert)
{
    if ("$alert" -ne "")
    {
        $splittedXml = $alert -split "<node"
        $alertTitle = ""
        $alertOption1Label = $null
        $alertOption1Coord = $null
        $alertOption2Label = $null
        $alertOption2Coord = $null

        if ($splittedXml.Count -ne 1) {
            # We have a "valid" XML
            # Use Regex to get the message and the options labels + coordinates.
            foreach ($iterator in $splittedXml) {
                if ($iterator.Contains("alertTitle")) {
                    $titleRegex = [regex]::Match($iterator, "text=\`"(?<text>.+)\`" resource-id")
                    $alertTitle = $titleRegex.Groups["text"].Value
                }
                elseif ($iterator.Contains("Button")) {
                    $buttonRegex = [regex]::Match($iterator, "text=\`"(?<text>.+)\`" resource-id.* bounds=\`"\[(?<horStart>\d+),(?<verStart>\d+)\]\[(?<horEnd>\d+),(?<verEnd>\d+)\]\`"")
                    if ($null -eq $alertOption1Label) {
                        $alertOption1Label = $buttonRegex.Groups["text"].Value
                        $alertOption1Coord = ($buttonRegex.Groups["horStart"].Value, $buttonRegex.Groups["verStart"].Value, $buttonRegex.Groups["horEnd"].Value, $buttonRegex.Groups["verEnd"].Value)
                    }
                    else {
                        $alertOption2Label = $buttonRegex.Groups["text"].Value
                        $alertOption2Coord = ($buttonRegex.Groups["horStart"].Value, $buttonRegex.Groups["verStart"].Value, $buttonRegex.Groups["horEnd"].Value, $buttonRegex.Groups["verEnd"].Value)
                    }
                }
            }

            if ($null -ne $alertTitle) {
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
            # Fallback to the old method of closing Alerts.
            Write-Warning "Active system alert found on $deviceId (API $deviceApi). Closing it. The alert was: '$alert'."
            if ($deviceApi -eq "21")
            {
                Write-Warning "Issuing ENTER command twice to close the current window."
                # sends "enter" - the first one focues the OK button, the second one taps it
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

function SignalActionSmokeStatus
{
    param ($smokeStatus)
    echo "::set-output name=smoke-status::$smokeStatus"
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
    SignalActionSmokeStatus("Completed")
    Throw "It seems like no devices were found $RawAdbDeviceList"
}
Else
{
    Write-Host "Found $DeviceCount devices: $DeviceList"
}

# Check if APK was built.
If (-not (Test-Path -Path "$ApkPath/$ApkFileName" ))
{
    SignalActionSmokeStatus("Failed")
    Throw "Expected APK on $ApkPath/$ApkFileName but it was not found."
}

# Test
foreach ($device in $DeviceList)
{
    $deviceApi = "$(adb -s $device shell getprop ro.build.version.sdk)".Trim()
    $deviceSdk = "$(adb -s $device shell getprop ro.build.version.release)".Trim()
    Write-Host "`nChecking device $device with SDK '$deviceSdk' and API '$deviceApi'"

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
        $stdout = (adb -s $device install -r $ApkPath/$ApkFileName)

        if ($stdout.Contains("Broken pipe"))
        {
            Write-Warning "Failed to comunicate with the Device, retrying..."
            Start-Sleep 3
            $adbInstallRetry--
        }
    } while ($adbInstallRetry -gt 1 -and $stdout.Contains("Broken pipe"))

    If ($stdout -notcontains "Success")
    {
        SignalActionSmokeStatus("Failed")
        Throw "Failed to Install APK: $stdout."
    }

    function RunTest([string] $Name, [string] $SuccessString, [string] $FailureString)
    {
        $AppStarted = $false

        Write-Host "Clearing logcat from $device."
        adb -s $device logcat -c

        Write-Host "Starting Test '$Name'"

        adb -s $device shell am start -n $TestActivityName -e test $Name
        #despite calling start, the app might not be started yet.

        Write-Host (DateTimeNow)
        $Timeout = 45
        While ($Timeout -gt 0)
        {
            #Get a list of active processes
            $processIsRunning = (adb -s $device shell ps)
            #And filter by ProcessName
            $processIsRunning = $processIsRunning | Select-String $ProcessName

            If ($processIsRunning -eq $null -And $AppStarted)
            {
                $Timeout = -2
                break
            }
            ElseIf ($processIsRunning -ne $null -And !$AppStarted)
            {
                # Some devices might take a while to start the test, so we wait for the activity to start before checking if it was closed.
                $AppStarted = $true
            }
            Write-Host "Waiting Process on $device to complete, waiting $Timeout seconds"
            Start-Sleep -Seconds 1
            $Timeout--
            CheckAndCloseActiveSystemAlerts $device $deviceApi
        }

        if ("$SuccessString" -eq "")
        {
            $SuccessString = "$($Name.ToUpper()) TEST: PASS"
        }

        if ("$FailureString" -eq "")
        {
            $FailureString = "$($Name.ToUpper()) TEST: FAIL"
        }

        Write-Host (DateTimeNow)
        $LogcatCache = adb -s $device logcat -d
        $lineWithSuccess = $LogcatCache | Select-String $SuccessString
        $lineWithFailure = $LogcatCache | Select-String $FailureString

        if ($lineWithFailure -eq $null)
        {
            $lineWithFailure = $LogcatCache | Select-String "Error: Activity class .* does not exist."
        }

        If ($lineWithFailure -ne $null)
        {
            SignalActionSmokeStatus("Failed")
            Write-Warning "$name test failed"
            Write-Warning "$lineWithFailure"
            OnError $device $deviceApi
            throw "$Name test: FAIL"
        }
        elseif ($lineWithSuccess -ne $null)
        {
            Write-Host "$lineWithSuccess"
            Write-Host "$Name test: PASS" -ForegroundColor Green
        }
        ElseIf (($LogcatCache | Select-String 'CRASH   :'))
        {
            SignalActionSmokeStatus("Crashed")
            Write-Warning "$name test app has crashed."
            OnError $device $deviceApi
            Throw "$name test app has crashed."
        }
        ElseIf (($LogcatCache | Select-String 'Unity   : Timeout while trying detaching primary window.'))
        {
            SignalActionSmokeStatus("Flaky")
            Write-Warning "$name test was flaky, unity failed to initialize."
            OnError $device $deviceApi
            Throw "$name test was flaky, unity failed to initialize."
        }
        ElseIf ($Timeout -le 0)
        {
            SignalActionSmokeStatus("Timeout")
            Write-Warning "$name Test Timeout, see Logcat info for more information below."
            Write-Host "PS info."
            adb -s $device shell ps
            OnError $device $deviceApi
            Throw "$name test Timeout"
        }
        Else
        {
            SignalActionSmokeStatus("Failed")
            Write-Warning "$name test: process completed but $Name test was not signaled."
            OnError $device $deviceApi
            Throw "$Name test Failed."
        }
    }

    RunTest -Name "smoke"
    RunTest -Name "hasnt-crashed"

    try
    {
        # Note: mobile apps post the crash on the second app launch, so we must run both as part of the "CrashTestWithServer"
        CrashTestWithServer -SuccessString "POST /api/12345/envelope/ HTTP/1.1`" 200 -b'1f8b08000000000000" -CrashTestCallback {
            RunTest -Name "crash" -SuccessString "CRASH TEST: Issuing a native crash" -FailureString "CRASH TEST: FAIL"
            RunTest -Name "has-crashed"
        }
    }
    catch
    {
        SignalActionSmokeStatus("Failed");
        OnError $device $deviceApi
        throw;
    }
}

SignalActionSmokeStatus("Completed")
Write-Host "Tests completed successfully." -ForegroundColor Green