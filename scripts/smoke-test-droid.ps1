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
    Throw "Expected APK on $ApkPath/$ApkFileName but it was not found."
}

# Test
foreach ($device in $DeviceList)
{
    $deviceSdk = adb shell getprop ro.build.version.sdk 
    $deviceApi = adb shell getprop ro.build.version.release 
    Write-Output ""
    Write-Output "Checking device $device with SDK $deviceSdk and API $deviceApi"
    Write-Output ""

    Write-Output "Removing previous APP if found."
    $stdout = adb -s $device uninstall $ProcessName
    Write-Output "Installing test app..."
    $stdout = (adb -s $device install -r $ApkPath/$ApkFileName)
    If($stdout -notcontains "Success")
    {
        Throw "Failed to Install APK: $stdout."
    }
    $AppStarted = 'False'

    Write-Output "Clearing logcat from $device."
    adb -s $device logcat -c

    Write-Output "Starting Test..."

    adb -s $device shell am start -n $TestActivityName -e test smoke
    #despite calling start, the app might not be started yet.

    $Timeout = 30
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
    }

    If ($Timeout -eq 0)
    {
        Write-Warning "Test Timeout, see Logcat info for more information below."
        adb -s $device logcat -d  | select-string "Unity|unity|sentry|Sentry|SMOKE"
        Write-Output "PS info."
        adb -s $device ps
        Throw "Test Timeout"
    }

    $stdout = adb -s $device logcat -d  | select-string SMOKE
    If ($stdout -ne $null)
    {
        Write-Output "$stdout"
    }
    Else
    {
        Write-Warning "Process completed but Smoke test was not signaled."
        adb -s $device logcat -d  | select-string "Unity|unity|sentry|Sentry|SMOKE"
        Throw "Smoke Test Failed."
    }
}

Write-Output "Test completed."
