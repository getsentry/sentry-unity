# GITHUB_WORKSPACE is the root folder where the project is stored.
Set-Variable -Name "ApkPath" -Value ($Env:GITHUB_WORKSPACE + "/samples/artifacts/builds/Android")
Set-Variable -Name "ApkFileName" -Value "IL2CPP_Player.apk"

# Check if APK was built.
if (Test-Path -Path "$ApkPath/$ApkFileName" ) 
{
    Write-Output "Found $ApkPath/$ApkFileName"
}
else
{
    Write-Error "Expected APK on $ApkPath/$ApkFileName but it was not found."
    exit(-1);
}


# Filter device List
$RawAdbDeviceList = ".adb" devices
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
    Write-Error "It seems like no devices were found $RawAdbDeviceList"
    exit(-1)
}
else
{
    Write-Output "Found $deviceCount devices, they are $deviceList"
}

# Test
foreach ($device in $deviceList)
{
    Write-Output "Installing Apk on $device."

    $stdout = ".adb" -s $device install -r $ApkPath/$ApkFileName
    if($stdout -notcontains "Success")
    {
        Write-Error "Failed to Install APK: $stdout."
        exit(-1)
    }

    Write-Output "Clearing logcat from $device."

    ".adb" -s $device logcat -c

    Write-Output "Starting Test..."

    ".adb" -s $device shell am start -n io.sentry.samples.unityofbugs/com.unity3d.player.UnityPlayerActivity -e test smoke

    Start-Sleep -Seconds 2

    for ($i = 30; $i -gt 0; $i--) {
        $smokeTestId = (& ".adb" '-s', $device, 'shell', 'pidof', 'io.sentry.samples.unityofbugs'  2>&1)
        if ( $smokeTestId -eq $null)
        {
            $i = -2;
        }
        else
        {
            Write-Output "Proccess $smokeTestId still running on $device, waiting $i seconds"
            Start-Sleep -Seconds 1
        }
    }

    if ( $i -eq -2)
    {
        Write-Error "Test Timeout"
        exit(-1)
    }

    $stdout = ".adb"  -s $device logcat -d  | findstr SMOKE
    if ( $stdout -ne $null)
    {
        Write-Output "$stdout"
    }
    else
    {
        Write-Error "Smoke Test Failed, printing logcat..."
        ".adb" -s $device logcat -d  | findstr "Unity unity sentry Sentry SMOKE"
        exit(-1)
    }
}

Write-Output "Test completed."
exit(0)