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
- `EnableAppHangTracking` configures Cocoa app-hang tracking on iOS and macOS when the Cocoa backend is active.
- App-hang tracking disables the C# ANR watchdog on iOS and macOS when the Cocoa backend is active.
- macOS Cocoa screenshots are disabled because UIKit is unavailable.
- Cocoa attachment synchronization is not implemented.
- iOS external initialization (experimental): C# never initializes or closes Cocoa, attaches only if the host app already initialized it, keeps the host's SDK name, and disables the C# ANR watchdog. It doesn't sync user, environment, or trace to the host's scope, turns off C# session tracking, uses the host's release and environment unless set, puts the host's user on C# events (per event) and logs (at start and on resume) unless the game sets its own (`NativeAppUser`), continues the host's trace (`NativePropagationContext`, read through the bridge on every use) unless a sampled C# span is active. Transactions started through `Sentry.Unity.SentrySdk.StartTransaction(name, operation)` join the host's trace: as a child of the host's active span, inheriting its sampling decision and DSC, otherwise as a root with sampling deferred to the game.

## iOS Export

- iOS editor code is in `src/Sentry.Unity.Editor.iOS/`.
- It copies `Sentry.xcframework~` and bridge source, mutates the PBX project through reflection, embeds/links the framework, adds `-ObjC`, and can write build-time options into `main.mm`.
- Reflection avoids requiring iOS editor modules on Windows or Linux.
- The iOS bridge looks up sentry-cocoa classes at runtime: the `SentryObjC*` classes when the export bundles the framework, otherwise the core classes of the app's own SDK (`SentrySDKInternal`, `SentryScope`, `SentryBreadcrumb`, `SentryUser`). It is compiled with `-fobjc-exceptions`.
- External exports copy no framework and add the symbol upload build phase to `UnityFramework`, which an embedding app builds.
- Disabled/unconfigured iOS still installs a no-op bridge so P/Invoke symbols resolve.
- Changing an append build from build-time to runtime/disabled requires a Replace export; postprocessing fails rather than preserve stale `main.mm` initialization.

## Tests

- `test/Sentry.Unity.iOS.Tests/SentryNativeIosTests.cs`
- `test/Sentry.Unity.Editor.iOS.Tests/BuildPostProcessorTests.cs`
- `test/Sentry.Unity.Editor.iOS.Tests/SentryXcodeProjectTests.cs`
