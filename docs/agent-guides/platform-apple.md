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

## Session Replay (Experimental, iOS)

- `options.Experimental.SessionReplay` is set from code. Replay is enabled when `SessionSampleRate` or `OnErrorSampleRate` is above 0.
- Replay is runtime-init only: `SentryCocoaBridgeProxy.Init` passes the sample rates and quality to sentry-cocoa's `sessionReplay` options (`SentryNativeBridgeOptionsSetReplay`). An already initialized or external SDK logs that replay is ignored.
- With `Capture = SessionReplayCapture.Native` (the default), sentry-cocoa captures the frames from the key window (Unity's Metal view included), on the main thread.
- With `Capture = SessionReplayCapture.Unity`, `CocoaReplayLifecycle` installs the bridge's `SentryUnityReplayFrameProvider` before the replay starts (`SentryNativeBridgeReplayUseUnityCapture`, through `SentryObjCSDK.internal.replay setScreenshotProvider:`). This hook is sentry-cocoa's `SentryObjCReplayScreenshotProvider` protocol (`imageWithView:onComplete:`), added in [getsentry/sentry-cocoa#9739](https://github.com/getsentry/sentry-cocoa/pull/9739), which the submodule points to; it counts no main-thread time for the provider, so sentry-cocoa doesn't back off its frame rate. sentry-cocoa sets the provider on the main thread and keeps it only until the SDK closes, so the lifecycle installs it again after every start of the SDK. sentry-cocoa asks for each frame on the main thread with the window's size in points and takes no further frame until that request completes. `UnityReplayRecorder` polls the request every frame (`SentryNativeBridgeReplayTakeFrameRequest`), captures the screen at the end of the frame, blits it top-down into an RGBA render texture of that size (`SentryScreenshot.BlitScreen`), reads it back with `AsyncGPUReadback`, and passes the readback's buffer to `SentryNativeBridgeReplayDeliverFrame`, which copies the pixels before it returns and makes the scale-1 `UIImage` and completes the request on a background queue. Every request completes exactly once: a failed capture or readback, a pause, a stop, a disabled or destroyed recorder host, or a request replacing an untaken one completes it with the current replay's last frame (a black one before the first or when the window's size changed), since a request left open stops capture for the rest of the replay. The bridge forgets the last frame whenever a replay starts or stops. After five failures in a row the recorder stops capturing and leaves the next request open, and Unity capture stays off for the rest of the process: the current replay and any later one record no further frames. Keep the provider's entry points in the no-op, external and macOS bridges in sync.
- The game's sentry-cocoa doesn't track sessions, so its replay doesn't start by itself. `CocoaReplayLifecycle` samples it when the SDK starts: below `SessionSampleRate` it starts a session replay, otherwise with `OnErrorSampleRate` above 0 it buffers for errors (sentry-cocoa samples the error rate when an error captures the buffer).
- Unity's pause and resume drive the replay through `SentryObjCSDK.internal.replay`. A pause is the replay's manual pause, so sentry-cocoa's own foreground observer doesn't resume it, and a second pause (sentry-cocoa's background observer) cuts no segment. After a background longer than `AutoSessionTrackingInterval`, the replay is stopped and a new one sampled and started. Closing the SDK stops the replay.
- `CocoaReplaySession` is the .NET SDK's replay session: the .NET client calls `SentryNativeBridgeReplayCapture` for each error event and puts the native scope's `replayId` on that event's dynamic sampling context; other events get the scope's current replay id. The capture runs on the main thread: from another thread, the bridge waits up to 500 ms for it and treats a timeout as no replay.
- The sample reads `-sentry_replay off|session|error` and `-sentry_replay_capture native|unity` from the launch arguments (`xcrun devicectl device process launch … <bundle id> -- -sentry_replay session`) through `NSProcessInfo`, since IL2CPP's `Environment.GetCommandLineArgs()` doesn't return them on iOS.

## iOS Export

- iOS editor code is in `src/Sentry.Unity.Editor.iOS/`.
- It copies `Sentry.xcframework~` and bridge source, mutates the PBX project through reflection, embeds/links the framework, adds `-ObjC`, and can write build-time options into `main.mm`.
- Reflection avoids requiring iOS editor modules on Windows or Linux.
- External exports copy `SentryNativeBridgeExternal.m` as the bridge instead: it looks up the core classes of the app's own SDK (`SentrySDKInternal`, `SentryScope`, `SentryBreadcrumb`) at runtime and checks every selector with `respondsToSelector:`, as Unity builds without Objective-C exceptions. Keep its entry points in sync with the other bridges.
- External exports copy no framework and add the symbol upload build phase to `UnityFramework`, which an embedding app builds.
- Disabled/unconfigured iOS still installs a no-op bridge so P/Invoke symbols resolve.
- Changing an append build from build-time to runtime/disabled requires a Replace export; postprocessing fails rather than preserve stale `main.mm` initialization.

## Tests

- `test/Sentry.Unity.iOS.Tests/SentryNativeIosTests.cs`
- `test/Sentry.Unity.iOS.Tests/CocoaReplayTests.cs`
- `test/Sentry.Unity.Editor.iOS.Tests/BuildPostProcessorTests.cs`
- `test/Sentry.Unity.Editor.iOS.Tests/SentryXcodeProjectTests.cs`
