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

. $PSScriptRoot/../test/Scripts.Integration.Test/common.ps1

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

function OnError() {
    WriteDeviceLog($device)
    WriteDeviceUiLog($device)
    TakeScreenshot($device)
}

function DateTimeNow {
    return Get-Date -UFormat "%T %Z"
}

function CheckAndCloseActiveSystemAlerts {
    param ($deviceId)
    $uiInfoXml = adb -s $deviceId exec-out uiautomator dump /dev/tty
    if (($uiInfoXml | select-string "android:id/alertTitle|has stopped|Close app") -ne $null) {
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
foreach ($device in $RawAdbDeviceList) {
    If ($device.EndsWith("device")) {
        $DeviceList += $device.Replace("device", '').Trim()
    }
}
$DeviceCount = $DeviceList.Count

If ($DeviceCount -eq 0) {
    SignalActionSmokeStatus("Completed")
    Throw "It seems like no devices were found $RawAdbDeviceList"
}
Else {
    Write-Output "Found $DeviceCount devices: $DeviceList"
}

# Check if APK was built.
If (Test-Path -Path "$ApkPath/$ApkFileName" ) {
    Write-Output "Found $ApkPath/$ApkFileName"
}
Else {
    SignalActionSmokeStatus("Completed")
    Throw "Expected APK on $ApkPath/$ApkFileName but it was not found."
}

# Test
foreach ($device in $DeviceList) {
    $deviceSdk = adb -s $device shell getprop ro.build.version.sdk
    $deviceApi = adb -s $device shell getprop ro.build.version.release
    Write-Output "`nChecking device $device with SDK $deviceSdk and API $deviceApi`n"

    $stdout = adb -s $device shell "pm list packages -f"
    if ($null -ne ($stdout | select-string $ProcessName)) {
        Write-Output "Removing previous APP."
        $stdout = adb -s $device uninstall $ProcessName
    }

    # Move device to home screen
    $stdout = adb -s $device shell input keyevent KEYCODE_HOME

    Write-Output "Installing test app..."
    $stdout = (adb -s $device install -r $ApkPath/$ApkFileName)
    If ($stdout -notcontains "Success") {
        SignalActionSmokeStatus("Completed")
        Throw "Failed to Install APK: $stdout."
    }

    function RunTest([string] $Name, [string] $SuccessString, [string] $FailureString) {
        $AppStarted = $false

        Write-Output "Clearing logcat from $device."
        adb -s $device logcat -c

        Write-Output "Starting Test..."

        adb -s $device shell am start -n $TestActivityName -e test $Name
        #despite calling start, the app might not be started yet.

        Write-Output (DateTimeNow)
        $Timeout = 45
        While ($Timeout -gt 0) {
            #Get a list of active processes
            $processIsRunning = (adb -s $device shell ps)
            #And filter by ProcessName
            $processIsRunning = $processIsRunning | select-string $ProcessName

            If ($processIsRunning -eq $null -And $AppStarted) {
                $Timeout = -2
                break
            }
            ElseIf ($processIsRunning -ne $null -And !$AppStarted) {
                # Some devices might take a while to start the test, so we wait for the activity to start before checking if it was closed.
                $AppStarted = $true
            }
            Write-Output "Waiting Process on $device to complete, waiting $Timeout seconds"
            Start-Sleep -Seconds 1
            $Timeout--
            CheckAndCloseActiveSystemAlerts($device)
        }

        Write-Output (DateTimeNow)
        $LogcatCache = adb -s $device logcat -d
        $lineWithSuccess = $LogcatCache | select-string $SuccessString
        $lineWithFailure = $LogcatCache | select-string $FailureString
        If ($lineWithFailure -ne $null) {
            SignalActionSmokeStatus("Failed")
            Write-Warning "$name test failed"
            Write-Warning "$lineWithFailure"
            OnError
            throw "$Name test: FAIL"
        }
        elseif ($lineWithSuccess -ne $null) {
            Write-Host "$lineWithSuccess"
            Write-Host "$Name test: PASS" -ForegroundColor Green
        }
        ElseIf (($LogcatCache | select-string 'Unity   : Timeout while trying detaching primary window.|because ULR inactive')) {
            SignalActionSmokeStatus("Flaky")
            Write-Warning "$name test was flaky, unity failed to initialize."
            OnError
            Throw "$name test was flaky, unity failed to initialize."
        }
        ElseIf ($Timeout -eq 0) {
            SignalActionSmokeStatus("Timeout")
            Write-Warning "$name Test Timeout, see Logcat info for more information below."
            Write-Host "PS info."
            adb -s $device shell ps
            OnError
            Throw "$name test Timeout"
        }
        Else {
            SignalActionSmokeStatus("Failed")
            Write-Warning "$name test: process completed but $Name test was not signaled."
            OnError
            Throw "$Name test Failed."
        }
    }

    RunTest -Name "smoke" -SuccessString "SMOKE TEST: PASS" -FailureString "SMOKE TEST: FAIL"
    # post-crash must fail now, because the previous run wasn't a crash
    RunTest -Name "post-crash" -SuccessString "POST-CRASH TEST | 1. options.CrashedLastRun() == true: FAIL" -FailureString "POST-CRASH TEST: PASS"

    try {
        # Note: mobile apps post the crash on the second app launch, so we must run both as part of the "CreshTestWithServer"
        CrashTestWithServer -SuccessString "TODO" -CrashTestCallback {
            RunTest -Name  "crash" -SuccessString "CRASH TEST: Issuing a native crash" -FailureString "CRASH TEST: FAIL"
            RunTest -Name  "post-crash" -SuccessString "POST-CRASH TEST: PASS" -FailureString "POST-CRASH TEST: FAIL"
        }
    }
    catch {
        SignalActionSmokeStatus("Failed");
        OnError
        throw;
    }
}

SignalActionSmokeStatus("Completed")
Write-Host "Tests completed successfully." -ForegroundColor Green