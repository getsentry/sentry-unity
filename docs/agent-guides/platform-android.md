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
