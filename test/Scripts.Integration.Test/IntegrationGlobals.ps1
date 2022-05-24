$ErrorActionPreference = "Stop"

. $PSScriptRoot/../../scripts/unity-utils.ps1

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
    ElseIf ($buildMethod.contains("IOS"))
    {
        return ""
    }
    ElseIf ($buildMethod.contains("WebGL"))
    {
        return ""
    }
    Else
    {
        Throw "Cannot determine Test App name for the given buildMethod: '$buildMethod'"
    }
}

$NewProjectName = "IntegrationTest"

#New Integration Project paths
$Global:NewProjectPathCache = $null
$NewProjectPath = "$(ProjectRoot)/samples/$NewProjectName"
$NewProjectBuildPath = "$NewProjectPath/Build"
$NewProjectAssetsPath = "$NewProjectPath/Assets"

$UnityOfBugsPath = "$(ProjectRoot)/samples/unity-of-bugs"

$IntegrationScriptsPath = "$(ProjectRoot)/test/Scripts.Integration.Test"
$PackageReleaseOutput = "$(ProjectRoot)/test-package-release"
$PackageReleaseAssetsPath = "$PackageReleaseOutput/Samples~/unity-of-bugs"

function FormatUnityPath
{
    param ($path)

    If ($path)
    {
        #Ajust path on MacOS
        If ($path -match "Unity.app/?$")
        {
            $path = $path + "/Contents/MacOS"
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
        "iOS" { return "Builder.BuildIOSPlayer" }
        ""
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
        Default { Throw "Unsupported build platform: '$platform'" }
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

function SymbolServerUrlFor([string] $UnityPath)
{
    $UnityPath.StartsWith("docker ") ? 'http://172.17.0.1:8000' : 'http://localhost:8000'
}

function RunUnityCustom([string] $unityPath, [string[]] $arguments, [switch] $ReturnLogOutput)
{
    If ($unityPath.StartsWith("docker "))
    {
        # Fix paths (they're supposed to be the current working directory in the docker container)
        Write-Host "Replacing project root ($(ProjectRoot)) in docker arguments: $arguments"
        $arguments = $arguments | ForEach-Object { $_.Replace("$(ProjectRoot)", "/sentry-unity") }

    }

    return RunUnity $unityPath $arguments -ReturnLogOutput:$ReturnLogOutput
}

function CheckSymbolServerOutput([string] $buildMethod, [string] $symbolServerOutput)
{
    $expectedFiles = @()
    If ($buildMethod.contains('Mac'))
    {
        throw 'Not implemented'
    }
    ElseIf ($buildMethod.contains('Windows'))
    {
        $expectedFiles = @(
            'test.exe',
            'GameAssembly.dll',
            'GameAssembly.pdb',
            'UnityPlayer.dll',
            'sentry.pdb',
            'sentry.dll'
        )
    }
    ElseIf ($buildMethod.contains('Linux'))
    {
        $expectedFiles = @(
            'test',
            'test_s.debug',
            'GameAssembly.so',
            'UnityPlayer.so',
            'UnityPlayer_s.debug',
            'libsentry.dbg.so'
        )
    }
    ElseIf ($buildMethod.contains('Android'))
    {
        $expectedFiles = @(
            'libmain.so',
            'libunity.so',
            'libil2cpp.so',
            'libil2cpp.dbg.so',
            'libil2cpp.sym.so',
            'libsentry.so',
            'libsentry-android.so'
        )
    }
    ElseIf ($buildMethod.contains('IOS'))
    {
        throw 'Not implemented'
    }
    ElseIf ($buildMethod.contains('WebGL'))
    {
        Write-Host 'No symbols are uploaded for WebGL - nothing to test.' -ForegroundColor Yellow
        return
    }
    Else
    {
        Throw "Cannot CheckSymbolServerOutput() for an unknown buildMethod: '$buildMethod'"
    }

    Write-Host 'Verifying debug symbol upload...'
    $successful = $true
    foreach ($name in $expectedFiles)
    {
        if ($symbolServerOutput -match "Received: .* $([Regex]::Escape($name))\b")
        {
            Write-Host "  $name - OK"
        }
        else
        {
            $successful = $false
            Write-Host "  $name - MISSING" -ForegroundColor Red
        }
    }
    if ($successful)
    {
        Write-Host 'All expected debug symbols have been uploaded' -ForegroundColor Green
    }
    else
    {
        exit 1
    }
}