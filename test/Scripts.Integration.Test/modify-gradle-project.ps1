param(
    [Parameter(Mandatory = $true)][string] $UnityVersion,
    [Parameter(Mandatory = $true)][string] $AndroidSdkRoot,
    [Parameter(Mandatory = $true)][string] $NdkPath
)

. $PSScriptRoot/common.ps1

$workingDirectory = "samples/IntegrationTest/Build/"

# Write SDK and NDK to the local.properties. The referenced Unity built-in SDK & NDK are not available in this job
$localPropertiesPath = Join-Path -Path $workingDirectory -ChildPath "local.properties"
Write-Log "Overwriting 'sdk.dir' and 'ndk.dir' at '$localPropertiesPath'"
$content = @(
    "sdk.dir=$androidSdkRoot"
    "ndk.dir=$ndkPath"
)
$content | Out-File -FilePath $localPropertiesPath -Encoding UTF8

# Starting with Unity 2021 the gradle template has the `ndkPath` now directly set in the build.gradle
# We're removing it to cause it to fall back to the local.properties
If ([int]$UnityVersion -ge 2021)
{
    $gradleFilePaths = @(
        Join-Path -Path $workingDirectory -ChildPath "launcher/build.gradle"
        Join-Path -Path $workingDirectory -ChildPath "unityLibrary/build.gradle"
    )
    
    foreach ($gradleFilePath in $gradleFilePaths)
    {
        if (Test-Path -Path $gradleFilePath)
        {
            Write-Detail "Removing 'ndk path' from: $gradleFilePath"

            $fileContent = Get-Content -Path $gradleFilePath
            $filteredContent = $fileContent | Where-Object { $_ -notmatch 'ndkPath.*AndroidPlayer/NDK"' }
            $filteredContent | Set-Content -Path $gradleFilePath
        }
        else
        {
            Write-Log "File not found: $gradleFilePath" -ForegroundColor Yellow
        }
    }
}

# This is a temporary workaround for build issues with Unity 2022.3. and newer. Unity writes an absolute path to the aapt2 in the gradle.properties file.
# This path is not available on the CI runners, so we're replacing it with the one available on the CI runners.
# https://discussions.unity.com/t/gradle-build-issues-for-android-api-sdk-35-in-unity-2022-3lts/1502187/10
If ([int]$UnityVersion -eq 2022)
{
    Write-Log "Updating aapt2 path..."

    $gradlePropertiesFile = Join-Path -Path $workingDirectory -ChildPath "gradle.properties"
    $fileContent = Get-Content -Path $gradlePropertiesFile

    $aapt2Path = "$androidSdkRoot/build-tools/34.0.0/aapt2"
    if (Test-Path $aapt2Path) 
    {
        Write-Detail "Setting the aapt2 path to: $aapt2Path"

        $updatedContent = $fileContent -replace '/opt/unity/Editor/Data/PlaybackEngines/AndroidPlayer/SDK/build-tools/34.0.0/aapt2', $aapt2Path
        $updatedContent | Set-Content -Path $gradlePropertiesFile
    }
    else 
    {
        Write-Log "aapt2 not found at: $aapt2Path" -ForegroundColor Yellow
    }
}

# Starting with Unity 6 the paths to SDK, NDK, and JDK are written to the `gradle.properties`
# We're removing it to cause it to fall back to the local.properties
If ([int]$UnityVersion -ge 6000)
{
    $gradlePropertiesFile = Join-Path -Path $workingDirectory -ChildPath "gradle.properties"
    Write-Log "Removing 'SDK, NDK, and JDK paths' from: $gradlePropertiesFile"

    $fileContent = Get-Content -Path $gradlePropertiesFile
    $filteredContent = $fileContent | Where-Object { $_ -notmatch 'unity.androidSdkPath|unity.androidNdkPath|unity.jdkPath' }
    $filteredContent | Set-Content -Path $gradlePropertiesFile
}