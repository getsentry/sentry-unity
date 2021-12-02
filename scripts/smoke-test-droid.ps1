# GITHUB_WORKSPACE is the root folder where the project is stored.
Write-Output "#################################################"
Write-Output "#   ANDROID                                     #"
Write-Output "#            VALIDATOR                          #"
Write-Output "#                       SCRIPT                  #"
Write-Output "#################################################"

Set-Variable -Name "ApkPath" -Value "samples/artifacts/builds/Android"
Set-Variable -Name "ApkFileName" -Value "IL2CPP_Player.apk"
Set-Variable -Name "ProcessName" -Value "io.sentry.samples.unityofbugs"
Set-Variable -Name "TestActivityName" -Value "io.sentry.samples.unityofbugs/com.unity3d.player.UnityPlayerActivity"
$LogcatCache = $null

function TakeScreenshot {
    param ( $deviceId )
    adb -s $deviceId shell "screencap -p /storage/emulated/0/screen.png"
    adb pull "/storage/emulated/0/screen.png" "$ApkPath"
    adb shell "rm /storage/emulated/0/screen.png" 
}

function WriteDeviceLog {
    param ( $deviceId ) 
    Write-Output $LogcatCache
}

function WriteDeviceUiLog {
    param ( $deviceId ) 
    Write-Output "`n`nUI XML Log"
    adb -s $deviceId exec-out uiautomator dump /dev/tty 
}

function DateTimeNow {
    return Get-Date -UFormat "%T %Z"
}

function CheckAndCloseActiveSystemAlerts {
    param ($deviceId)
    $uiInfoXml = adb -s $deviceId exec-out uiautomator dump /dev/tty 
    if (($uiInfoXml | select-string "android:id/alertTitle|has stopped|Close app") -ne $null)
    {
        Write-Warning "Active system alert found on $deviceId.  Closing it."
        adb shell input keyevent 4
    }
}

function SignalActionSmokeStatus {
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
    Write-Output "Found $DeviceCount devices: $DeviceList"
}

# Check if APK was built.
If (Test-Path -Path "$ApkPath/$ApkFileName" ) 
{
    Write-Output "Found $ApkPath/$ApkFileName"
}
Else
{
    SignalActionSmokeStatus("Completed")
    Throw "Expected APK on $ApkPath/$ApkFileName but it was not found."
}

# Test
foreach ($device in $DeviceList)
{
    $deviceSdk = adb -s $device shell getprop ro.build.version.sdk 
    $deviceApi = adb -s $device shell getprop ro.build.version.release 
    Write-Output "`nChecking device $device with SDK $deviceSdk and API $deviceApi`n"

    $stdout = adb -s $device shell "pm list packages -f"
    if (($stdout | select-string $ProcessName) -ne $null)
    {
        Write-Output "Removing previous APP."
        $stdout = adb -s $device uninstall $ProcessName
    }

    # Move device to home screen
    $stdout = adb -s $device shell input keyevent KEYCODE_HOME

    Write-Output "Installing test app..."
    $stdout = (adb -s $device install -r $ApkPath/$ApkFileName)
    If($stdout -notcontains "Success")
    {
        SignalActionSmokeStatus("Completed")
        Throw "Failed to Install APK: $stdout."
    }

    $AppStarted = 'False'

    Write-Output "Clearing logcat from $device."
    adb -s $device logcat -c

    Write-Output "Starting Test..."

    adb -s $device shell am start -n $TestActivityName -e test smoke
    #despite calling start, the app might not be started yet.

    Write-Output (DateTimeNow)
    $Timeout = 45
    While ($Timeout -gt 0) 
    {
        #Get a list of active processes
        $processIsRunning = (adb -s $device shell ps)
        #And filter by ProcessName
        $processIsRunning = $processIsRunning | select-string $ProcessName
        
        If ($processIsRunning -eq $null -And $AppStarted -eq 'True')
        {
            $Timeout = -2
            break
        }
        ElseIf ($processIsRunning -ne $null -And $AppStarted -eq 'False')
        {
            # Some devices might take a while to start the test, so we wait for the activity to start before checking if it was closed.
            $AppStarted = 'True'
        }
        Write-Output "Waiting Process on $device to complete, waiting $Timeout seconds"
        Start-Sleep -Seconds 1
        $Timeout--
        CheckAndCloseActiveSystemAlerts($device)
    }

    Write-Output (DateTimeNow)
    SignalActionSmokeStatus("Completed")
    $LogcatCache = adb -s $device logcat -d
    $stdout = $LogcatCache  | select-string 'SMOKE TEST: PASS'
    If ($stdout -ne $null)
    {
        Write-Output "`n`n`n Final logcat"
        WriteDeviceLog($device)
        Write-Output "`n`n`n $stdout"
    }
    ElseIf ($Timeout -eq 0)
    {
        Write-Warning "Test Timeout, see Logcat info for more information below."
        WriteDeviceLog($device)
        Write-Output "PS info."
        adb -s $device shell ps

        WriteDeviceUiLog($device)
        TakeScreenshot($device)
        Throw "Test Timeout"
    }
    ElseIf (($LogcatCache | select-string 'Unity   : Timeout while trying detaching primary window.'))
    {
        SignalActionSmokeStatus("Flaky")
        Write-Warning "Test was flaky, unity failed to initialize."
        WriteDeviceLog($device)
        WriteDeviceUiLog($device)
        TakeScreenshot($device)
        Throw "Test was flaky, unity failed to initialize."
    }
    Else
    {
        Write-Warning "Process completed but Smoke test was not signaled."
        WriteDeviceLog($device)
        WriteDeviceUiLog($device)
        TakeScreenshot($device)
        Throw "Smoke Test Failed."
    }
}

Write-Output "`nTest completed."
