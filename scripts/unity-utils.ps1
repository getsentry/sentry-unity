$RunUnityLicenseRetryTimeoutSeconds = 1800
$RunUnityLicenseRetryIntervalMinSeconds = 20
$RunUnityLicenseRetryIntervalMaxSeconds = 90
$ClearUnityLogTimeoutSeconds = 30
$RunUnityRateLimitRetrySleepSeconds = 90

# Returns the name of a known infra failure worth one retry, or $null.
function GetUnityRetrySignature([string] $unityPath, [string[]] $arguments, [int] $exitCode, [string[]] $stdout)
{
    if ($exitCode -eq 0)
    {
        return $null
    }

    # Unity 6.6 kills the PID in a stale Library/burst.pid at startup, which in a fresh container can be its own xvfb-run.
    if ($exitCode -eq 137)
    {
        return 'killed'
    }

    # xvfb-run exits 5 instead of 137 when it fails to clean up after the kill. Unity never got to log its exit.
    if ($exitCode -eq 5 -and $unityPath -in @('docker', 'xvfb-run') `
            -and -not ($stdout -match 'Cleanup mono|Exiting batchmode|Aborting batchmode|Application will (exit|terminate) with return code|Caught fatal signal'))
    {
        return 'killed'
    }

    # Unity gives the LicensingClient it just launched 5s to open its IPC channel. The next launch finds it running.
    if ($exitCode -eq 199 -and ($stdout -match "IPC channel to LicensingClient doesn't exist; aborting"))
    {
        return 'licensing-ipc'
    }

    # A crash while running tests may be an SDK bug.
    if ($arguments -contains '-runTests')
    {
        return $null
    }

    if ($stdout -match 'Received status code 429 from server')
    {
        return 'gradle-429'
    }

    # Bee reports a segfaulting build tool, e.g. il2cpp, as its exit code.
    if ($stdout -match '\| 139 \(Segmentation fault\)')
    {
        return 'tool-segfault'
    }

    if (($stdout -match 'Batchmode quit successfully invoked') -and ($stdout -match 'Caught fatal signal'))
    {
        return 'shutdown-crash'
    }

    return $null
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
    $licenseTry = 1
    $retried = @{}
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
        
        if ($hasLicenseError)
        {
            $msg = "Unity failed because it couldn't acquire a license."
            $timeRemaining = $RunUnityLicenseRetryTimeoutSeconds - $stopwatchTotal.Elapsed.TotalSeconds
            $interval = Get-Random -Minimum $RunUnityLicenseRetryIntervalMinSeconds -Maximum ($RunUnityLicenseRetryIntervalMaxSeconds + 1)
            $timeToSleep = $timeRemaining -gt $interval ? $interval : $timeRemaining - 1
            if ($timeToSleep -gt 0)
            {
                Write-Host -ForegroundColor Yellow "$msg Sleeping for $timeToSleep seconds before retrying. Total time remaining: $timeRemaining seconds."
                Start-Sleep -Seconds $timeToSleep
                $try = $try + 1
                $licenseTry = $licenseTry + 1
            }
            else
            {
                $msg = "Unity couldn't acquire a license after $licenseTry tries in $RunUnityLicenseRetryTimeoutSeconds seconds. The floating license entitlements are likely exhausted (no free Unity license seat)."
                Write-Host "::error::$msg"
                Throw $msg
            }
        }
        ElseIf (($signature = GetUnityRetrySignature $unityPath $arguments $process.ExitCode $stdout) -and -not $retried[$signature])
        {
            Write-Host "::warning::Unity failed with exit code $($process.ExitCode) (signature: $signature). Retrying once."
            $retried[$signature] = $true
            if ($signature -eq 'gradle-429')
            {
                Start-Sleep -Seconds $RunUnityRateLimitRetrySleepSeconds
            }
            $try = $try + 1
        }
        else
        {
            break
        }
    } while ($true)

    if ($process.ExitCode -ne 0 -and $env:IgnoreExitCode -ne "true")
    {
        Throw "Unity exited with code $($process.ExitCode)"
    }

    $timeTaken = "Time taken: $($stopwatch.Elapsed.ToString('hh\:mm\:ss\.fff'))"
    Write-Host -ForegroundColor Green "Unity finished successfully. $timeTaken"
    if ($licenseTry -gt 3)
    {
        Write-Host "::$($licenseTry -gt 10 ? 'warning' : 'notice')::It took $licenseTry tries to successfully acquire a Unity license. Total time taken: $($stopwatchTotal.Elapsed.ToString('hh\:mm\:ss\.fff'))"
    }
    return $ReturnLogOutput ? $stdout : $null
}

function ClearUnityLog([string] $logFilePath)
{
    Write-Host "Removing Unity log $logFilePath"

    # A Unity process that has just exited can still hold a handle to its log file for a
    # short while. I.e. child processes it spawned like the licensing client inherit the
    # handle and outlive it.
    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    while ($true)
    {
        If (-not (Test-Path -Path $logFilePath))
        {
            return
        }

        try
        {
            Remove-Item -Path $logFilePath -Force -ErrorAction Stop
            return
        }
        catch
        {
            if ($stopwatch.Elapsed.TotalSeconds -ge $ClearUnityLogTimeoutSeconds)
            {
                Throw "Failed to remove '$logFilePath' after $ClearUnityLogTimeoutSeconds seconds - it is still in use by another process. $($_.Exception.Message)"
            }

            Write-Host "  '$logFilePath' is still in use by another process, retrying..."
            Start-Sleep -Milliseconds 500
        }
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
