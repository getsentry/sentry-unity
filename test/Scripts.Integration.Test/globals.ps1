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
    ElseIf ($buildMethod.contains("AndroidIl2CPPPlayer"))
    {
        return "test.apk"
    }
    ElseIf ($buildMethod.contains("AndroidIl2CPPProject") -or $buildMethod.contains("IOS") -or $buildMethod.contains("WebGL"))
    {
        return ""
    }
    ElseIf ($buildMethod.contains("Switch"))
    {
        return "test"
    }
    Else
    {
        Throw "Cannot determine Test App name for the given buildMethod: '$buildMethod'"
    }
}

function GetNewProjectName
{
    return "IntegrationTest$Global:UnityVersionInUse"
}

function GetNewProjectPath
{
    return "$(ProjectRoot)/samples/$(GetNewProjectName)"
}

function GetNewProjectBuildPath
{
    return "$(GetNewProjectPath)/Build"
}

function GetNewProjectAssetsPath
{
    return "$(GetNewProjectPath)/Assets"
}

$Global:NewProjectPathCache = $null
$Global:UnityVersionInUse = $null

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
        "Android-Export" { return "Builder.BuildAndroidIl2CPPProject" }
        "MacOS" { return "Builder.BuildMacIl2CPPPlayer" }
        "Windows" { return "Builder.BuildWindowsIl2CPPPlayer" }
        "Linux" { return "Builder.BuildLinuxIl2CPPPlayer" }
        "WebGL" { return "Builder.BuildWebGLPlayer" }
        "iOS" { return "Builder.BuildIOSProject" }
        "Switch" { return "Builder.BuildSwitchIL2CPPPlayer" }
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
