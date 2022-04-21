$ErrorActionPreference = "Stop"

function ProjectRoot
{
    if ($null -ne $Global:NewProjectPathCache)
    {
        return [string]$Global:NewProjectPathCache
    }
    $Global:NewProjectPathCache = (Get-Item .).FullName
    return [string]$Global:NewProjectPathCache
}

function GetTestAppName
{
    param ($buildMethod)

    If ($buildMethod.contains("Mac"))
    {
        return "test.app"
    }
    ElseIf ($buildMethod.contains("Windows"))
    {
        return "test.exe"
    }
    ElseIf ($buildMethod.contains("Linux"))
    {
        return "test"
    }
    ElseIf ($buildMethod.contains("Android"))
    {
        return "test.apk"
    }
    ElseIf ($buildMethod.contains("WebGL"))
    {
        return ""
    }
    Else
    {
        Throw "Cannot find Test App name for the given buildMethod: '$buildMethod'"
    }
}

$NewProjectName = "IntegrationTest"

#New Integration Project paths
$Global:NewProjectPathCache = $null
$NewProjectPath = "$(ProjectRoot)/samples/$NewProjectName"
$NewProjectBuildPath = "$NewProjectPath/Build"
$NewProjectAssetsPath = "$NewProjectPath/Assets"

$UnityOfBugsPath = "$(ProjectRoot)/samples/unity-of-bugs"

$NewProjectLogPath = "$(ProjectRoot)/samples/logfile.txt"

$IntegrationScriptsPath = "$(ProjectRoot)/test/Scripts.Integration.Test"
$PackageReleaseOutput = "$(ProjectRoot)/test-package-release"
$PackageReleaseAssetsPath = "$PackageReleaseOutput/Samples~/unity-of-bugs"

function FormatUnityPath
{
    param ($path)

    If ($path)
    {
        #Ajust path on MacOS
        If ($path -match "Unity.app/$")
        {
            $path = $path + "Contents/MacOS"
        }
        $unityPath = $path
    }
    Else
    {
        Throw "Unity path is required."
    }

    If ($unityPath.StartsWith("docker "))
    {
    }
    ElseIf ($IsMacOS)
    {
        If (-not $unityPath.EndsWith("Contents/MacOS/Unity"))
        {
            $unityPath = $unityPath + "/Unity"
        }
    }
    ElseIf ($IsWindows)
    {
        If (-not $unityPath.EndsWith("Unity.exe"))
        {
            $unityPath = $unityPath + "/Unity.exe"
        }
    }
    ElseIf ($IsLinux)
    {
        If (((Get-Item $unityPath) -is [System.IO.DirectoryInfo]) -and $unityPath.EndsWith("unity"))
        {
            $unityPath = $unityPath + "/Editor/Unity"
        }
    }
    Else
    {
        Throw "Cannot find Unity executable name for the current operating system"
    }

    Write-Host "Unity path is $unityPath"
    return $unityPath
}

function BuildMethodFor([string] $platform)
{
    switch ("$platform")
    {
        "Android" { return "Builder.BuildAndroidIl2CPPPlayer" }
        "MacOS" { return "Builder.BuildMacIl2CPPPlayer" }
        "Windows" { return "Builder.BuildWindowsIl2CPPPlayer" }
        "Linux" { return "Builder.BuildLinuxIl2CPPPlayer" }
        "WebGL" { return "Builder.BuildWebGLPlayer" }
        Default
        {
            If ($IsMacOS)
            {
                return BuildMethodFor "MacOS"
            }
            ElseIf ($IsWindows)
            {
                return BuildMethodFor "Windows"
            }
            ElseIf ($IsLinux)
            {
                return BuildMethodFor "Linux"
            }
            Else
            {
                Throw "Unsupported build platform"
            }
        }
    }
}

function TestDsnFor([string] $platform)
{
    $dsn = "http://publickey@"
    switch ("$platform")
    {
        "Android" { $dsn += "10.0.2.2"; break; }
        "WebGL" { $dsn += "127.0.0.1"; break; }
        Default { $dsn += "localhost" }
    }
    $dsn += ":8000/12345"
    return $dsn
}

function RunUnity([string] $unityPath, [string[]] $arguments)
{
    If ($unityPath.StartsWith("docker "))
    {
        # Fix paths (they're supposed to be the current working directory in the docker container)
        Write-Host "Replacing project root ($(ProjectRoot)) in docker arguments: $arguments"
        $arguments = $arguments | ForEach-Object { $_.Replace("$(ProjectRoot)", "/sentry-unity") }
        # Remove "-batchmode" which ends up being duplicate because the referenced unity-editor script already adds it
        Write-Host "Removing argument '-batchmode' - it would be duplicate and cause a build to fail"
        $arguments = $arguments | Where-Object { $_ –ne "-batchmode" }
        Write-Host "Updated arguments: $arguments"
        # Move docker arguments from $unityPath to $arguments, leaving "docker" as $unityPath
        $arguments = ($unityPath.Split(" ") | Select-Object -Skip 1) + $arguments
        $unityPath = "docker"
    }
    ElseIf ($IsLinux -and "$env:XDG_CURRENT_DESKTOP" -eq "")
    {
        $arguments = @("-ae", "/dev/stdout", "$unityPath") + $arguments
        $unityPath = "xvfb-run"
    }

    ClearUnityLog
    Write-Host "Running $unityPath $arguments"
    $process = Start-Process -FilePath $unityPath -ArgumentList $arguments -PassThru

    Write-Host "Waiting for Unity to finish."
    WaitForLogFile 30
    $stdout = SubscribeToUnityLogFile $process

    if ($process.ExitCode -ne 0)
    {
        Throw "Unity exited with code $($process.ExitCode)"
    }

    return $stdout
}

function ClearUnityLog
{
    Write-Host -NoNewline "Removing Unity log:"
    If (Test-Path -Path "$NewProjectLogPath")
    {
        #Force is required if it's opened by another process.
        Remove-Item -Path "$NewProjectLogPath" -Force
    }
    Write-Host " OK"
}

function WaitForLogFile
{
    $timeout = 30
    Write-Host -NoNewline "Waiting for log file "
    do
    {
        $LogCreated = Test-Path -Path "$NewProjectLogPath"
        Write-Host -NoNewline "."
        $timeout--
        If ($timeout -eq 0)
        {
            Throw "Timeout"
        }
        ElseIf (!$LogCreated) #validate
        {
            Start-Sleep -Seconds 1
        }
    } while ($LogCreated -ne $true)
    Write-Host " OK"
}

function SubscribeToUnityLogFile([System.Diagnostics.Process] $unityProcess)
{
    $unityClosedDelay = 0

    If ($unityProcess -eq $null)
    {
        Throw "Unity process not received"
    }

    $logFileStream = New-Object System.IO.FileStream("$NewProjectLogPath", [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::ReadWrite)
    If (-not $logFileStream)
    {
        Throw "Failed to open logfile on $NewProjectLogPath"
    }
    $logStreamReader = New-Object System.IO.StreamReader($LogFileStream)
    do
    {
        $line = $logStreamReader.ReadLine()

        If ($line -eq $null)
        {
            Start-Sleep -Milliseconds 400
        }
        Else
        {
            $dateNow = Get-Date -Format "HH:mm:ss.fff"
            #print line as normal/errored/warning
            If ($null -ne ($line | Select-String "\bERROR\b|\bFailed\b"))
            {
                $color = 'Red'
            }
            ElseIf ($null -ne ($line | Select-String "\bWARNING\b|\bLine:"))
            {
                $color = 'Yellow'
            }
            Else
            {
                $color = (Get-Host).ui.rawui.ForegroundColor
            }
            Write-Host "$dateNow | $line" -ForegroundColor $color
            Write-Output "$dateNow | $line"
        }
        # Unity is closed but logfile wasn't updated in time.
        # Adds additional delay to wait for the last lines.
        If ($UnityProcess.HasExited)
        {
            $unityClosedDelay++
        }
    } while ($unityClosedDelay -le 2 -or $line -ne $null)
    $logStreamReader.Dispose()
    $logFileStream.Dispose()
}
