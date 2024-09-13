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