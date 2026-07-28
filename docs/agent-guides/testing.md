# Testing

## Batch Unity Tests

Run from repository root with a discoverable Unity installation:

```sh
dotnet msbuild /t:UnityPlayModeTest test/Sentry.Unity.Tests
dotnet msbuild /t:UnityEditModeTest test/Sentry.Unity.Editor.Tests
```

- PlayMode target applies only to `Sentry.Unity.Tests`.
- EditMode target applies only to `Sentry.Unity.Editor.Tests`.
- Results are written to `artifacts/test/playmode/results.xml` or `artifacts/test/editmode/results.xml`; the target validates the result XML.

## Connected Editor Harness

```pwsh
pwsh scripts/run-tests.ps1
pwsh scripts/run-tests.ps1 -Mode EditMode
pwsh scripts/run-tests.ps1 -Mode PlayMode -Filter "Throttler"
pwsh scripts/run-tests.ps1 -SkipBuild -Filter "MyTest"
```

Before running:

- Create and open `samples/unity-of-bugs-local` in Unity `6000.6` or newer.
- Install `com.unity.pipeline`.
- Add `io.sentry.unity.dev` as a local `file:` dependency pointing to this checkout's `package-dev`.
- Stop the Unity editor, connect Pipeline, and make `unity` available on `PATH`.

The harness builds by default, then recompiles and runs selected Editor/PlayMode tests. It fails for failed, inconclusive, empty, malformed, timed-out, or command-error results.

## Integration Tests

Use the local wrapper:

```pwsh
pwsh ./test/Scripts.Integration.Test/dev-integration-test.ps1 `
  -UnityVersion "6000.5.0f1" `
  -Platform "MacOS" `
  -Repack
```

- The wrapper locates Unity, builds, optionally repacks/extracts the package, then invokes the core script.
- `-Clean`, `-Recreate`, `-Rebuild`, `-SkipTests`, and `-NativeSDKPath` are available.
- `integration-test.ps1` is lower-level: it requires `-UnityPath`, `-UnityVersion`, `-Platform`, and `-PackagePath`.
- Automated Pester execution covers desktop, Android, iOS, WebGL, and Xbox. Switch and PS5 are build-only.

## Relevant Tests

- Runtime: `test/Sentry.Unity.Tests/`
- Editor: `test/Sentry.Unity.Editor.Tests/`
- Android runtime: `test/Sentry.Unity.Android.Tests/`
- Cocoa runtime: `test/Sentry.Unity.iOS.Tests/`
- iOS editor export: `test/Sentry.Unity.Editor.iOS.Tests/`
- Cross-platform projects: `test/Scripts.Integration.Test/`
