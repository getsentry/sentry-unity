# Native Desktop And Console Support

## Architecture

- `src/Sentry.Unity.Native/` supports Windows, Linux, experimental macOS native backend, Xbox, PS5, and Switch variants.
- It initializes sentry-native, synchronizes scope/context/debug images, caches crashed-last-run by database path, and closes at quit.
- `sentry_get_crashed_last_run` clears native state; SDK caches its result for the process lifetime. Do not make it repeatable.
- Native backend reinstalls before first scene after Unity takes crash/signal handlers.
- Native logger forwarding to C# exists only under IL2CPP.
- Desktop library is named `sentry-native`; plain `sentry` resolves to the managed `Sentry.dll` under Mono. Renamed in `build/native-sdks.targets`, so the package already ships it that way.
- Android keeps `sentry` because sentry-java loads its AAR library by name, so it gets its own `Sentry.Unity.Native.Android.dll`.

## Backend Choices

| Platform | Default | Experimental native |
| --- | --- | --- |
| Windows | Crashpad | `sentry-crash.exe` |
| Linux | In-process Breakpad | `sentry-crash` |
| macOS | Cocoa | `sentry-crash` |

Experimental native modes raise minimum shutdown timeout to 10 seconds.

## Post-Build Behavior

`Sentry.Unity.Editor/Native/BuildPostProcess.cs` selects legacy `Sentry~` or experimental `SentryNative~`, clears stale handler artifacts when switching backend, copies runtime libraries to player locations, and leaves symbols in package for upload.

- Windows: runtime files beside player `.exe`; the library lands as `sentry-native.dll`. Both
  architectures ship, under `x86/` and `x64/` inside each backend folder, because Unity still
  builds a 32-bit standalone player. `BuildTarget.StandaloneWindows` picks `x86/`.
- Linux: `libsentry-native.so` under `<Player>_Data/Plugins/x86_64`; native daemon beside executable.
- macOS: `libsentry-native.dylib` in `.app/Contents/PlugIns`; handler in `.app/Contents/MacOS`. Cocoa's `Sentry.dylib` keeps its name, it is dlopened not P/Invoked.
- Post-build copies names through unchanged; stale cleanup wipes pre-rename names.

## Console Plugins

- PS5/Xbox libraries are user-supplied: `Assets/Plugins/Sentry/{PS5,XSX,XB1}/`.
- Switch needs user-supplied static `libsentry.a` and `libzstd.a`; none uses shipped no-op stubs, partial installation is an error.
- Console and Android assemblies compile separately with platform defines. Chained `Csc` targets in `Sentry.Unity.Native.csproj`.

## Tests

- `test/Sentry.Unity.Editor.Tests/Native/BuildPostProcessTests.cs`
- Platform integration coverage: `test/Scripts.Integration.Test/`
