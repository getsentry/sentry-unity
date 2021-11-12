# GITHUB_WORKSPACE is the root folder where the project is stored.
Write-Output "#################################################"
Write-Output "#   ANDROID                                     #"
Write-Output "#            VALIDATOR                          #"
Write-Output "#                       SCRIPT                  #"
Write-Output "#################################################"

Set-Variable -Name "ApkPath" -Value "/samples/artifacts/builds/Android"
Set-Variable -Name "ApkFileName" -Value "IL2CPP_Player.apk"
Set-Variable -Name "ActivityName" -Value "io.sentry.samples.unityofbugs"
Set-Variable -Name "TestActivityName" -Value "io.sentry.samples.unityofbugs/com.unity3d.player.UnityPlayerActivity"

# Filter device List
$RawAdbDeviceList = adb devices
$deviceList = @()
foreach ($device in $RawAdbDeviceList)
{
    if ($device.EndsWith("device"))
    {
        $deviceList += $device.Replace("device", '').Trim()
    }
}
$deviceCount = $deviceList.Count

if ($deviceCount -eq 0)
{
    Throw "It seems like no devices were found $RawAdbDeviceList"
}
else
{
    Write-Output "Found $deviceCount devices: $deviceList"
}

# Check if APK was built.
if (Test-Path -Path "$ApkPath/$ApkFileName" ) 
{
    Write-Output "Found $ApkPath/$ApkFileName"
}
else
{
    Throw "Expected APK on $ApkPath/$ApkFileName but it was not found."
}

# Test
foreach ($device in $deviceList)
{
    $deviceSdk = adb shell getprop ro.build.version.sdk 
    $deviceApi = adb shell getprop ro.build.version.release 
    Write-Output ""
    Write-Output "Checking device $device with SDK $deviceSdk and API $deviceApi"
    Write-Output ""

    # Check Command Available for checking if App is running
    # Works with Android 7.0 and Higher
#    $IsRunningArg = "New"
#    $stdout = adb -s $device shell pidof $ActivityName
#    if ($stdout -like "*pidof*")
#    {
#        $IsRunningArg = "Old"
#    }

    $stdout = (adb -s $device install -r $ApkPath/$ApkFileName)
    if($stdout -notcontains "Success")
    {
        Throw "Failed to Install APK: $stdout."
        exit(-1)
    }

    Write-Output "Clearing logcat from $device."

    adb -s $device logcat -c

    Write-Output "Starting Test..."

    adb -s $device shell am start -n $TestActivityName -e test smoke

    for ($i = 30; $i -gt 0; $i--) {
	
#        if ($IsRunningArg -eq "New")
#        {
#            # Android 7 and Higher
#            $smokeTestId = adb -s $device shell pidof $ActivityName
#        }
#        else
#        {
        $smokeTestId = (adb -s $device shell ps)
	    $smokeTestId = $smokeTestId | select-string $ActivityName
#        }

        if ($smokeTestId -eq $null)
        {
            $i = -2
        }
        else
        {
            Write-Output "Process still running on $device, waiting $i seconds"
            Start-Sleep -Seconds 1
        }
    }

    if ($i -eq 0)
    {
	Write-Output "Logcat info."
        adb -s $device logcat -d  | select-string "Unity|unity|sentry|Sentry|SMOKE"
	Write-Output "PS info."
        adb -s $device ps
        Throw "Test Timeout"
    }

    $stdout = adb -s $device logcat -d  | select-string SMOKE
    if ($stdout -ne $null)
    {
        Write-Output "$stdout"
    }
    else
    {
        adb -s $device logcat -d  | select-string "Unity|unity|sentry|Sentry|SMOKE"
        Throw "Smoke Test Failed."
    }
}

Write-Output "Test completed."