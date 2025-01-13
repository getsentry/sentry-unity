param(
    [Parameter(Mandatory = $true)][string] $UnityVersion,
    [Parameter(Mandatory = $true)][string] $AndroidSdkRoot,
    [Parameter(Mandatory = $true)][string] $NdkPath
)

$workingDirectory = "samples/IntegrationTest/Build/"

# Write SDK and NDK to the local.properties. The referenced Unity built-in SDK & NDK are not available in this job
$localPropertiesPath = Join-Path -Path $workingDirectory -ChildPath "local.properties"
Write-Host "Overwriting 'sdk.dir' and 'ndk.dir' at '$localPropertiesPath'"
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
            Write-Output "Removing 'ndk path' from: $gradleFilePath"

            $fileContent = Get-Content -Path $gradleFilePath
            $filteredContent = $fileContent | Where-Object { $_ -notmatch 'ndkPath.*AndroidPlayer/NDK"' }
            $filteredContent | Set-Content -Path $gradleFilePath
        }
        else
        {
            Write-Output "File not found: $gradleFilePath"
        }
    }
}

# This is a temporary workaround for build issues with Unity 2022.3. and newer. Unity writes an absolute path to the aapt2 in the gradle.properties file.
# This path is not available on the CI runners, so we're replacing it with the one available on the CI runners.
# https://discussions.unity.com/t/gradle-build-issues-for-android-api-sdk-35-in-unity-2022-3lts/1502187/10
If ([int]$UnityVersion -eq 2022)
{
    Write-Output "Updating aapt2 path."

    $gradlePropertiesFile = Join-Path -Path $workingDirectory -ChildPath "gradle.properties"
    $fileContent = Get-Content -Path $gradlePropertiesFile

    $aapt2Path = "$androidSdkRoot/build-tools/34.0.0/aapt2"
    if (Test-Path $aapt2Path) 
    {
        Write-Output "Setting the aapt2 path to: $aapt2Path"

        $updatedContent = $fileContent -replace '/opt/unity/Editor/Data/PlaybackEngines/AndroidPlayer/SDK/build-tools/34.0.0/aapt2', $aapt2Path
        $updatedContent | Set-Content -Path $gradlePropertiesFile
    }
    else 
    {
        Write-Output "aapt2 not found at: $aapt2Path"
    }
}

# Starting with Unity 6 the paths to SDK, NDK, and JDK are written to the `gradle.properties`
# We're removing it to cause it to fall back to the local.properties
If ([int]$UnityVersion -ge 6000)
{
    Write-Output "Removing 'SDK, NDK, and JDK paths' from: $gradleFilePath"

    $gradlePropertiesFile = Join-Path -Path $workingDirectory -ChildPath "gradle.properties"

    $fileContent = Get-Content -Path $gradlePropertiesFile
    $filteredContent = $fileContent | Where-Object { $_ -notmatch 'unity.androidSdkPath|unity.androidNdkPath|unity.jdkPath' }
    $filteredContent | Set-Content -Path $gradlePropertiesFile
}