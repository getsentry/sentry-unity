# Editor Build Pipeline

Read this when a hunk touches `src/Sentry.Unity.Editor/**`, `src/Sentry.Unity.Editor.iOS/**`, or `package-dev/Editor/**`.

## Processors And Order

| Processor | Hook | Order |
|-----------|------|-------|
| `Native/{Switch,PS5,Xbox}NativePluginBuildPreProcess` | `IPreprocessBuildWithReport` | -100 |
| `Il2CppBuildPreProcess`, `MonoLinkerProcessor` | `IPreprocessBuildWithReport` | 0 |
| `WebGL/BuildPreProcess` | `IPreprocessBuildWithReport` | 1 |
| `Native/BuildPostProcess` (Windows, Linux, macOS, consoles) | `IPostprocessBuildWithReport` | 1 |
| `Android/AndroidManifestConfiguration` | `IPostGenerateGradleAndroidProject` | `PostGenerateGradleProjectCallbackOrder` |
| `Sentry.Unity.Editor.iOS/BuildPostProcess` | `[PostProcessBuild(1)]` | 1 |

Options are loaded with `SentryScriptableObject.LoadOptions(isBuilding: true)`, which can return null. Every processor must handle null and a disabled SDK.

## Build Targets

- Use `NamedBuildTargets.FromBuildReport(report)` for player settings such as the scripting backend. `BuildTargetGroup.Standalone` covers Dedicated Server, which has its own `NamedBuildTarget.Server` settings. Reading the group alone gets the player's backend.
- `GetSubtarget<StandaloneBuildSubtarget>()` throws on non-Standalone groups. Only call it after checking the group.
- Switch 2 exists only in newer Unity versions. Compare it through `SwitchBuildTargets.IsSwitch2()` and `IsSwitchFamily()`. A `BuildTarget.Switch` or `BuildTargetGroup.Switch` check alone misses Switch 2.
- Unity plugin CPU folder names are case sensitive: `x86_64` and `ARM64`, not `arm64`.
- Windows ships `x86/` and `x64/` folders. `BuildTarget.StandaloneWindows` is x86.
- `nameof(x)` returns the identifier `"x"`, not its value. That bug has shipped here before in plugin path lookups.

## State That Must Not Leak

The Editor process outlives each build, and exported projects can be built again with **Append**. Check:

- **Global arguments**: `PlayerSettings.SetAdditionalIl2CppArgs`, the UnityLinker diagnostic switch (`VMUnityLinkerAdditionalArgs`, Unity 6.5+ only), and environment variables. The SDK may only add or remove its own flags (`--emit-source-mapping`, `--link-symbols`). It must leave user flags alone, and it must remove its flag when the controlling option (`Il2CppLineNumberSupportEnabled`) is off.
- **Xcode Append exports** (`SentryXcodeProject`, `NativeMain`, iOS `BuildPostProcess`): files copied once (`SentryNativeBridge*.m`, frameworks), build phases (symbol upload on `UnityFramework` vs the main target), and `main.mm` initialization stay from the previous export. When an option switch changes what the export needs, the code must either replace the old artifact or fail with a `BuildFailedException` that asks for a Replace export. That is the repo convention.
- **User plugin folders** (`Assets/Plugins/Sentry/...`): Switch stubs are copied there while the libraries are missing, and must be deleted once the libraries exist, so that a stub never links next to the real library.
- **Native backend switch** (`Sentry~` vs `SentryNative~`): stale handler artifacts from the other backend must be cleared.

A fresh, non-Append player build recreates its output folder. Do not report stale files there.

## Option Paths

A user option can reach a player through several paths. A change to one path usually needs the others:

1. Runtime: `ScriptableSentryUnityOptions` → `ToSentryUnityOptions` → `SentryUnityOptions` → platform `Configure*` → native bridge options.
2. Android build-time: `AndroidManifestConfiguration` writes `io.sentry.*` meta-data. Runtime init writes `io.sentry.auto-init=false`.
3. iOS build-time: `NativeOptions` generates the options dictionary in `main.mm`.
4. Editor UI: `ConfigurationWindow/*Tab.cs` edits serialized fields. Removing a toggle while the runtime still reads the field leaves old saved values stuck on.

## Batch Mode

CI and users build with `-batchmode -executeMethod`. `EditorApplication.delayCall`, `InitializeOnLoad` work scheduled for later, and window-driven setup may not run before the build starts. Preprocess steps must not depend on them.
