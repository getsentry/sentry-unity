# Apple Native Support

## Architecture

- `src/Sentry.Unity.iOS/` supplies iOS and Cocoa-backed macOS runtime support.
- C# calls exported Objective-C bridge functions through `__Internal`.
- iOS statically links Sentry Cocoa. macOS Cocoa bridge dynamically loads `Sentry.dylib` from app bundle.

## macOS Backends

- Default: Cocoa, IL2CPP-only.
- Experimental: sentry-native out-of-process backend, supports Mono and IL2CPP.
- On macOS IL2CPP, both code paths compile; `Experimental.MacosBackend` chooses runtime configuration.

## Cocoa Behavior

- Cocoa configuration initializes bridge, scope/context sync, SDK name, crashed-last-run, and IL2CPP installation ID.
- iOS native app-hang tracking can disable C# ANR watchdog.
- macOS Cocoa screenshots are disabled because UIKit is unavailable.
- Cocoa attachment synchronization is not implemented.

## iOS Export

- iOS editor code is in `src/Sentry.Unity.Editor.iOS/`.
- It copies `Sentry.xcframework~` and bridge source, mutates the PBX project through reflection, embeds/links the framework, adds `-ObjC`, and can write build-time options into `main.mm`.
- Reflection avoids requiring iOS editor modules on Windows or Linux.
- Disabled/unconfigured iOS still installs a no-op bridge so P/Invoke symbols resolve.
- Changing an append build from build-time to runtime/disabled requires a Replace export; postprocessing fails rather than preserve stale `main.mm` initialization.

## Tests

- `test/Sentry.Unity.iOS.Tests/SentryNativeIosTests.cs`
- `test/Sentry.Unity.Editor.iOS.Tests/BuildPostProcessorTests.cs`
- `test/Sentry.Unity.Editor.iOS.Tests/SentryXcodeProjectTests.cs`
