# Native Desktop And Console Support

## Architecture

- `src/Sentry.Unity.Native/` supports Windows, Linux, experimental macOS native backend, Xbox, PS5, and Switch variants.
- It initializes sentry-native, synchronizes scope/context/debug images, caches crashed-last-run by database path, and closes at quit.
- `sentry_get_crashed_last_run` clears native state; SDK caches its result for the process lifetime. Do not make it repeatable.
- Native backend reinstalls before first scene after Unity takes crash/signal handlers.
- Native logger forwarding to C# exists only under IL2CPP.
- The native library is copied into the player as `sentry-native`, not `sentry`, because `sentry` resolves to the managed `Sentry.dll` on Windows.
- Android is the exception: its `libsentry.so` comes from the sentry-android-ndk AAR, so it keeps `sentry` and gets its own `Sentry.Unity.Native.Android.dll` built from the same sources.

## Backend Choices

| Platform | Default | Experimental native |
| --- | --- | --- |
| Windows | Crashpad | `sentry-crash.exe` |
| Linux | In-process Breakpad | `sentry-crash` |
| macOS | Cocoa | `sentry-crash` |

Experimental native modes raise minimum shutdown timeout to 10 seconds.

## Post-Build Behavior

`Sentry.Unity.Editor/Native/BuildPostProcess.cs` selects legacy `Sentry~` or experimental `SentryNative~`, clears stale handler artifacts when switching backend, copies runtime libraries to player locations, and leaves symbols in package for upload.

- Windows: runtime files beside player `.exe`; the native library lands as `sentry-native.dll`.
- Linux: `libsentry-native.so` under `<Player>_Data/Plugins/x86_64`; native daemon beside executable.
- macOS: dylib in `.app/Contents/PlugIns` as `libsentry-native.dylib`; handler in `.app/Contents/MacOS`. The Cocoa backend's `Sentry.dylib` keeps its name, it is dlopened rather than P/Invoked.

## Console Plugins

- PS5/Xbox libraries are user-supplied: `Assets/Plugins/Sentry/{PS5,XSX,XB1}/`.
- Switch needs user-supplied static `libsentry.a` and `libzstd.a`; none uses shipped no-op stubs, partial installation is an error.
- Console assemblies compile separately with platform defines.

## Tests

- `test/Sentry.Unity.Editor.Tests/Native/BuildPostProcessTests.cs`
- Platform integration coverage: `test/Scripts.Integration.Test/`
