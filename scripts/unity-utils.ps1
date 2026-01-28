$RunUnityLicenseRetryTimeoutSeconds = 3600
$RunUnityLicenseRetryIntervalSeconds = 60

function FindNewestUnity()
{
    if ($IsWindows)
    {
        $hubPath = "C:\Program Files\Unity\Hub\Editor\"
    }
    elseif ($IsMacOS)
    {
        $hubPath = "/Applications/Unity/Hub/Editor/"
    }
    elseif ($IsLinux)
    {
        $hubPath = "$env:HOME/Unity/Hub/Editor/"
    }
    else
    {
        throw "Unsupported platform for Unity auto-detection"
    }

    if (-not (Test-Path $hubPath))
    {
        throw "Unity Hub Editor folder not found at: $hubPath"
    }

    $unityVersions = Get-ChildItem $hubPath -Directory | Select-Object -ExpandProperty Name
    if ($unityVersions.Count -eq 0)
    {
        throw "No Unity versions found in: $hubPath"
    }

    $unityVersion = $unityVersions | Sort-Object -Descending | Select-Object -First 1

    if ($IsWindows)
    {
        $unityPath = "${hubPath}${unityVersion}\Editor\Unity.exe"
    }
    elseif ($IsMacOS)
    {
        $unityPath = "${hubPath}${unityVersion}/Unity.app/Contents/MacOS/Unity"
    }
    else
    {
        $unityPath = "${hubPath}${unityVersion}/Editor/Unity"
    }

    Write-Host "Auto-detected Unity $unityVersion at: $unityPath"
    return $unityPath
}

function RunUnity([string] $unityPath, [string[]] $arguments, [switch] $ReturnLogOutput)
{
    If ($unityPath.StartsWith("docker "))
    {
        # Move docker arguments from $unityPath to $arguments, leaving "docker" as $unityPath
        $arguments = ($unityPath.Split(" ") | Select-Object -Skip 1) + $arguments
        $unityPath = "docker"
    }

    If ($unityPath -eq "docker")
    {
        # Remove "-batchmode" which ends up being duplicate because the referenced unity-editor script already adds it
        Write-Host "Removing argument '-batchmode' - it would be duplicate and cause a build to fail"
        $arguments = $arguments | Where-Object { $_ –ne "-batchmode" }
        Write-Host "Updated arguments: $arguments"
    }
    ElseIf ($IsLinux -and "$env:XDG_CURRENT_DESKTOP" -eq "" -and $unityPath -ne "xvfb-run")
    {
        $arguments = @("-ae", "/dev/stdout", "$unityPath") + $arguments
        $unityPath = "xvfb-run"
    }

    $logFilePath = "unity.log"

    foreach ($arg in $arguments)
    {
        if ($arg -ieq "logfile" -or $arg -ieq "-logfile")
        {
            # Note: if we really needed to use a custom value: get the next arg and overwrite $logFilePath.
            #       The only issue then would be if it was stdout ('-').
            throw "You must not pass the 'logfile' argument - this script needs to set it to a custom value."
        }
    }

    $arguments += @("-logfile", $logFilePath)

    $stopwatchTotal = [System.Diagnostics.Stopwatch]::new()
    $stopwatchTotal.Start()
    $stopwatch = [System.Diagnostics.Stopwatch]::new()
    $stdout = ""
    $try = 1
    do
    {
        $stopwatch.Restart()
        $runInfo = ($try -gt 1) ? "Retry #$try run" : 'Run'
        Write-Host "::group::$runInfo $unityPath $arguments"
        try
        {
            ClearUnityLog $logFilePath
            New-Item $logFilePath > $null

            $process = Start-Process -FilePath $unityPath -ArgumentList $arguments -PassThru

            $stdout = WaitForUnityExit $logFilePath $process
        }
        finally
        {
            Write-Host "::endgroup::"
        }
        
        $hasLicenseError = $stdout -match "No valid Unity Editor license found. Please activate your license."
        # In Unity 6.0, building on mobile with no license available errors with "unsuppored". Retrying works :)
        $hasUnsupportedTargetError = $stdout -match "Error building player because build target was unsupported"
        
        if ($hasLicenseError -or $hasUnsupportedTargetError)
        {
            $msg = if ($hasLicenseError) { "Unity failed because it couldn't acquire a license." } else { "Unity failed because build target was unsupported but it's probably a license issue." }
            $timeRemaining = $RunUnityLicenseRetryTimeoutSeconds - $stopwatchTotal.Elapsed.TotalSeconds
            $timeToSleep = $timeRemaining -gt $RunUnityLicenseRetryIntervalSeconds ? $RunUnityLicenseRetryIntervalSeconds : $timeRemaining - 1
            if ($timeToSleep -gt 0)
            {
                Write-Host -ForegroundColor Yellow "$msg Sleeping for $timeToSleep seconds before retrying. Total time remaining: $timeRemaining seconds."
                Start-Sleep -Seconds $timeToSleep
                $try = $try + 1
            }
            else
            {
                Write-Host "::error::$msg"
                Throw $msg
            }
        }
        else
        {
            break
        }
    } while ($stopwatchTotal.Elapsed.TotalSeconds -lt $RunUnityLicenseRetryTimeoutSeconds)

    if ($process.ExitCode -ne 0 -and $env:IgnoreExitCode -ne "true")
    {
        Throw "Unity exited with code $($process.ExitCode)"
    }

    $timeTaken = "Time taken: $($stopwatch.Elapsed.ToString('hh\:mm\:ss\.fff'))"
    Write-Host -ForegroundColor Green "Unity finished successfully. $timeTaken"
    if ($try -gt 3)
    {
        Write-Host "::$($try -gt 10 ? 'warning' : 'notice')::It took $try tries to successfully acquire a Unity license. Total time taken: $($stopwatchTotal.Elapsed.ToString('hh\:mm\:ss\.fff'))"
    }
    return $ReturnLogOutput ? $stdout : $null
}

function ClearUnityLog([string] $logFilePath)
{
    Write-Host "Removing Unity log $logFilePath"
    If (Test-Path -Path $logFilePath)
    {
        #Force is required if it's opened by another process.
        Remove-Item -Path $logFilePath -Force
    }
}

function WaitForUnityExit([string] $RunUnityLogFile, [System.Diagnostics.Process] $unityProcess)
{
    Write-Host "Waiting for Unity to finish."
    $unityClosedDelay = 0

    If ($unityProcess -eq $null)
    {
        Throw "Unity process not received"
    }

    $logFileStream = New-Object System.IO.FileStream($RunUnityLogFile, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::ReadWrite)
    If (-not $logFileStream)
    {
        Throw "Failed to open logfile on $RunUnityLogFile"
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
                Write-Host "$dateNow | $line" -ForegroundColor Red
            }
            ElseIf ($null -ne ($line | Select-String "\bWARNING\b|\bLine:"))
            {
                Write-Host "$dateNow | $line" -ForegroundColor Yellow
            }
            Else
            {
                Write-Host "$dateNow | $line"
            }
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
