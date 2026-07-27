# Agent Guidance

## Build

```sh
dotnet workload restore
dotnet build
```

- Run standard SDK builds from repository root.
- Build outputs and native plugins live in `package-dev/`; do not hand-edit generated assemblies or downloaded native artifacts.
- See `docs/agent-guides/build.md` for target ownership, Unity selection, and local native builds.

## Prebuilt Native SDKs

```sh
dotnet msbuild /t:DownloadNativeSDKs src/Sentry.Unity
```

- Downloads prebuilt native SDKs from CI. Fastest bootstrap path; requires `gh`.

## Test

```sh
dotnet msbuild /t:UnityPlayModeTest test/Sentry.Unity.Tests
dotnet msbuild /t:UnityEditModeTest test/Sentry.Unity.Editor.Tests
```

- See `docs/agent-guides/testing.md` before connected-editor or integration tests.

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
