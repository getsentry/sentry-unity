param(
    [Parameter(Mandatory = $true)][string] $UnityVersion,
    [string] $Modules = ''
)

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true

$CliVersion = '1.0.0-beta.14'
$CliBinaries = @{
    'darwin-arm64' = @('unity-darwin-arm64', '367ea124c61e91dc22409699cb40d2e65d23952f3220e911ccea1f6d8a1ff37f')
    'linux-x64'    = @('unity-linux-x64', 'd9a2c1716ce723b970898f7f40adc09cceee04e84ce2f1f1638f07ac3e9898aa')
    'win32-x64'    = @('unity-windows-x64.exe', '78761a0547cd4a9bc212e27a217c66e6a621bf48c753ba440e4b2327ee7ff8ac')
}

$unity = (Get-Content "$PSScriptRoot/unity-versions.json" -Raw | ConvertFrom-Json).$UnityVersion
if (-not $unity) { throw "Unity version '$UnityVersion' not found in scripts/unity-versions.json" }
$moduleList = -split $Modules

$env:UNITY_NON_INTERACTIVE = '1'
$env:UNITY_NO_BANNER = '1'
$env:UNITY_NO_PAGER = '1'

# Unity CLI, pinned by version and checksum.
$os = $IsWindows ? 'win32' : $IsMacOS ? 'darwin' : 'linux'
$arch = "$([System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture)".ToLower()
$cliFile, $cliSha = $CliBinaries["$os-$arch"]
if (-not $cliFile) { throw "No Unity CLI binary pinned for $os-$arch" }

$cliDir = Join-Path ($env:RUNNER_TEMP ?? [System.IO.Path]::GetTempPath()) 'unity-cli'
New-Item -ItemType Directory -Force -Path $cliDir | Out-Null
$cli = Join-Path $cliDir ($IsWindows ? 'unity.exe' : 'unity')
Invoke-WebRequest -Uri "https://public-cdn.cloud.unity3d.com/hub/prod/cli/$CliVersion/$cliFile" -OutFile $cli
if ((Get-FileHash $cli -Algorithm SHA256).Hash -ne $cliSha) { throw "Unity CLI checksum mismatch for $cliFile" }
if (-not $IsWindows) { chmod +x $cli }

$installArgs = @('install', $unity.version, '--changeset', $unity.changeset, '--child-modules', '--accept-eula', '--yes')
if ($moduleList) { $installArgs += @('--module') + $moduleList }
& $cli @installArgs

$editor = (& $cli editors --installed --verbose --json | ConvertFrom-Json).data | Where-Object version -EQ $unity.version | Select-Object -First 1
if (-not $editor) { throw "Unity $($unity.version) not listed after install" }
$location = $editor.location
$unityPath = @($location, "$location/Contents/MacOS/Unity", "$location/Unity.exe", "$location/Editor/Unity.exe") |
    Where-Object { Test-Path $_ -PathType Leaf } |
    Select-Object -First 1
if (-not $unityPath) { throw "Unity executable not found at $location" }
Write-Host "Unity $($unity.version) installed at $unityPath"

if ($env:UNITY_LICENSE_SERVER_CONFIG) {
    if ($IsWindows) {
        New-Item -ItemType Directory -Force -Path 'C:/ProgramData/Unity/config' | Out-Null
        Set-Content -Path 'C:/ProgramData/Unity/config/services-config.json' -Value $env:UNITY_LICENSE_SERVER_CONFIG
    } else {
        $unityDataDir = $IsMacOS ? '/Library/Application Support/Unity' : '/usr/share/unity3d'
        $configFile = Join-Path $cliDir 'services-config.json'
        Set-Content -Path $configFile -Value $env:UNITY_LICENSE_SERVER_CONFIG
        sudo mkdir -p "$unityDataDir/config"
        sudo cp $configFile "$unityDataDir/config/services-config.json"
        sudo chown -R "$(id -un)" $unityDataDir
        Remove-Item $configFile
    }
}

if ($env:GITHUB_ENV) { "UNITY_PATH=$unityPath" >> $env:GITHUB_ENV }
