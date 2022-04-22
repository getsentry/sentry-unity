$RunUnityLicenseRetryTimeoutSeconds = 3600
$RunUnityLicenseRetryIntervalSeconds = 60
$RunUnityLogFile = 'unity.log'

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

    $arguments += @("-logfile", $RunUnityLogFile)

    $stopwatch = [System.Diagnostics.Stopwatch]::new()
    $stopwatch.Start()

    do
    {
        ClearUnityLog
        Write-Host "Running $unityPath $arguments"
        $process = Start-Process -FilePath $unityPath -ArgumentList $arguments -PassThru

        Write-Host "Waiting for Unity to finish."
        WaitForLogFile 30
        $stdout = SubscribeToUnityLogFile $process

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
            if ($process.ExitCode -ne 0)
            {
                Throw "Unity exited with code $($process.ExitCode)"
            }
            return $stdout
        }
    } while ($stopwatch.Elapsed.TotalSeconds -lt $RunUnityLicenseRetryTimeoutSeconds)
}

function ClearUnityLog
{
    Write-Host -NoNewline "Removing Unity log:"
    If (Test-Path -Path "$RunUnityLogFile")
    {
        #Force is required if it's opened by another process.
        Remove-Item -Path "$RunUnityLogFile" -Force
    }
    Write-Host " OK"
}

function WaitForLogFile
{
    for ($i = 0; $i -lt 30; $i++)
    {
        if (Test-Path -Path "$RunUnityLogFile")
        {
            return
        }
        Write-Host "Waiting for log file to appear: $RunUnityLogFile ..."
        Start-Sleep -Seconds 1
    }
    Throw "Timeout while waiting for the log file to appear: $RunUnityLogFile"
}

function SubscribeToUnityLogFile([System.Diagnostics.Process] $unityProcess)
{
    $unityClosedDelay = 0

    If ($unityProcess -eq $null)
    {
        Throw "Unity process not received"
    }

    $logFileStream = New-Object System.IO.FileStream("$RunUnityLogFile", [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::ReadWrite)
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
