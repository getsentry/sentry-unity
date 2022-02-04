param($action, $testArg1, $testArg1Count, $testArg2, $testArg2Count)
Write-Host "Args received action=$action, testArg1=$testArg1, testArg1Count=$testArg1Count, testArg2=$testArg2, testArg2Count=$testArg2Count"
# $action: 'Build' for build only
#          'Test' for Smoke test only
#          null for building and testing
# $testArg1 and $testArg2: test parameter name
#          'Skip' the amount of devices to be skipped the test
#                 null for not skipping any device
#          'Run'  the amount of devices to run the test
$ErrorActionPreference = "Stop"

$XcodeArtifactPath = "samples/artifacts/builds/iOS/Xcode"
$ArchievePath = "$XcodeArtifactPath/archieve"
$ProjectName = "Unity-iPhone"
$AppPath = "$ArchievePath/$ProjectName/Build/Products/Release-IphoneSimulator/unity-of-bugs.app"

Class AppleDevice
{
    [String]$Name
    [String]$UUID
    [boolean]$TestPassed
    [boolean]$TestSkipped

    Parse([String]$unparsedDevice)
    {
        # Example of unparsed device:
        #    iPhone 11 (D3762152-4648-4734-A409-15855F84587A) (Shutdown)
        $unparsedDevice = $unparsedDevice.Trim()
        $result = [regex]::Match($unparsedDevice, "(?<model>.+) \((?<uuid>[0-9A-Fa-f\-]{36})")
        if ($result.Success -eq $False)
        {
            #error
        }
        $this.Name = $result.Groups["model"].Value
        $this.UUID = $result.Groups["uuid"].Value
    }
}

function GetTestSkipCount
{
    If ($testArg1 -eq "Skip" -And $testArg1Count -match "^\d+$")
    {
        return $testArg1Count
    }
    If ($testArg2 -eq "Skip" -And $testArg2Count -match "^\d+$")
    {
        return $testArg2Count
    }
    return 0
}

function GetTestRunCount
{
    If ($testArg1 -eq "Run" -And $testArg1Count -match "^\d+$")
    {
        return $testArg1Count
    }
    If ($testArg2 -eq "Run" -And $testArg2Count -match "^\d+$")
    {
        return $testArg2Count
    }
    return 0
}

function Build()
{

    Write-Host -NoNewline "Applying SmokeTest flag on info.plist: "
    $smokeTestKey = "<key>RunSentrySmokeTest</key>"
    $infoPlist = (Get-Content -path "$XcodeArtifactPath/Info.plist" -Raw)
    If ($infoPlist -clike "*$smokeTestKey*")
    {
        Write-Host "SKIPPED" -ForegroundColor Gray
    }
    Else 
    {        
        $infoPlist.Replace("</dict>`n</plist>", "	$smokeTestKey`n	<string>True</string>`n</dict>`n</plist>") | Set-Content "$XcodeArtifactPath/Info.plist"
        Write-Host "OK" -ForegroundColor Green
    }

    $mapFileParser = "MapFileParser.sh"
    Write-Host -NoNewline "Fixing permission for $mapFileParser : "
    $smokeTestKey = "<key>RunSentrySmokeTest</key>"
    $infoPlist = (Get-Content -path "$XcodeArtifactPath/Info.plist" -Raw)
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
    $xCodeBuild =  Start-Process -FilePath "xcodebuild" -ArgumentList "-project", "$XcodeArtifactPath/$ProjectName.xcodeproj", "-scheme", "Unity-iPhone", "-configuration", "Release", "-sdk", "iphonesimulator", "-derivedDataPath", "$ArchievePath/$ProjectName" -PassThru
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
    # junk will contain the first item from the String that should be == Devices ==
    $deviceLabel , $iOSVersion, $deviceListRaw = xcrun simctl list devices
    [AppleDevice[]]$deviceList = @()
    $iOSVersion = $iOSVersion.Trim("-")
    foreach ($device in $deviceListRaw)
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

    Write-Host "Found $deviceCount devices on version $iOSVersion" -ForegroundColor Green
    ForEach ($device in $deviceList)
    {
        Write-Host "$($device.Name) - $($device.UUID)"
    }

    $skipCount = GetTestSkipCount
    $skippedItems = 0
    $runCount = GetTestRunCount
    $devicesRan = 0
    If (0 -eq $runCount)
    {
        $runCount = $deviceCount
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
        If ($devicesRan -ge $runCount)
        {
            Write-Host "Skipping Simulator $($device.Name) UUID $($device.UUID)" -ForegroundColor Green
            $device.TestSkipped = $true
            continue
        }
        $devicesRan++
        Write-Host "Starting Simulator $($device.Name) UUID $($device.UUID)" -ForegroundColor Green
        xcrun simctl boot $($device.UUID)
        Write-Host -NoNewline "Installing Smoke Test on $($device.Name): "
        xcrun simctl install $($device.UUID) "$AppPath"
        Write-Host "OK" -ForegroundColor Green
        Write-Host "Launching SmokeTest on $($device.Name)" -ForegroundColor Green
        $consoleOut = xcrun simctl launch --console-pty $($device.UUID) "io.sentry.samples.unityofbugs"
        
        Write-Host -NoNewline "Smoke test STATUS: "
        $stdout = $consoleOut  | select-string 'SMOKE TEST: PASS'
        If ($null -ne $stdout)
        {
            Write-Host "PASSED" -ForegroundColor Green
            $device.TestPassed = $True
        }
        Else 
        {
            Write-Host "FAILED" -ForegroundColor Red
            Write-Host "$($device.Name) Console"
            foreach ($consoleLine in $consoleOut)
            {
                Write-Host $consoleLine
            }
            Write-Host " ===== END OF CONSOLE ====="
        }
        Write-Host -NoNewline "Removing Smoke Test from $($device.Name): "
        xcrun simctl uninstall $($device.UUID) "io.sentry.samples.unityofbugs"
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
        If ($device.TestPassed)
        {
            Write-Host "PASSED" -ForegroundColor Green
        }
        ElseIf ($device.TestSkipped)
        {
            Write-Host "SKIPPED" -ForegroundColor Gray
        }
        else 
        {        
            Write-Host "FAILED" -ForegroundColor Red
            $testFailed = $true
        }
    }
    Write-Host "End of test."

    If ($testFailed -eq $true)
    {
        Throw "One or more tests failed."
    }
}

If (-not $IsMacOS)
{
    Write-Host "This script should only be run on a MacOS." -ForegroundColor Yellow
}
# MAIN
If ($null -eq $action -Or $action -eq "Build")
{
    Build
}
If ($null -eq $action -Or $action -eq "Test")
{
    Test
}