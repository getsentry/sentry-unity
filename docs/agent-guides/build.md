# Build

## Toolchain

- Use .NET SDK `10.0.302`; `global.json` disables roll-forward.
- Run `pwsh bootstrap.ps1` after cloning. It restores workloads and attempts optional local setup.
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
- Does not download Sentry CLI or build native SDKs.

## Bootstrap

Run from repository root:

```pwsh
pwsh bootstrap.ps1
```

- Checks for Git, .NET, GitHub CLI, and Unity CLI before initializing submodules, restoring workloads, downloading missing native artifacts and Sentry CLI, building the managed SDK, and configuring samples when matching environment variables are available.
- Continues past recoverable failures and prints remediation for each stage.
- `APPLE_ID` configures both sample projects' Apple Developer Team ID. `SENTRY_AUTH_TOKEN` configures shared Sentry CLI options.

## Prebuilt Native SDKs

```sh
dotnet msbuild /t:DownloadNativeSDKs src/Sentry.Unity
```

- Downloads missing artifacts from successful GitHub Actions CI runs, preferring `main` then current branch.
- Refuses to overwrite tracked changes under `package-dev/Plugins/`.
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
