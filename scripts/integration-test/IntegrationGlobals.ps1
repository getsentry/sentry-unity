$Global:UnityPath = $null

function ProjectRoot {
    if ($Global:NewProjectPathCache -ne $null)
    {
        return [string]$Global:NewProjectPathCache
    }
    $Global:NewProjectPathCache = (Get-Item .).FullName
    return [string]$Global:NewProjectPathCache
}

function GetLineBreakMode {
    If ($IsWindows)
    {
        return "`r`n"
    }
    return   "`n"
}

function GetTestAppName {
    If ($IsMacOS)
    {
        return "test.app"
    }
    ElseIf($IsWindows)
    {
        return "test.exe"
    }
    Else
    {
        Throw "Unsupported build"
    }
}

function GetUnityName {
    If ($IsMacOS)
    {
        return "Unity"
    }
    ElseIf($IsWindows)
    {
        return "Unity.exe"
    }
    Else
    {
        Throw "Unsupported build"
    }
}

$LineBreak = $(GetLineBreakMode)

$Unity = "$(GetUnityName)"
$NewProjectName = "IntegrationTest"
$LogFile = "logfile.txt"
$SolutionFile = "IntegrationTest.sln"

#New Integration Project paths
$Global:NewProjectPathCache = $null
$NewProjectPath = "$(ProjectRoot)/samples/$NewProjectName"
$NewProjectEditorBuildSettingsPath = "$NewProjectPath/ProjectSettings"
$NewProjectBuildPath = "$NewProjectPath/Build"
$NewProjectAssetsPath = "$NewProjectPath/Assets"
$NewProjectSettingsPath = "$NewProjectPath/ProjectSettings"

$UnityOfBugsPath =  "$(ProjectRoot)/samples/unity-of-bugs"

$NewProjectLogPath = "$(ProjectRoot)/samples"
$Global:TestApp = "$(GetTestAppName)"

$IntegrationScriptsPath = "$(ProjectRoot)/scripts/integration-test"
$PackageReleaseOutput = "$(ProjectRoot)/package-release"

$Timeout = 30

function ShowIntroAndValidateRequiredPaths {
    param ( $showIntro, $stepName, $path)

    if ($showIntro -eq "True") {
        Write-Output "                                         "
        Write-Output " #  # #   # # ####### #     #            "
        Write-Output " #  # ##  # #    #     #   #  INTEGRATION"
        Write-Output " #  # # # # #    #      ###  TEST        "
        Write-Output " #  # #  ## #    #       #               "
        Write-Output "  ##  #   # #    #       #               "
        Write-Output "                                         "
        Write-Output "  $stepName"
        Write-Output " =====================================  "
    }

    If ($Global:UnityPath)
    {
        #Already set.
    }
    ElseIf ($path)
    {
        #Ajust path on MacOS
        If ($path -match "Unity.app/$")
        {
            $path = $path + "Contents/MacOS"
        }
        $Global:UnityPath = $path
    }
    Else
    {
        Throw "Unity path is required."
    }

    Write-Output "Unity path is $Global:UnityPath"
}

function ClearUnityLog {
    Write-Host -NoNewline "Removing Unity log:"
    If (Test-Path -Path "$NewProjectLogPath/$LogFile" ) 
    {
        Remove-Item -Path "$NewProjectLogPath/$LogFile" -Force -Recurse -ErrorAction Stop
    }
    Write-Output " OK"
}

function WaitLogFileToBeCreated {
    param (
        $Timeout
    )
    if ($Timeout -eq $null){
        Write-Output "Timeout not set, using 30 seconds as default"
        $Timeout = 30
    }
    Write-Host -NoNewline "Waiting for log file "
    do
    {
        Start-Sleep -Seconds 1
        $LogCreated = Test-Path -Path "$NewProjectLogPath/$LogFile"
        Write-Host -NoNewline "."
        $Timeout--
        if ($Timeout -eq 0)
        {
            Throw "Timeout"
        }
    } while ($LogCreated -ne "True")
    Write-Output " OK"
}

function TrackCacheUntilUnityClose() {
    param (
        [Parameter(Mandatory=$true, Position=0)]
        [System.Diagnostics.Process]$UnityProcess,        
        [Parameter(Mandatory=$false, Position=1)]
        [string] $SuccessString,
        [Parameter(Mandatory=$false, Position=2)]
        [string]$FailString
    )

    $returnCondition = $null
    $unityClosed = $null

    if ($UnityProcess -eq $null)
    {
        Throw "Unity process not received"
    }

    $logFileStream = New-Object System.IO.FileStream("$NewProjectLogPath/$LogFile", [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::ReadWrite) -ErrorAction Stop
    If ($logFileStream) {}
    Else
    {
        Throw "Failed to open logfile on $NewProjectLogPath/$LogFile"
    }
    $logStreamReader = New-Object System.IO.StreamReader($LogFileStream) -ErrorAction Stop

    do
    {  

        if ($logFileStream.Length -eq $logFileStream.Position)
        {
            Start-Sleep -Milliseconds 100
        }
        else
        {
            $lines = $logStreamReader.ReadToEnd()
            $dateNow = Get-Date -UFormat "%T %Z"

            ForEach ($line in $($lines -split $LineBreak))
            {
                $stdout = $line
                If (($stdout | select-string "ERROR |Failed ") -ne $null)
                {
                    Write-Host "$dateNow - $stdout" -ForegroundColor Red
                }
                ElseIf (($stdout | Select-String "WARNING | Line:") -ne $null)
                {
                    Write-Host "$dateNow - $stdout" -ForegroundColor Yellow
                }
                Else
                {
                    $Host.UI.WriteLine("$dateNow - $stdout")
                }

                #check for condition
                If ($SuccessString -and ($stdout | Select-String $SuccessString))
                {
                    $returnCondition = $stdout
                }
                ElseIf($FailString -and ($stdout | Select-String $FailString))
                {
                    $returnCondition = $stdout
                }
            }

            If ($UnityProcess.HasExited -and !$unityClosed) 
            {
                $currentPos = $logFileStream.Position
                $logStreamReader.Dispose()
                $logFileStream.Dispose()
                
                Start-Sleep -Milliseconds 1000

                $logFileStream = New-Object System.IO.FileStream("$NewProjectLogPath/$LogFile", [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::ReadWrite) -ErrorAction Stop
                $logFileStream.Seek($currentPos, [System.IO.SeekOrigin]::Begin)
                $logStreamReader = New-Object System.IO.StreamReader($LogFileStream) -ErrorAction Stop

                $unityClosed = "True"
            }
          
        }

    } while (!$UnityProcess.HasExited -or $logFileStream.Length -ne $logFileStream.Position)
    $logStreamReader.Dispose()
    $logFileStream.Dispose()
    return $returnCondition
}

function WaitProgramToClose {
    param ( $process )

    If ($process -eq $null) 
    {
        Throw "Process not found."
    }
    $Timeout = 60 * 2
    $processName = $process.Name
    Write-Host -NoNewline "Waiting for $processName"

    While (!$process.HasExited -and $processName -gt 0) 
    {
        Start-Sleep -Milliseconds 500
        Write-Host -NoNewline "."
        $cacheHandle = $process.Handle
        $Timeout = $Timeout - 1
    }
}