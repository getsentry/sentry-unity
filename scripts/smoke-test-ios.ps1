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

    Write-Host "Building iOS project"
    xcodebuild -project "$XcodeArtifactPath/$ProjectName.xcodeproj" -scheme Unity-iPhone -configuration Release -sdk iphonesimulator -derivedDataPath "$ArchievePath/$ProjectName"
    Write-Host "OK" -ForegroundColor Green

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

ForEach ($device in $deviceList)
{
    Write-Host "Starting Simulator $($device.Name) UUID $($device.UUID)" -ForegroundColor Green
    xcrun simctl boot $($device.UUID)
    Write-Host -NoNewline "Installing Smoke Test on $($device.Name): "
    xcrun simctl install $($device.UUID) "$AppPath"
    Write-Host "OK" -ForegroundColor Green
    Write-Host "Launching SmokeTest on $($device.Name)" -ForegroundColor Green
    $consoleOut = xcrun simctl launch --console-pty $($device.UUID) "io.sentry.samples.unityofbugs"
    
    Write-Host -NoNewline "Smoke test STATUS: "
    $stdout = $consoleOut  | select-string 'SMOKE TEST: PASS'
    If ($stdout -ne $null)
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

Write-Host "Test result"
foreach ($device in $deviceList)
{
    Write-Host -NoNewline "$($device.Name) UUID $($device.UUID): "
    If ($device.TestPassed)
    {
        Write-Host "PASSED" -ForegroundColor Green
    }
    else 
    {        
        Write-Host "FAILED" -ForegroundColor Red
    }
}
Write-Host "End of test."
