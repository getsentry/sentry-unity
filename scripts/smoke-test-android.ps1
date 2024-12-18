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
Write-Host "#####################################################"
Write-Host "#   ___ __  __  ___  _  _____ _____ ___ ____ _____  #"
Write-Host "#  / __|  \/  |/ _ \| |/ / __|_   _| __|/ __|_   _| #"
Write-Host "#  \__ \ |\/| | (_) | ' <| _|  | | | _| \__ \ | |   #"
Write-Host "#  |___/_|  |_|\___/|_|\_\___| |_| |___|___/  |_|   #"
Write-Host "#                                                   #"
Write-Host "#####################################################"

if ($IsIntegrationTest)
{
    $BuildDir = $(GetNewProjectBuildPath)
    $ApkFileName = "test.apk"
    $ProcessName = "com.DefaultCompany.$(GetNewProjectName)"

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
$FallBackTestActivityName = "$ProcessName/com.unity3d.player.UnityPlayerGameActivity"

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
    if ($IsMacOS)
    {
        screencapture "$(ArtifactsPath)/host-screenshot.jpg"
    }
    else {
        Write-Warning "Screenshot functionality is not implemented for this platform."
    }
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
    $startTime = Get-Date
    $timeout = New-TimeSpan -Seconds 60
    
    while ((Get-Date) - $startTime -lt $timeout)
    {
        if ($deviceApi -eq "21")
        {
            # `pidof` doesn't exist - take second column from the `ps` output for the given process instead.
            $processId = (adb -s $deviceId shell "ps | grep '$processName'") -Split " +" | Select-Object -Skip 1 -First 1
        }
        else
        {
            $processId = adb -s $deviceId shell pidof $processName
        }

        if (-not [string]::IsNullOrWhiteSpace($processId))
        {
            return $processId
        }

        Write-Host "Process '$processName' not found, retrying in 2 seconds..."
        Start-Sleep -Seconds 2
    }

    Write-Warning "Could not find PID for process '$processName' after 60 seconds"
    return $null
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

# Filter device list
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
    Write-Error "It seems like no devices were found $RawAdbDeviceList"
    exit 1
}
Else
{
    Write-Host "Found $DeviceCount devices: $DeviceList"
}

# Check if APK was built.
If (-not (Test-Path -Path "$BuildDir/$ApkFileName" ))
{
    Write-Error "Expected APK on $BuildDir/$ApkFileName but it was not found."
    exit 1
}


### START TEST

# Pick the first device available
$device = $DeviceList[0]
adb -s $device logcat -c

$deviceApi = "$(adb -s $device shell getprop ro.build.version.sdk)".Trim()
$deviceSdk = "$(adb -s $device shell getprop ro.build.version.release)".Trim()
Write-Host "`nChecking device $device with SDK '$deviceSdk' and API '$deviceApi'"

# Uninstall previous installation
$stdout = adb -s $device shell "pm list packages -f"
if ($null -ne ($stdout | Select-String $ProcessName))
{
    Write-Host "Uninstalling previous $ProcessName."
    $stdout = adb -s $device uninstall $ProcessName
}

# Move device to home screen
$stdout = adb -s $device shell input keyevent KEYCODE_HOME

# Install the test app
$adbInstallRetry = 5
do
{
    Write-Host "Installing test app"
    $stdout = (adb -s $device install -r $BuildDir/$ApkFileName 2>&1)

    if ($stdout.Contains("Broken pipe"))
    {
        Write-Warning "Failed to comunicate with the Device, retrying..."
        Start-Sleep 3
        $adbInstallRetry--
    }
} while ($adbInstallRetry -gt 1 -and $stdout.Contains("Broken pipe"))

# Validate the installation
If ($stdout -contains "Success")
{
    Write-Host "Successfully installed APK"
}
else 
{
    OnError $device $deviceApi
    Write-Error "Failed to install APK: $stdout."
    return 1
}

function ProcessNewLogs([array]$newLogs, [ref]$lastLogCount, [array]$logCache) {
    if ($newLogs) {
        $currentLogs = @($newLogs)  # Force array creation even for single line
        if ($currentLogs.Count -gt $lastLogCount.Value) {
            $newLines = $currentLogs[$lastLogCount.Value..($currentLogs.Count-1)]
            $lastLogCount.Value = $currentLogs.Count
            
            if ($newLines) {
                $logCache += $newLines
            }
        }
    }
    return $logCache
}

function RunTest([string] $Name, [string] $SuccessString, [string] $FailureString)
{
    Write-Host "::group::Test: '$name'"

    Write-Host "Clearing logcat from '$device'"
    adb -s $device logcat -c

    $activityName = $TestActivityName

    Write-Host "Starting app '$activityName'"

    # Mark the full-screen notification as acknowledged
    adb -s $device shell "settings put secure immersive_mode_confirmations confirmed"
    adb -s $device shell "input keyevent KEYCODE_HOME"

    $output = & adb -s $device shell am start -n $activityName -e test $Name -W 2>&1
    
    # Check if the activity failed to start
    if ($output -match "Error type 3" -or $output -match "Activity class \{$activityName\} does not exist.")
    {
        $activityName = $FallBackTestActivityName
        Write-Host "Trying fallback activity $activityName"

        $output = & adb -s $device shell am start -n $activityName -e test $Name -W 2>&1
        
        # Check if the fallback activity failed to start
        if ($output -match "Error type 3" -or $output -match "Activity class \{$activityName\} does not exist.")
        {
            Write-Error "Activity does not exist"
            return $false
        }
    } 
    
    Write-Host "Activity started successfully"

    $appPID = PidOf $device $ProcessName
    if ($null -eq $appPID)
    {
        Write-Host "::endgroup::"
        Write-Error "Could not find PID for process '$ProcessName'"
        exit 1
    }

    Write-Host "Retrieved ID for '$ProcessName': $appPID"

    Write-Host "Waiting for tests to run..."
    
    $processFinished = $false
    $logCache = @()
    $startTime = Get-Date
    $timeout = New-TimeSpan -Seconds 500
    $lastLogCount = 0

    # Wait for the tests to run and the game process to complete
    while ((Get-Date) - $startTime -lt $timeout)
    {
        $newLogs = adb -s $device logcat -d --pid=$appPID
        $logCache = ProcessNewLogs -newLogs $newLogs -lastLogCount ([ref]$lastLogCount) -logCache $logCache

        # The SmokeTester logs "SmokeTester is quitting." in OnApplicationQuit() to reliably inform when tests finish running.
        # For crash tests, we expect to see a native crash log "terminating with uncaught exception of type char const*".
        if (($newLogs | Select-String "SmokeTester is quitting.") -or ($newLogs | Select-String "terminating with uncaught exception of type char const*"))
        {
            Write-Host "Process finished marker detected. Finish waiting for tests to run."
            $processFinished = $true
            break
        }

        Start-Sleep -Seconds 1
    }

    if ($processFinished)
    {
        Write-Host "'$Name' test finished running in $((Get-Date) - $runTime | ForEach-Object { '{0:mm}m {0:ss}s' -f $_ })"
    }
    else
    {   
        Write-Host "'$Name' tests timed out. See logcat for more details."
    }

    Write-Host "::endgroup::"

    # Fetch the latest logs from the device
    $logCache = ProcessNewLogs -newLogs $newLogs -lastLogCount ([ref]$lastLogCount) -logCache $logCache

    Write-Host "::group::logcat"
    $logCache | ForEach-Object { Write-Host $_ } 
    Write-Host "::endgroup::"

    $lineWithSuccess = $logCache | Select-String $SuccessString
    $lineWithFailure = $logCache | Select-String $FailureString

    if ($null -ne $lineWithSuccess)
    {
        Write-Host "'$Name' test passed." -ForegroundColor Green
        return $true
    }
    elseif ($null -ne $lineWithFailure)
    {
        Write-Host "'$Name' test failed. See logcat for more details." -ForegroundColor Red
        return $false
    }
    
    Write-Host "'$Name' test execution failed." -ForegroundColor Red
    return $false
}

function RunTestWithRetry([string] $Name, [string] $SuccessString, [string] $FailureString, [int] $MaxRetries = 3)
{
    for ($retryCount = 0; $retryCount -lt $MaxRetries; $retryCount++)
    {
        if ($retryCount -gt 0)
        {
            Write-Host "Retry attempt $retryCount for test '$Name'"
            Start-Sleep -Seconds 2  # Brief pause between retries
        }

        Write-Host "Running test attempt $($retryCount + 1)/$MaxRetries"
        $result = RunTest -Name $Name -SuccessString $SuccessString -FailureString $FailureString
        
        if ($result)
        {
            Write-Host "'$Name' test passed on attempt $($retryCount + 1)." -ForegroundColor Green
            return $true
        }
        
        if ($retryCount + 1 -lt $MaxRetries)
        {
            Write-Host "'$Name' test failed. Retrying..." -ForegroundColor Yellow
            continue
        }
        
        Write-Host "'$Name' test failed after $MaxRetries attempts." -ForegroundColor Red
        return $false
    }
    
    return $false
}

$results = @{
    smokeTestPassed = $false
    hasntCrashedTestPassed = $false
    crashTestPassed = $false
    hasCrashTestPassed = $false
}

$results.smoketestPassed = RunTestWithRetry -Name "smoke" -SuccessString "SMOKE TEST: PASS" -FailureString "SMOKE TEST: FAIL" -MaxRetries 3
$results.hasntCrashedTestPassed = RunTestWithRetry -Name "hasnt-crashed" -SuccessString "HASNT-CRASHED TEST: PASS" -FailureString "HASNT-CRASHED TEST: FAIL" -MaxRetries 3

try
{
    CrashTestWithServer -SuccessString "POST /api/12345/envelope/ HTTP/1.1`" 200 -b'1f8b08000000000000" -CrashTestCallback {
        $results.crashTestPassed = RunTestWithRetry -Name "crash" -SuccessString "CRASH TEST: Issuing a native crash" -FailureString "CRASH TEST: FAIL" -MaxRetries 3
        $results.hasCrashTestPassed = RunTestWithRetry -Name "has-crashed" -SuccessString "HAS-CRASHED TEST: PASS" -FailureString "HAS-CRASHED TEST: FAIL" -MaxRetries 3
    }
}
catch
{
    Write-Warning "Caught exception: $_"
    Write-Host $_.ScriptStackTrace
    OnError $device $deviceApi
    exit 1
}

$failed = $false

if (-not $results.smoketestPassed) 
{
    Write-Error "Smoke test failed"
    $failed = $true
}

if (-not $results.hasntCrashedTestPassed)
{
    Write-Error "HasntCrashed test failed" 
    $failed = $true
}

if (-not $results.crashTestPassed)
{
    Write-Error "Crash test failed"
    $failed = $true
}

if (-not $results.hasCrashTestPassed)
{
    Write-Error "HasCrashed test failed"
    $failed = $true
}

if ($failed)
{
    exit 1
}

Write-Host "All tests passed" -ForegroundColor Green
exit 0
