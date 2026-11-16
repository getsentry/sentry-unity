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
- External initialization (Unity as a Library, experimental): the export ships no SDK, writes no manifest entries (they would merge into the app's), and keeps the ProGuard consumer rules. At runtime the game attaches if sentry-android is enabled in its process (no user, environment, or trace sync, no close, the NDK app-hang heartbeat only if the app enabled `enableNdkAppHangTracking`). In a secondary process (e.g. `:Unity`), where the app didn't initialize it, the game initializes it from the Unity options with its own cache directory (`cache/sentry-<process>`), so it doesn't take over the main process's session and persisted scope, and with ANR and tombstone reporting off, since the main process's SDK reports the exits of all the app's processes. There it continues the main process's trace, read from that SDK's persisted scope (`cache/sentry/<dsn hash>/.scope-cache/trace.json`) at start and on resume, and turns off its own trace generation. The symbol upload task goes into `unityLibrary/build.gradle` and runs after `merge<Variant>JniLibFolders`, since the app never builds `launcher`. In the main process without an SDK, native support is skipped. The process name comes from `Application.getProcessName` (API 28+) or `/proc/self/cmdline` below. C# sessions are off and the app's release and environment are used unless set.

## Export Processing

`AndroidManifestConfiguration` copies four Android artifacts into the exported Gradle project, configures `unityLibrary/build.gradle`, ProGuard rules, manifest values, and optional symbol upload.

The Java layer disables duplicate screenshots, auto-session tracking, activity lifecycle breadcrumbs, and user-interaction breadcrumbs/tracing because Unity/.NET owns them.

## Session Replay (Experimental)

- `options.Experimental.SessionReplay` is set from code, with no editor UI. Replay is enabled when `SessionSampleRate` or `OnErrorSampleRate` is above 0.
- `sentry-android-replay-release.aar` ships in `Sentry~` next to the other artifacts. `GradleSetup` adds it, and `org.jetbrains.kotlin:kotlin-stdlib` from Maven at the version sentry-android-replay builds with, to `unityLibrary/build.gradle` only when replay is enabled in the build-time options. Without replay, the Sentry dependency block is unchanged. Adding and clearing recognize both variants of the block, so a persisted Gradle project follows replay and native support being toggled.
- Replay is runtime-init only: `SentryJava.Init` configures the replay options (sample rates, quality, and SurfaceView capture when sentry-java captures the frames, since Unity renders into a SurfaceView). Build-time and external initialization log that replay is ignored. A missing replay library logs an error asking to enable replay in the build-time options.
- With `Capture = SessionReplayCapture.Unity`, `UnityReplayRecorder` replaces the `ReplayIntegration` sentry-java added with one built through JNI around a custom `Recorder` (a Unity `AndroidJavaProxy`, returned by a `kotlin.jvm.functions.Function0` proxy). sentry-java calls the recorder on Android's UI thread, which only updates flags and the frame config. sentry-java never sends a custom recorder a frame config: after each start or reset, and when Unity's screen size changes, Unity's main thread asks the UI thread to call `onWindowSizeChanged` with the activity window's size, and sentry-java computes the config from it. It gets the window's size, not `Screen.width/height`, which can be a lower render resolution: sentry-java scales recorded touches, which are in window pixels, by that config. At the config's frame rate, Unity's main thread captures the screen at end of frame, blits it top-down into an RGBA render texture at the recording size (`SentryScreenshot.BlitScreen`) and reads it back with `AsyncGPUReadback`. A worker thread attached to the JVM copies each frame into a new `ARGB_8888` bitmap and calls `onScreenshotRecorded`. A started recorder captures until it's paused: sentry-java only resumes a paused one.
- `proguard-sentry-unity.pro` keeps the replay classes Unity reaches through JNI and proxies, and the classes in the JNI signatures it looks up (`ICurrentDateProvider`, `Integration`, `Function0`, `Function1`): keeping a method doesn't keep the classes in its signature, so minified builds would rename them.
- sentry-java samples and starts the replay itself. `AndroidReplaySession` is the .NET SDK's replay session: the replay id of the dynamic sampling context comes from the Java scope. The .NET client calls `captureReplay` for each error event and puts the returned replay id on that event's dynamic sampling context; other events get the Java scope's current replay id.
- Unity builds don't start androidx's process lifecycle (the `lifecycle-process` startup provider isn't in the manifest), so the Java SDK doesn't see the app go to the background. When it isn't running, Unity's pause and resume pause and resume the replay, starting a new one after a background longer than `AutoSessionTrackingInterval`.

## Constraints

- Android native ANR and C# ANR monitor different threads; both can report one full hang.
- Historical Android ANRs are runtime-only; no manifest metadata exists for this setting.
- Local Android build steps and Maven-local version constraint: see `build.md`.

## Tests

- `test/Sentry.Unity.Android.Tests/SentryNativeAndroidTests.cs`
- `test/Sentry.Unity.Android.Tests/SentryJavaTests.cs`
- `test/Sentry.Unity.Editor.Tests/Android/`
