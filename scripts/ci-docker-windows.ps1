param(
    [Parameter(Mandatory=$true)]
    [string]$UnityPrefix,
    [Parameter(Mandatory=$true)]
    [string]$ImageVariant,
    [Parameter(Mandatory=$true)]
    [string]$LicenseConfig
)

$ErrorActionPreference = "Stop"

$unityVersion = & pwsh ./scripts/ci-env.ps1 "unity$UnityPrefix"
$imageVariant = $ImageVariant.ToLower()

$container = "unity"
$image = "unityci/editor:windows-$unityVersion-$imageVariant-3"
$cwd = if ($env:GITHUB_WORKSPACE) { $env:GITHUB_WORKSPACE } else { (Get-Location).Path }

# Check for existing container
$existing = docker ps -a --filter "name=^/$container$" --format '{{.Names}}'
if ($existing -eq $container) {
    Write-Host "Removing existing container '$container'"
    docker stop $container
    docker rm $container
}

Write-Host "Starting up '$image' as '$container'"

# Format: <job-name>-<image-variant>-<run-id>
$jobName = if ($env:GITHUB_JOB) { $env:GITHUB_JOB } else { "local" }
$runId = if ($env:GITHUB_RUN_ID) { $env:GITHUB_RUN_ID } else { "0" }
$uniqueHostname = "$jobName-$imageVariant-$runId" -replace '[_\s]', '-'

docker run -td --name $container `
    --hostname $uniqueHostname `
    -v "${cwd}:C:\sentry-unity" `
    -e UNITY_VERSION=$unityVersion `
    -e GITHUB_ACTIONS=$env:GITHUB_ACTIONS `
    -e SENTRY_AUTH_TOKEN=$env:SENTRY_AUTH_TOKEN `
    --workdir "C:\sentry-unity" $image

# Set up Unity license configuration
docker exec $container powershell -Command "New-Item -ItemType Directory -Path 'C:\ProgramData\Unity\config' -Force | Out-Null"
docker exec $container powershell -Command "Set-Content -Path 'C:\ProgramData\Unity\config\services-config.json' -Value '$LicenseConfig'"

# Create unity-editor wrapper script to match the Linux Docker image convention.
# In Linux GameCI images, /usr/bin/unity-editor wraps Unity with -batchmode.
# We replicate that here so the same UNITY_PATH env var works for both platforms.
docker exec $container powershell -Command @'
$unityExe = Get-ChildItem 'C:\Program Files\Unity' -Filter Unity.exe -Recurse -ErrorAction SilentlyContinue |
    Where-Object { $_.DirectoryName -match '\\Editor$' } |
    Select-Object -First 1
if (-not $unityExe) { throw 'Unity.exe not found in the container' }
$content = "@echo off`r`n`"$($unityExe.FullName)`" -batchmode %*"
Set-Content -Path 'C:\Windows\unity-editor.cmd' -Value $content
Write-Host "Created unity-editor wrapper pointing to $($unityExe.FullName)"
'@

Write-Host "Container started successfully:"
docker ps --filter "name=^/$container$"
