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
    ElseIf ($buildMethod.contains("Linux")) {
        return "test"
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

$UnityOfBugsPath =  "$(ProjectRoot)/samples/unity-of-bugs"

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

    If ($IsMacOS)
    {
        If ($unityPath.EndsWith("Contents/MacOS/Unity") -eq $false)
        {
            $unityPath = $unityPath + "/Unity"
        }
    }
    ElseIf ($IsWindows)
    {
        If ($unityPath.EndsWith("Unity.exe") -eq $false)
        {
            $unityPath = $unityPath +  "/Unity.exe"
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

function RunUnity([string] $unityPath, [string[]] $arguments)
{
    If ($IsLinux -and "$env:XDG_CURRENT_DESKTOP" -eq "")
    {
        $arguments = @("-ae", "/dev/stdout", "$unityPath") + $arguments
        $unityPath = "xvfb-run"
    }
    return Start-Process -FilePath $unityPath -ArgumentList $arguments -PassThru
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

function SubscribeToUnityLogFile()
{
    param (
        [Parameter(Mandatory=$true, Position=0)]
        [System.Diagnostics.Process]$unityProcess,
        [Parameter(Mandatory=$false, Position=1)]
        [string] $SuccessString,
        [Parameter(Mandatory=$false, Position=2)]
        [string]$FailString
    )

    $returnCondition = $null
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
            $dateNow = Get-Date -UFormat "%T %Z"

            #print line as normal/errored/warning
            If ($null -ne ($line | select-string "ERROR | Failed "))
            {
                # line contains "ERROR " OR " Failed "
                Write-Host "$dateNow - $line" -ForegroundColor Red
            }
            ElseIf ($null -ne ($line | Select-String "WARNING | Line:"))
            {
                Write-Host "$dateNow - $line" -ForegroundColor Yellow
            }
            Else
            {
                $Host.UI.WriteLine("$dateNow - $line")
            }

            If ($SuccessString -and ($line | Select-String $SuccessString))
            {
                $returnCondition = $line
            }
            ElseIf($FailString -and ($line | Select-String $FailString))
            {
                $returnCondition = $line
            }
        }
        # Unity is closed but logfile wasn't updated in time.
        # Adds additional delay to wait for the last lines.
        If ($UnityProcess.HasExited)
        {
            $unityClosedDelay++
        }
    } while ($unityClosedDelay -le 2  -or $line -ne $null)
    $logStreamReader.Dispose()
    $logFileStream.Dispose()
    return $returnCondition
}
