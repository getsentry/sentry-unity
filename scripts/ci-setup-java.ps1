param(
    [Parameter(Mandatory=$true)]
    [string]$UnityVersion
)

function Test-JavaPath {
    param($Path, $Version)
    if (-not (Test-Path $Path)) {
        throw "Java $Version path not found at: $Path."
    }
}

switch -Regex ($UnityVersion) {
    "2019" {
        Test-JavaPath $env:JAVA_HOME_8_X64 "8"
        $javaHome = $env:JAVA_HOME_8_X64
        Write-Host "Using Java 8 for Unity 2019"
    }
    "2022" {
        Test-JavaPath $env:JAVA_HOME_11_X64 "11"
        $javaHome = $env:JAVA_HOME_11_X64
        Write-Host "Using Java 11 for Unity 2022"
    }
    "6000" {
        Test-JavaPath $env:JAVA_HOME_17_X64 "17"
        $javaHome = $env:JAVA_HOME_17_X64
        Write-Host "Using Java 17 for Unity 6"
    }
    default {
        throw "Unexpected Unity version: $UnityVersion"
    }
}

Write-Host "Selected JAVA_HOME: $javaHome"
"JAVA_HOME=$javaHome" >> $env:GITHUB_ENV
