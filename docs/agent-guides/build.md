# Build

## Toolchain

- Use .NET SDK `10.0.302`; `global.json` disables roll-forward.
- Run `dotnet workload restore` after cloning or changing SDK workloads.
- Unity is resolved from `samples/unity-of-bugs/ProjectSettings/ProjectVersion.txt`; set `UNITY_VERSION` to override it.
- `FindUnity` also honors `HubInstallDir` and `HubDefaultEditor` MSBuild properties.

## Standard SDK Build

Run from repository root:

```sh
dotnet build
```

- Runtime assemblies are written to `package-dev/Runtime`.
- Editor assemblies are written to `package-dev/Editor`.
- `netstandard2.0` is the default runtime target; Unity `2022.*` and `6000.*` use `netstandard2.1`.

## Native SDK Bootstrap

Fastest bootstrap path; requires `gh`:

```sh
dotnet msbuild /t:DownloadNativeSDKs src/Sentry.Unity
```

- Downloads missing artifacts from successful GitHub Actions CI runs, preferring `main` then current branch.
- Missing plugins trigger host-specific local native builds during `dotnet build`; download is recommended, not required.
- Do not edit downloaded plugin artifacts in `package-dev/Plugins/`.

## Native Targets

Run targets from repository root:

```sh
dotnet msbuild /t:<Target> src/Sentry.Unity
```

| Target | Purpose |
| --- | --- |
| `BuildCocoaSDK` | Build iOS/macOS Cocoa artifacts. |
| `BuildAndroidSDK` | Publish local NDK and build Android Java artifacts. |
| `BuildLinuxSDK` / `BuildLinuxNativeSDK` | Build Linux legacy/native backend artifacts. |
| `BuildWindowsSDK` / `BuildWindowsNativeSDK` | Build Windows legacy/native backend artifacts. |
| `BuildMacOSNativeSDK` | Build experimental macOS sentry-native backend. |
| `PublishNativeNdkLocal` | Publish sentry-native NDK to Maven local. |

## Android Local Development

```sh
dotnet msbuild /t:PublishNativeNdkLocal src/Sentry.Unity
dotnet msbuild /t:PublishNativeNdkLocal src/Sentry.Unity -p:PurgeNdkCache=true
dotnet msbuild /t:BuildAndroidSDK src/Sentry.Unity
```

- `BuildAndroidSDK` requires checked-out `modules/sentry-java` and `modules/sentry-native/ndk`.
- Publish an NDK version absent from Maven Central; otherwise Gradle can resolve the released artifact instead of Maven local.

## Unity Build Targets

`Directory.Build.targets` defines `UnityConfigureSentryOptions`, `UnityBuildStandalonePlayerIL2CPP`, `UnityBuildPlayerAndroid`, `UnityBuildPlayerIOS`, `UnityBuildPlayerWebGL`, and `UnitySmokeTestPlayerWebGL`.

Read `build/local-dev.targets` before changing their arguments or behavior.
