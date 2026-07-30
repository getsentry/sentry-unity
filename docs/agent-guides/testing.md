# Testing

## Unity CLI Harness

```pwsh
pwsh scripts/run-tests.ps1
pwsh scripts/run-tests.ps1 -Mode EditMode
pwsh scripts/run-tests.ps1 -Mode PlayMode -Filter "Throttler"
```

- Targets `samples/unity-of-bugs-local`. It uses Pipeline for an open Editor or `unity test` headlessly when no Editor is running.
- Make `unity` available on `PATH`. To reuse an Editor, open the sample in Unity 6.6+ with `com.unity.pipeline` and leave play mode stopped.
- Run `dotnet build` before tests after SDK changes. `Filter` narrows selected tests.

## Local Sample Build

```pwsh
pwsh scripts/build-sample.ps1 -Target Android
```

- Requires a target: `StandaloneWindows64`, `StandaloneOSX`, `StandaloneLinux64`,
  `Android`, `iOS`, or `WebGL`.
- Outputs under `samples/unity-of-bugs-local/Builds/<Target>/`.

## Batch Unity Tests

Use only when Unity CLI harness is unavailable:

```sh
dotnet msbuild /t:UnityPlayModeTest test/Sentry.Unity.Tests
dotnet msbuild /t:UnityEditModeTest test/Sentry.Unity.Editor.Tests
```

## Integration Tests

```pwsh
pwsh ./test/Scripts.Integration.Test/dev-integration-test.ps1 `
  -UnityVersion "6000.5.0f1" `
  -Platform "MacOS" `
  -Repack
```

## Relevant Tests

- Runtime: `test/Sentry.Unity.Tests/`
- Editor: `test/Sentry.Unity.Editor.Tests/`
- Android runtime: `test/Sentry.Unity.Android.Tests/`
- Cocoa runtime: `test/Sentry.Unity.iOS.Tests/`
- iOS editor export: `test/Sentry.Unity.Editor.iOS.Tests/`
- Cross-platform projects: `test/Scripts.Integration.Test/`
