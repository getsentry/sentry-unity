param (
    [string] $Action,
    [string] $SelectedRuntime,
    [Int32] $DevicesToRun = 1,
    [Switch] $IsIntegrationTest,
    [string] $UnityVersion = "",
    [string] $iOSMinVersion = ""
)
# $Action: 'Build' for build only
#          'Test' for Smoke test only
#          null for building and testing
# $SelectedRuntime: the runtime to be run,
#          'latest' for runing the test with the latest runtime
#          'iOS <version>' to run on the specified runtime ex: iOS 12.4
# $DevicesToRun: the amount of devices to run
#          '0' or empty will run on 1 device, otherwise on the specified amount.

. $PSScriptRoot/../test/Scripts.Integration.Test/common.ps1

Write-Log "Args received Action=$Action, SelectedRuntime=$SelectedRuntime, IsIntegrationTest=$IsIntegrationTest"

$ProjectName = "Unity-iPhone"
$XcodeArtifactPath = "samples/IntegrationTest/Build"
$ArchivePath = "$XcodeArtifactPath/archive"
$AppPath = "$XcodeArtifactPath/IntegrationTest.app"
$AppName = "com.DefaultCompany.IntegrationTest"

Class AppleDevice
{
    [String]$Name
    [String]$UUID
    [boolean]$TestFailed
    [boolean]$TestSkipped

    Parse([String]$unparsedDevice)
    {
        # Example of unparsed device:
        #    iPhone 11 (D3762152-4648-4734-A409-15855F84587A) (Shutdown)
        $unparsedDevice = $unparsedDevice.Trim()
        $result = [regex]::Match($unparsedDevice, "(?<model>.+) \((?<uuid>[0-9A-Fa-f\-]{36})")
        if ($result.Success -eq $False)
        {
            Throw "$unparsedDevice is not a valid iOS device"
        }
        $this.Name = $result.Groups["model"].Value
        $this.UUID = $result.Groups["uuid"].Value
    }
}

function Build()
{
    MakeExecutable "$XcodeArtifactPath/MapFileParser.sh"
    MakeExecutable "$XcodeArtifactPath/sentry-cli-Darwin-universal"

    $buildCallback = {

        Write-Host "::group::Building iOS project"
        try
        {
            xcodebuild `
                -project "$XcodeArtifactPath/$ProjectName.xcodeproj" `
                -scheme "Unity-iPhone" `
                -configuration "Release" `
                -sdk "iphonesimulator" `
                -destination "platform=iOS Simulator,OS=$iOSMinVersion" `
                -destination "platform=iOS Simulator,OS=latest" `
                -parallel-testing-enabled YES `
                -derivedDataPath "$ArchivePath/$ProjectName" `
            | Write-Host
        }
        finally
        {
            Write-Host "::endgroup::"
        }
    }

    if ($IsIntegrationTest)
    {
        $symbolServerOutput = RunWithSymbolServer -Callback $buildCallback
        CheckSymbolServerOutput 'IOS' $symbolServerOutput $UnityVersion
    }
    else
    {
        $buildCallback.Invoke()
    }
}

function Test
{
    Write-Log "Retrieving list of available simulators" -ForegroundColor Green
    $deviceListRaw = xcrun simctl list devices
    Write-Host "::group::Available simulators:"
    $deviceListRaw | Write-Host
    Write-Host "::endgroup::"
    
    [AppleDevice[]]$deviceList = @()

    Write-Log "Picking simulator based on selected runtime" -ForegroundColor Green

    # Find the index of the selected runtime
    $runtimeIndex = ($deviceListRaw | Select-String "-- $SelectedRuntime --").LineNumber
    If ($null -eq $runtimeIndex)
    {
        $deviceListRaw | Write-Host
        throw " Runtime (-- $SelectedRuntime --) not found"
    }

    foreach ($device in $deviceListRaw[$runtimeIndex..($deviceListRaw.Count - 1)])
    {
        If ($device.StartsWith("--"))
        {
            # Reached at the end of the iOS list
            break
        }
        $dev = [AppleDevice]::new()
        $dev.Parse($device)
        $deviceList += $dev
    }

    $deviceCount = $DeviceList.Count

    Write-Host "::group::Found $deviceCount devices on version $SelectedRuntime" -ForegroundColor Green
    ForEach ($device in $deviceList)
    {
        Write-Host "$($device.Name) - $($device.UUID)"
    }
    Write-Host "::endgroup::"

    $devicesRan = 0
    ForEach ($device in $deviceList)
    {
        If ($devicesRan -ge $DevicesToRun)
        {
            # Write-Log "Skipping Simulator $($device.Name) UUID $($device.UUID)" -ForegroundColor Green
            $device.TestSkipped = $true
            continue
        }
        $devicesRan++
        Write-Log "Starting Simulator $($device.Name) UUID $($device.UUID)" -ForegroundColor Green
        xcrun simctl boot $($device.UUID)
        Write-Log -NoNewline "Installing Smoke Test on $($device.Name): "
        If (!(Test-Path $AppPath))
        {
            Write-Error "App doesn't exist at the expected path $AppPath. Did you forget to run Build first?"
        }
        xcrun simctl install $($device.UUID) "$AppPath"
        Write-Log "OK" -ForegroundColor Green

        function RunTest([string] $Name, [string] $SuccessString, [int] $TimeoutSeconds = 180)
        {
            Write-Log "Test: '$Name'"
            Write-Log "Launching '$Name' test on '$($device.Name)'" -ForegroundColor Green

            # Use Start-Process with output redirection and timeout to prevent hanging
            # when the app crashes (which is expected for crash tests)
            $outFile = New-TemporaryFile
            $errFile = New-TemporaryFile
            $consoleOut = @()

            try
            {
                $process = Start-Process "xcrun" `
                    -ArgumentList "simctl", "launch", "--console-pty", $device.UUID, $AppName, "--test", $Name `
                    -NoNewWindow -PassThru `
                    -RedirectStandardOutput $outFile `
                    -RedirectStandardError $errFile

                $timedOut = $null
                $process | Wait-Process -Timeout $TimeoutSeconds -ErrorAction SilentlyContinue -ErrorVariable timedOut

                if ($timedOut)
                {
                    Write-Log "Test '$Name' timed out after $TimeoutSeconds seconds - stopping process" -ForegroundColor Yellow
                    $process | Stop-Process -Force -ErrorAction SilentlyContinue
                }

                # Read captured output
                $consoleOut = @(Get-Content $outFile -ErrorAction SilentlyContinue) + `
                              @(Get-Content $errFile -ErrorAction SilentlyContinue)
            }
            finally
            {
                Remove-Item $outFile -ErrorAction SilentlyContinue
                Remove-Item $errFile -ErrorAction SilentlyContinue
            }

            if ("$SuccessString" -eq "")
            {
                $SuccessString = "$($Name.ToUpper()) TEST: PASS"
            }

            Write-Host "::group::$($device.Name) Console Output"
            foreach ($consoleLine in $consoleOut)
            {
                Write-Host $consoleLine
            }
            Write-Host "::endgroup::"

            Write-Log -NoNewline "'$Name' test STATUS: "
            $stdout = $consoleOut | Select-String $SuccessString
            If ($null -ne $stdout)
            {
                Write-Log "PASSED" -ForegroundColor Green
            }
            Else
            {
                $device.TestFailed = $True
                Write-Log "FAILED" -ForegroundColor Red
                throw "Test '$Name' failed - success string '$SuccessString' not found in output"
            }
        }

        function RunTestSuiteWithRetry([int] $MaxRetries = 3)
        {
            for ($attempt = 1; $attempt -le $MaxRetries; $attempt++)
            {
                Write-Log "Test suite attempt $attempt/$MaxRetries" -ForegroundColor Cyan

                if ($attempt -gt 1)
                {
                    # Reset simulator state between retries
                    Write-Log "Reinstalling app to reset state..." -ForegroundColor Yellow
                    xcrun simctl uninstall $($device.UUID) $AppName 2>$null
                    Start-Sleep -Seconds 2
                    xcrun simctl install $($device.UUID) "$AppPath"
                    Start-Sleep -Seconds 2
                }

                $device.TestFailed = $false

                try
                {
                    RunTest "smoke"
                    RunTest "hasnt-crashed"

                    # Note: mobile apps post the crash on the second app launch, so we must run both as part of the "CrashTestWithServer"
                    CrashTestWithServer -SuccessString "POST /api/12345/envelope/ HTTP/1.1`" 200 -b'1f8b08000000000000" -CrashTestCallback {
                        RunTest "crash" "CRASH TEST: Issuing a native crash"
                        RunTest "has-crashed"
                    }

                    Write-Log "All tests passed on attempt $attempt/$MaxRetries" -ForegroundColor Green
                    return  # Success!
                }
                catch
                {
                    Write-Log "Test suite attempt $attempt failed: $_" -ForegroundColor Yellow

                    if ($attempt -lt $MaxRetries)
                    {
                        Write-Log "Will retry..." -ForegroundColor Yellow
                        continue
                    }

                    # Final attempt failed
                    throw
                }
            }
        }

        RunTestSuiteWithRetry -MaxRetries 3

        Write-Log -NoNewline "Removing Smoke Test from $($device.Name): "
        xcrun simctl uninstall $($device.UUID) $AppName
        Write-Log "OK" -ForegroundColor Green

        Write-Log -NoNewline "Requesting shutdown for $($device.Name): "
        # Do not wait for the Simulator to close and continue testing the other simulators.
        Start-Process xcrun -ArgumentList "simctl shutdown `"$($device.UUID)`""
        Write-Log "OK" -ForegroundColor Green
    }

    $testFailed = $false
    Write-Log "Test result"
    foreach ($device in $deviceList)
    {
        Write-Log -NoNewline "$($device.Name) UUID $($device.UUID): "
        If ($device.TestFailed)
        {
            Write-Log "FAILED" -ForegroundColor Red
            $testFailed = $true
        }
        ElseIf ($device.TestSkipped)
        {
            Write-Log "SKIPPED" -ForegroundColor Gray
        }
        Else
        {
            Write-Log "PASSED" -ForegroundColor Green
        }
    }
    Write-Log "End of test."

    If ($testFailed -eq $true)
    {
        Throw "One or more tests failed."
    }
}

function LatestRuntime
{
    $runtimes = xcrun simctl list runtimes iOS
    $lastRuntime = $runtimes | Select-Object -Last 1
    $result = [regex]::Match($lastRuntime, "(?<runtime>iOS [0-9.]+)")
    if ($result.Success -eq $False)
    {
        Throw "Last runtime was not found, result: $result"
    }
    $lastRuntimeParsed = $result.Groups["runtime"].Value
    Write-Log "Using latest runtime = $lastRuntimeParsed"
    return $lastRuntimeParsed
}

# MAIN
If (-not $IsMacOS)
{
    Write-Log "This script should only be run on a MacOS." -ForegroundColor Yellow
}
If ($null -eq $action -Or $action -eq "Build")
{
    Build
}
If ($null -eq $action -Or $action -eq "Test")
{
    If ($SelectedRuntime -eq "latest" -Or $null -eq $SelectedRuntime)
    {
        $SelectedRuntime = LatestRuntime
    }
    Test
}
