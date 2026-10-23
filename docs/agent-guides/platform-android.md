# Android Native Support

## Architecture

- Android native support is IL2CPP-only.
- `SentryNativeAndroid` configures Java SDK initialization, context/scope synchronization, installation ID, and crashed-last-run state.
- `SentryJava` owns JNI calls. Initialization status, init, installation ID, and crash state must run on Unity main thread.
- After Unity installs signal handlers, the Android NDK backend is reinstalled to retain native scope and tag data.

## Initialization Modes

- Runtime initialization writes `io.sentry.auto-init=false`; C# initializes the Java SDK later.
- Build-time initialization writes options to the Android manifest and lets Java auto-initialize.
- Runtime option changes cannot alter build-time initialization behavior.
- External initialization (Unity as a Library): the export ships no SDK, writes no manifest entries (they would merge into the app's), and keeps the ProGuard consumer rules. At runtime the game attaches if sentry-android is enabled in its process (no user, environment, or trace sync, no close, no NDK app-hang heartbeat). In a secondary process (e.g. `:Unity`), where the app didn't initialize it, the game initializes it from the Unity options with its own cache directory (`cache/sentry-<process>`), so it doesn't take over the main process's session and persisted scope, and with ANR and tombstone reporting off, since the main process's SDK reports the exits of all the app's processes. There it continues the main process's trace, read from that SDK's persisted scope (`cache/sentry/<dsn hash>/.scope-cache/trace.json`) at start and on resume, and turns off its own trace generation. The symbol upload task goes into `unityLibrary/build.gradle` and runs after `merge<Variant>JniLibFolders`, since the app never builds `launcher`. In the main process without an SDK, native support is skipped. The process name comes from `Application.getProcessName` (API 28+) or `/proc/self/cmdline` below. C# sessions are off and the app's release and environment are used unless set.

## Export Processing

`AndroidManifestConfiguration` copies four Android artifacts into the exported Gradle project, configures `unityLibrary/build.gradle`, ProGuard rules, manifest values, and optional symbol upload.

The Java layer disables duplicate screenshots, auto-session tracking, activity lifecycle breadcrumbs, and user-interaction breadcrumbs/tracing because Unity/.NET owns them.

## Constraints

- Android native ANR and C# ANR monitor different threads; both can report one full hang.
- Historical Android ANRs are runtime-only; no manifest metadata exists for this setting.
- Local Android build steps and Maven-local version constraint: see `build.md`.

## Tests

- `test/Sentry.Unity.Android.Tests/SentryNativeAndroidTests.cs`
- `test/Sentry.Unity.Android.Tests/SentryJavaTests.cs`
- `test/Sentry.Unity.Editor.Tests/Android/`
