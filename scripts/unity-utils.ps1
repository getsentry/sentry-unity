$RunUnityLicenseRetryTimeoutSeconds = 3600
$RunUnityLicenseRetryIntervalSeconds = 60

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
    ElseIf ($IsLinux -and "$env:XDG_CURRENT_DESKTOP" -eq "")
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

    $stopwatch = [System.Diagnostics.Stopwatch]::new()
    $stopwatch.Start()

    do
    {
        ClearUnityLog $logFilePath
        New-Item $logFilePath

        Write-Host "Running $unityPath $arguments"
        $process = Start-Process -FilePath $unityPath -ArgumentList $arguments -PassThru

        $stdout = WaitForUnityExit $logFilePath $process

        if ($stdout -match "No valid Unity Editor license found. Please activate your license.")
        {
            Write-Host -ForegroundColor Red "Unity failed because it couldn't acquire a license."
            $timeRemaining = $RunUnityLicenseRetryTimeoutSeconds - $stopwatch.Elapsed.TotalSeconds
            $timeToSleep = $timeRemaining -gt $RunUnityLicenseRetryIntervalSeconds ? $RunUnityLicenseRetryIntervalSeconds : $timeRemaining - 1
            if ($timeToSleep -gt 0)
            {
                Write-Host -ForegroundColor Yellow "Sleeping for $timeToSleep seconds before retrying. Total time remaining: $timeRemaining seconds."
                Start-Sleep -Seconds $timeToSleep
            }
        }
        else
        {
            break
        }
    } while ($stopwatch.Elapsed.TotalSeconds -lt $RunUnityLicenseRetryTimeoutSeconds)

    if ($process.ExitCode -ne 0 -and $env:IgnoreExitCode -ne "true")
    {
        Throw "Unity exited with code $($process.ExitCode)"
    }

    Write-Host -ForegroundColor Green "Unity finished successfully. Time taken: $($stopwatch.Elapsed.ToString('hh\:mm\:ss\.fff'))"
    return $ReturnLogOutput ? $stdout : $null
}

function ClearUnityLog([string] $logFilePath)
{
    Write-Host -NoNewline "Removing Unity log:"
    If (Test-Path -Path $logFilePath)
    {
        #Force is required if it's opened by another process.
        Remove-Item -Path $logFilePath -Force
    }
    Write-Host " OK"
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
