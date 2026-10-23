# Runtime Integrations

## Initialization

- `package-dev/Runtime/SentryInitialization.cs` is generated package source and starts SDK initialization.
- WebGL initializes before scene load; other targets initialize at subsystem registration.
- `SentryUnitySdk.Init()` configures processors, .NET SDK initialization, startup tracing, native-context writing, and shutdown handling.
- `SentryMonoBehaviour` is hidden, persistent, lazily created, and queues coroutines submitted from background threads.

## Integration Ownership

- `UnityLogHandlerIntegration` intercepts `Debug.LogException`, preserving the exception and setting mechanism `Unity.LogException` with `handled:false`.
- `UnityApplicationLoggingIntegration` handles `Application.LogMessageReceived` for events, breadcrumbs, and structured logs.
- Startup, scene tracing, trace generation, before-scene-load, lifecycle/session, scene breadcrumbs, scope, ANR, low-memory, and exception filters are configured through `SentryUnityOptions`.
- WebGL uses `UnityWebGLExceptionHandler` rather than `UnityLogHandlerIntegration`.
- Screenshot, view-hierarchy, and IL2CPP processors are optional additions after options construction.

## Constraints

- Startup tracing requires non-Editor, non-WebGL execution, `AutoStartupTraces`, and positive `TracesSampleRate`.
- Startup transaction: `app.start`, name `runtime.initialization`; spans cover subsystem registration, assemblies, splash screen, and first scene.
- Scene tracing replaces global `SceneManagerAPI.overrideAPI`; it overwrites application customization and logs a warning.
- C# ANR monitoring pauses while backgrounded. Cocoa/native app-hang tracking can disable it to prevent duplicate reports.
- Screenshot capture and view-hierarchy capture must run on Unity main thread.
- Screenshots get attached to the event's hint as `SentryScreenshotAttachmentContent`, so they ride along the event's own envelope. Events in the same frame share one, taken at end of frame. An async GPU readback gets encoded off the main thread right after it completes; until then, later events share the in-flight screenshot. The content waits for it when the envelope gets serialized on the SDK's worker; it never waits on the main thread and times out if the main thread stops rendering.

## Tests

- `test/Sentry.Unity.Tests/StartupTracingIntegrationTests.cs`
- `test/Sentry.Unity.Tests/IntegrationTests.cs`
- Related runtime coverage under `test/Sentry.Unity.Tests/`
