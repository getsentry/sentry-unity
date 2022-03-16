param($Action, $SelectedRuntime, $DevicesToRun)
Write-Host "Args received Action=$Action, SelectedRuntime=$SelectedRuntime"
# $Action: 'Build' for build only
#          'Test' for Smoke test only
#          null for building and testing
# $SelectedRuntime: the runtime to be run,
#          'latest' for runing the test with the latest runtime
#          'iOS <version>' to run on the specified runtime ex: iOS 12.4
# $DevicesToRun: the amount of devices to run
#          '0' or empty will run on 3 devices, otherwise on the specified amount.
$ErrorActionPreference = "Stop"

. $PSScriptRoot/../test/Scripts.Integration.Test/common.ps1

$XcodeArtifactPath = "samples/artifacts/builds/iOS/Xcode"
$ArchivePath = "$XcodeArtifactPath/archive"
$ProjectName = "Unity-iPhone"
$AppPath = "$ArchivePath/$ProjectName/Build/Products/Release-IphoneSimulator/unity-of-bugs.app"
$AppName = "io.sentry.samples.unityofbugs"

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
    $mapFileParser = "MapFileParser.sh"
    Write-Host -NoNewline "Fixing permission for $mapFileParser : "
    If (Test-Path -Path "$XcodeArtifactPath/$mapFileParser")
    {
        # We need to allow the execution of this script, otherwise Xcode will throw permission denied.
        chmod +x "$XcodeArtifactPath/$mapFileParser"
        Write-Host "OK" -ForegroundColor Green
    }
    Else
    {
        Write-Host "NOT FOUND" -ForegroundColor Gray
    }

    Write-Host "Building iOS project"
    $xCodeBuild = Start-Process -FilePath "xcodebuild" -ArgumentList "-project", "$XcodeArtifactPath/$ProjectName.xcodeproj", "-scheme", "Unity-iPhone", "-configuration", "Release", "-sdk", "iphonesimulator", "-derivedDataPath", "$ArchivePath/$ProjectName" -PassThru
    $xCodeBuild.WaitForExit()
    If ($xCodeBuild.ExitCode -ne 0)
    {
        Throw "Xcode build exited with code $($xCodeBuild.ExitCode)."
    }
    Write-Host "OK" -ForegroundColor Green
}

function Test
{
    Write-Host "Retrieving list of available simulators" -ForegroundColor Green
    $deviceListRaw = xcrun simctl list devices
    [AppleDevice[]]$deviceList = @()

    # Find the index of the selected runtime
    $runtimeIndex = ($deviceListRaw | Select-String "-- $SelectedRuntime --").LineNumber
    If ($null -eq $runtimeIndex)
    {
        $deviceListRaw | Write-Host
        throw " Runtime (-- $SelectedRuntime --) not found"
    }

    foreach ($device in $deviceListRaw[$runtimeIndex..$deviceListRaw.Count])
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

    Write-Host "Found $deviceCount devices on version $SelectedRuntime" -ForegroundColor Green

    ForEach ($device in $deviceList)
    {
        Write-Host "$($device.Name) - $($device.UUID)"
    }

    $devicesRan = 0
    If (0 -eq $DevicesToRun -Or $null -eq $DevicesToRun)
    {
        $DevicesToRun = 3
    }
    ForEach ($device in $deviceList)
    {
        If ($skippedItems -lt $skipCount)
        {
            Write-Host "Skipping Simulator $($device.Name) UUID $($device.UUID)" -ForegroundColor Green
            $device.TestSkipped = $true
            $skippedItems++
            continue
        }
        If ($devicesRan -ge $DevicesToRun)
        {
            Write-Host "Skipping Simulator $($device.Name) UUID $($device.UUID)" -ForegroundColor Green
            $device.TestSkipped = $true
            continue
        }
        $devicesRan++
        Write-Host "Starting Simulator $($device.Name) UUID $($device.UUID)" -ForegroundColor Green
        xcrun simctl boot $($device.UUID)
        Write-Host -NoNewline "Installing Smoke Test on $($device.Name): "
        If (!(Test-Path $AppPath))
        {
            Write-Error "App doesn't exist at the expected path $AppPath. Did you forget to run Build first?"
        }
        xcrun simctl install $($device.UUID) "$AppPath"
        Write-Host "OK" -ForegroundColor Green

        function RunTest([string] $Name, [string] $SuccessString)
        {
            Write-Host "Launching $Name test on $($device.Name)" -ForegroundColor Green
            $consoleOut = xcrun simctl launch --console-pty $($device.UUID) $AppName "--test" $Name

            if ("$SuccessString" -eq "")
            {
                $SuccessString = "${$Name.ToUpper()} TEST: PASS"
            }

            Write-Host -NoNewline "$Name test STATUS: "
            $stdout = $consoleOut  | Select-String $SuccessString
            If ($null -ne $stdout)
            {
                Write-Host "PASSED" -ForegroundColor Green
            }
            Else
            {
                $device.TestFailed = $True
                Write-Host "FAILED" -ForegroundColor Red
                Write-Host "$($device.Name) Console"
                foreach ($consoleLine in $consoleOut)
                {
                    Write-Host $consoleLine
                }
                Write-Host " ===== END OF CONSOLE ====="
            }
        }

        RunTest "smoke"
        RunTest "hasnt-crashed"

        try
        {
            # Note: mobile apps post the crash on the second app launch, so we must run both as part of the "CrashTestWithServer"
            CrashTestWithServer -SuccessString "POST /api/12345/envelope/ HTTP/1.1`" 200 -b'1f8b08000000000000" -CrashTestCallback {
                RunTest "crash" "CRASH TEST: Issuing a native crash"
                RunTest "has-crashed"
            }
        }
        catch
        {
            Write-Host "$($device.Name) Console"
            foreach ($consoleLine in $consoleOut)
            {
                Write-Host $consoleLine
            }
            throw;
        }

        Write-Host -NoNewline "Removing Smoke Test from $($device.Name): "
        xcrun simctl uninstall $($device.UUID) $AppName
        Write-Host "OK" -ForegroundColor Green

        Write-Host -NoNewline "Requesting shutdown for $($device.Name): "
        # Do not wait for the Simulator to close and continue testing the other simulators.
        Start-Process xcrun -ArgumentList "simctl shutdown `"$($device.UUID)`""
        Write-Host "OK" -ForegroundColor Green
    }

    $testFailed = $false
    Write-Host "Test result"
    foreach ($device in $deviceList)
    {
        Write-Host -NoNewline "$($device.Name) UUID $($device.UUID): "
        If ($device.TestFailed)
        {
            Write-Host "FAILED" -ForegroundColor Red
            $testFailed = $true
        }
        ElseIf ($device.TestSkipped)
        {
            Write-Host "SKIPPED" -ForegroundColor Gray
        }
        Else
        {
            Write-Host "PASSED" -ForegroundColor Green
        }
    }
    Write-Host "End of test."

    If ($testFailed -eq $true)
    {
        Throw "One or more tests failed."
    }
}

function LatestRuntime
{
    $runtimes = xcrun simctl list runtimes iOS
    $lastRuntime = $runtimes[$runtimesnewArray.Count – 1]
    $result = [regex]::Match($lastRuntime, "(?<runtime>iOS [0-9.]+)")
    if ($result.Success -eq $False)
    {
        Throw "Last runtime was not found, result: $result"
    }
    $lastRuntimeParsed = $result.Groups["runtime"].Value
    Write-Host "Using latest runtime = $lastRuntimeParsed"
    return $lastRuntimeParsed
}

# MAIN
If (-not $IsMacOS)
{
    Write-Host "This script should only be run on a MacOS." -ForegroundColor Yellow
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
