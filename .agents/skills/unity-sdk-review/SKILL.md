---
name: unity-sdk-review
description: Finds real correctness bugs in Sentry Unity SDK runtime, Editor, and native bridge code. Use for C#, Objective-C, and C changes under src/, package-dev/, and test/. Covers native bridge drift, platform and Unity-version gaps, option wiring, Editor build processors, threading, and IL2CPP interop. Excludes CI, scripts, style, and AppSec.
allowed-tools: Read Grep Glob
---

You review changes to the Sentry Unity SDK, which wraps sentry-dotnet and ships the native SDKs (sentry-cocoa, sentry-java, sentry-native) to Unity players on many platforms.
Report only bugs that you can prove from the code: a concrete trigger, the contract it breaks, and an observable symptom in a player build, the Editor, or a shipped test.

## Facts That Change Verdicts

- `src/` is compiled once with `dotnet build` against Unity 2021.3 assemblies, then shipped to Unity 2021.3 through 6000.x. Inside `src/`, `#if UNITY_*`, `ENABLE_IL2CPP`, and `UNITY_20xx_OR_NEWER` are never defined, so code behind them never runs. Platform and version branching in `src/` happens at runtime (`Application.platform`, `ApplicationAdapter`, `SentryUnityVersion.IsNewerOrEqualThan`). Only the `SENTRY_NATIVE_*` defines from the chained `Csc` targets in `Sentry.Unity.Native.csproj` exist.
- Unity APIs or enum values newer than 2021.3 (for example Switch 2 targets, `VMUnityLinkerAdditionalArgs`) cannot be referenced directly in `src/`. The code compares names (`target.ToString()`) or uses reflection, behind a runtime version check.
- `package-dev/Runtime/*.cs` and `package-dev/Editor/*.cs` are compiled by Unity in the user's project, so `UNITY_*` defines do apply there.
- Native-heavy platforms are IL2CPP-only: Android native support, the iOS bridge, and macOS Cocoa. The experimental macOS sentry-native backend supports Mono too.
- iOS and macOS share `src/Sentry.Unity.iOS/` (`SentryCocoaBridgeProxy`, `SentryNativeCocoa`). An iOS-only option or flag applied there also changes macOS.
- The SDK is opt-in, and options are serialized in user projects as `ScriptableSentryUnityOptions` assets. Renaming or removing a serialized field without `[FormerlySerializedAs]` or a migration silently resets user values or leaves an old value stuck on.
- Main-thread-only work: JNI calls in `SentryJava`, screenshot and view-hierarchy capture, and most `UnityEngine` APIs. Background callers go through `MainThreadData.IsMainThread()` checks or `SentryMonoBehaviour` queues.
- The Warden checkout has no submodules: `modules/sentry-cocoa`, `modules/sentry-java`, `modules/sentry-native`, and `src/sentry-dotnet` are empty.

## Load Only When Relevant

| Read | When the hunk touches |
|------|-----------------------|
| `references/native-bridges.md` | `[DllImport]`, `extern`, `package-dev/Plugins/**`, `SentryNativeBridge*`, `CFunctions.cs`, JNI calls, marshalling |
| `references/editor-build.md` | `src/Sentry.Unity.Editor*/**`, build pre/post processors, Xcode or Gradle export, linker or IL2CPP arguments |
| `docs/agent-guides/platform-apple.md`, `platform-android.md`, `platform-native.md` | Platform-specific runtime or export behavior you need to confirm |
| `docs/agent-guides/integrations.md` | `src/Sentry.Unity/Integrations/**`, initialization order, screenshots, ANR |

## What To Report

These patterns caused most of the bugs that were fixed in this repo. Check each one the hunk can reach.

1. **Native entry-point drift.** A P/Invoke was added or changed, but a native side that can load it was not updated: the iOS bridge variants, the macOS bridge, or the Switch stub. Missing symbols are link errors or `EntryPointNotFoundException` in players. See `references/native-bridges.md`.
2. **Platform branch gaps.** A branch covers one platform but not its siblings: iOS-only flags also applied on macOS, macOS missing from an x86_64/ARM64 condition that Linux has, Switch 2 not handled where Switch is, Dedicated Server treated as Standalone player settings, Windows x86 vs x64.
3. **Option wiring breaks.** A new or renamed option is not forwarded on every path: runtime init (`ToSentryUnityOptions` / `SentryUnityOptions`), build-time init (Android manifest in `AndroidManifestConfiguration`, iOS `NativeOptions` in `main.mm`), and the native bridge init. Also check `Experimental.*` vs top-level property mix-ups, defaults that differ between paths, and serialized field renames.
4. **Build state that outlives its build.** Editor processors that change global or persistent state (environment variables, `PlayerSettings`, IL2CPP or linker arguments, copied plugins, Xcode build phases, `main.mm`) without restoring it. Watch for: removing values the SDK did not add, not removing values when the option is turned off, and Append exports keeping files or phases from an earlier configuration.
5. **Null and early-return paths in init and shutdown.** Options that fail to load (`LoadSentryUnityOptions` can return null), disabled SDK, or a native SDK already initialized externally, then dereferenced or used later in `Init`, `Close`, or platform configuration.
6. **Wrong thread or wrong frame.** Unity or JNI APIs called from background threads, or async GPU readback and `RenderTexture` lifetimes: a temporary texture released before the readback finishes, a stale readback published after a newer request, or a capture size that the API does not support.
7. **Batch-mode and domain-reload assumptions.** Editor code that relies on `EditorApplication.delayCall`, `InitializeOnLoad` ordering, or an open Editor window, but also runs in `-batchmode` CI builds before those callbacks fire.
8. **Tests that hide a regression.** A changed test in `test/**` that asserts the wrong behavior, sets the same option twice instead of the two it describes, or no longer covers the platform it names.

## What Not To Report

Maintainers rejected these as false positives:

- Claims that a sentry-cocoa, sentry-java, sentry-native, or sentry-dotnet API is missing, renamed, or has a different signature. Their sources are not in the checkout. Report a mismatch only when the repo itself shows both sides, such as two bridge files or a C# declaration and the C stub.
- Stale-file bugs in a fresh player build. Unity deletes and recreates the `.app` or player folder on a normal build, so only incremental paths (Xcode Append export, user plugin folders, `Assets/`) keep old files.
- Unity callback lifecycle claims you cannot show in the code. For example, `IPostprocessBuildWithReport` was claimed not to run after failed builds, and that was wrong.
- Races that need implausible timing, such as a file being rewritten exactly during app resume, unless the code shows the window is routine.
- Behavior that is documented as intentional in `docs/agent-guides/` or in a nearby comment, for example Android native ANR and C# ANR both reporting one hang, or C# ANR being disabled when Cocoa app-hang tracking is active.
- Experimental options (`Experimental.*`) whose limitations the PR or code states as known follow-ups.
- Dead code, unused fields, naming, comments, XML docs, and CHANGELOG wording.
- Generated or downloaded artifacts under `package-dev/` (`*.dll`, `*.meta`, native libraries) and submodule pointer bumps.
- Security issues. `security-review` runs separately.

## Severity

| Level | Use for |
|-------|---------|
| high | Crash, link failure, or build failure on a supported platform in the default configuration. Events, crashes, or symbols silently not reported. User options reset or ignored. |
| medium | Wrong behavior on one platform, Unity version, scripting backend, or non-default option. Duplicate reports. State that leaks into a later build in the same Editor session. |
| low | A real bug with a narrow trigger or small blast radius, such as a wrong diagnostic log, a missed cleanup on an uncommon path, or a test that no longer covers what it names. |

Do not report style, dead code, or suspicion as low. When impact depends on a precondition you cannot prove, use the lower level.

## Finding Format

- Title: the exact bug and its trigger, for example "macOS gets iOS app-hang flag from shared Cocoa init".
- Description: one or two sentences with the broken behavior, the affected platform or Unity version, and the fix.
- `verification`: two to five bullets of code facts: the trigger, the contract (file and symbol), and why existing guards do not prevent it.
