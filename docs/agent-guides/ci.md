# CI

## Entry Points

- `ci.yml` is the primary workflow for pushes to `main`/`release/*`, pull requests, and manual dispatch.
- `build.yml` builds the package, aliases assemblies, packages it, builds the dependency-conflict fixture, and runs Unity tests.
- `sdk.yml` builds native SDK artifacts.
- `create-unity-matrix.yml` selects integration-test Unity versions.

## Current Matrix

| Trigger | Integration Unity versions |
| --- | --- |
| Pull request | `2021.3`, `6000.5` |
| Main or non-PR | `2021.3`, `2022.3`, `6000.0`, `6000.3`, `6000.5` |
| `chore/unity-<major.minor>.*` PR | Matching version only |

Exact Unity editor versions and changesets live in `scripts/unity-versions.json`. The package build itself currently uses Unity `2021.3`.

## Build Workflow

`build.yml`:

1. Checks out `src/sentry-dotnet`.
2. Starts a `unityci/editor` Docker image.
3. Restores .NET workloads and downloads native CI artifacts.
4. Runs `dotnet build -c Release -v:d` in the container.
5. Installs `Sentry.AssemblyAlias` and runs `scripts/alias-assemblies.ps1`.
6. Runs `scripts/pack.ps1` and uploads `package-release`.
7. Builds/uploads the DependencyConflict fixture.
8. Runs runtime PlayMode and editor EditMode tests.

## Failure Triage

- Start at the failed reusable workflow, not only the caller in `ci.yml`.
- Match local verification to the failing job; see `testing.md`, `build.md`, or `packaging.md`.
- Native, Unity Library, and many test-app artifacts retain for 14 days. Build-size artifacts retain for one day; `package-release` has no workflow retention override.
- The `dependency-conflict-package` artifact tests that aliased SDK assemblies coexist with unaliased dependency assemblies. A failure can indicate an aliasing regression, not a fixture problem.

## Workflow Ownership

| Area | Workflows |
| --- | --- |
| Package build and Unity tests | `build.yml` |
| Native SDKs | `sdk.yml` |
| Integration project creation | `test-create.yml` |
| Android build/run | `test-build-android.yml`, `test-run-android.yml` |
| iOS build/compile/run | `test-build-ios.yml`, `test-compile-ios.yml`, `test-run-ios.yml` |
| Release preparation | `release.yml` |
