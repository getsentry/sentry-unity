Set-StrictMode -Version latest

$meta_path = "$PSScriptRoot/../package-dev/Runtime/Sentry.Unity.dll.meta"
$meta_content = Get-Content -Path $meta_path
$guid = ([Regex]::Match($meta_content, '(?<=guid: )\S+'))

if(!$guid.Success)
{
    Write-Error "Failed to retrieve the guid from '$meta_path'"
    return
}

$AssetContent = @"
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
  m_Script: {fileID: 582302131, guid: $guid, type: 3}
  m_Name: SentryCliOptions
  m_EditorClassIdentifier: 
  <UploadSymbols>k__BackingField: 1
  <UploadDevelopmentSymbols>k__BackingField: 0
  <UploadSources>k__BackingField: 1
  <UrlOverride>k__BackingField: 
  <Auth>k__BackingField: $Env:SENTRY_AUTH_TOKEN
  <Organization>k__BackingField: sentry-sdks
  <Project>k__BackingField: sentry-unity
"@

$AssetPath = "$PSScriptRoot/../samples/unity-of-bugs/Assets/Plugins/Sentry/"
If (-not(Test-Path -Path $AssetPath))
{
  New-Item $AssetPath -Type Directory
}

$AssetPath += "SentryCliOptions.asset"
If (-not(Test-Path -Path $AssetPath))
{
  New-Item $AssetPath
}

Set-Content $AssetPath $AssetContent
