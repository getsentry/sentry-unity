<<<<<<< Updated upstream
# Agent Guidance

## Build

```sh
pwsh bootstrap.ps1
dotnet build
```

- Build outputs and native plugins live in `package-dev/`; do not hand-edit generated assemblies or downloaded native artifacts.
- See `docs/agent-guides/build.md` for target ownership, Unity selection, and local native builds.

## Native SDKs

```sh
dotnet msbuild /t:DownloadNativeSDKs src/Sentry.Unity
```

- Requires `gh`. Use when native artifacts are missing; `dotnet build` does not download them.

## Test

```pwsh
pwsh scripts/run-tests.ps1
pwsh scripts/run-tests.ps1 -Mode PlayMode -Filter "MyTest"
pwsh scripts/run-tests.ps1 -Mode EditMode
```

- The harness targets `samples/unity-of-bugs-local`, using Pipeline for an open Editor or `unity test` headlessly when none is running.
- Make `unity` available on `PATH`. To reuse an Editor, open the sample in Unity 6.6+ with `com.unity.pipeline` and leave play mode stopped.
- Run `dotnet build` before tests after SDK changes. `Filter` narrows selected tests.
- Use batch MSBuild test targets only when Unity CLI is unavailable. See `docs/agent-guides/testing.md` for commands and integration tests.

## Area Guides

Read only guide relevant to task. Do not import all guides at startup.

| Area | Paths or task | Guide |
| --- | --- | --- |
| Build targets and native bootstrap | `Directory.Build.*`, `build/`, build failures | `docs/agent-guides/build.md` |
| GitHub Actions | `.github/workflows/`, CI failures | `docs/agent-guides/ci.md` |
| Tests | `test/`, Unity test harness, integration tests | `docs/agent-guides/testing.md` |
| Packaging and releases | `package*/`, `.craft.yml`, `scripts/pack.ps1`, `scripts/repack.ps1` | `docs/agent-guides/packaging.md` |
| Runtime integrations | `src/Sentry.Unity/`, especially `Integrations/` | `docs/agent-guides/integrations.md` |
| Android | `src/Sentry.Unity.Android/`, Android plugins or export | `docs/agent-guides/platform-android.md` |
| iOS and macOS Cocoa | `src/Sentry.Unity.iOS/`, `src/Sentry.Unity.Editor.iOS/`, Apple plugins | `docs/agent-guides/platform-apple.md` |
| Native desktop and consoles | `src/Sentry.Unity.Native/`, native plugins | `docs/agent-guides/platform-native.md` |
| Unity Editor | `src/Sentry.Unity.Editor/`, Editor configuration or preprocessors | `docs/agent-guides/editor.md` |

## Commits

- Use direct, capitalized commit subjects without conventional-commit prefixes.
- Include the committing agent's own `Co-Authored-By` attribution when a commit is requested.
||||||| Stash base
=======
# Agent Guidance

## Build

```sh
pwsh bootstrap.ps1
dotnet build
```

- Build outputs and native plugins live in `package-dev/`; do not hand-edit generated assemblies or downloaded native artifacts.
- See `docs/agent-guides/build.md` for target ownership, Unity selection, and local native builds.

## Native SDKs

```sh
pwsh scripts/download-native-sdks.ps1
```

- Requires `gh`. Use when native artifacts are missing; `dotnet build` does not download them.

## Test

```pwsh
pwsh scripts/run-tests.ps1
pwsh scripts/run-tests.ps1 -Mode PlayMode -Filter "MyTest"
pwsh scripts/run-tests.ps1 -Mode EditMode
```

- The harness targets `samples/unity-of-bugs-local`, using Pipeline for an open Editor or `unity test` headlessly when none is running.
- Make `unity` available on `PATH`. To reuse an Editor, open the sample in Unity 6.6+ with `com.unity.pipeline` and leave play mode stopped.
- Run `dotnet build` before tests after SDK changes. `Filter` narrows selected tests.
- Use batch MSBuild test targets only when Unity CLI is unavailable. See `docs/agent-guides/testing.md` for commands and integration tests.

## Area Guides

Read only guide relevant to task. Do not import all guides at startup.

| Area | Paths or task | Guide |
| --- | --- | --- |
| Build targets and native bootstrap | `Directory.Build.*`, `build/`, build failures | `docs/agent-guides/build.md` |
| GitHub Actions | `.github/workflows/`, CI failures | `docs/agent-guides/ci.md` |
| Tests | `test/`, Unity test harness, integration tests | `docs/agent-guides/testing.md` |
| Packaging and releases | `package*/`, `.craft.yml`, `scripts/pack.ps1`, `scripts/repack.ps1` | `docs/agent-guides/packaging.md` |
| Runtime integrations | `src/Sentry.Unity/`, especially `Integrations/` | `docs/agent-guides/integrations.md` |
| Android | `src/Sentry.Unity.Android/`, Android plugins or export | `docs/agent-guides/platform-android.md` |
| iOS and macOS Cocoa | `src/Sentry.Unity.iOS/`, `src/Sentry.Unity.Editor.iOS/`, Apple plugins | `docs/agent-guides/platform-apple.md` |
| Native desktop and consoles | `src/Sentry.Unity.Native/`, native plugins | `docs/agent-guides/platform-native.md` |
| Unity Editor | `src/Sentry.Unity.Editor/`, Editor configuration or preprocessors | `docs/agent-guides/editor.md` |

## Commits

- Use direct, capitalized commit subjects without conventional-commit prefixes.
- Include the committing agent's own `Co-Authored-By` attribution when a commit is requested.
>>>>>>> Stashed changes
