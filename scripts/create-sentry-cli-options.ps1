Set-StrictMode -Version latest
$ErrorActionPreference = "Stop"

Write-Output "Creating Sentry CLI Options"

$meta_path = "$PSScriptRoot/../package-dev/Runtime/Sentry.Unity.dll.meta"
$meta_content = Get-Content -Path $meta_path
$guid = ([Regex]::Match($meta_content, '(?<=guid: )\S+'))

if($guid.Success)
{
  Write-Output "Successfully read GUID '$guid' from '$meta_path'"    
}
else 
{
  Write-Error "Failed to retrieve the guid from '$meta_path'"
}

$assetContent = @"
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 1079966944, guid: $guid, type: 3}
  m_Name: SentryCliOptions
  m_EditorClassIdentifier: 
  <UploadSymbols>k__BackingField: 1
  <UploadDevelopmentSymbols>k__BackingField: 0
  <UploadSources>k__BackingField: 1
  <UrlOverride>k__BackingField: 
  <Auth>k__BackingField: $Env:SENTRY_AUTH_TOKEN
  <Organization>k__BackingField: sentry-sdks
  <Project>k__BackingField: sentry-unity
  <IgnoreCliErrors>k__BackingField: 0
  <CliOptionsConfiguration>k__BackingField: {fileID: 11400000, guid: d57b7f2fd263a40d6945e08d5708dc2a,
    type: 2}
"@

$assetPath = "$PSScriptRoot/../samples/unity-of-bugs/Assets/Plugins/Sentry/"
If (-not(Test-Path -Path $assetPath))
{
  Write-Output "Creating directory at '$assetPath'"
  New-Item $assetPath -Type Directory
}

$assetPath += "SentryCliOptions.asset"
If (-not(Test-Path -Path $assetPath))
{
  Write-Output "Creating asset file."
  New-Item $assetPath
}

Write-Output "Writing content to file."
Set-Content $assetPath $assetContent
