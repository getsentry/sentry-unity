# DependencyConflict — assembly-aliasing stress-test package

DependencyConflict is **not** a real SDK. It is a deliberately tiny package whose
only job is to recreate, in a single Unity project, the managed-dependency
version collision that the real Sentry SDK's assembly aliasing is meant to
survive — and to do so automatically in CI on every integration build.

## The scenario

`package-dev` (the real Sentry SDK) ships its `System.*` / `Microsoft.*`
dependencies **aliased** (prefixed `Sentry.`, internalized) so they can never
clash with whatever a user project drags in.

DependencyConflict plays the part of "a user project / third-party package" that
drags in **plain, unaliased** copies of those same assemblies, at versions that
**differ** from package-dev's — just enough mismatch, in enough places, to cause
friction:

| Assembly                        | package-dev (aliased) | DependencyConflict (plain) |
| ------------------------------- | --------------------- | -------------------------- |
| System.Text.Json                | 8.0.5                 | 8.0.4 (mild skew)          |
| System.Collections.Immutable    | 5.0.0                 | 7.0.0 (clear skew)         |
| Microsoft.Bcl.AsyncInterfaces   | 8.0 (transitive)      | transitive via STJ         |

[`DependencyConflictPackageClient.cs`](DependencyConflictPackageClient.cs) genuinely calls into
all three so the references are real, not just dropped DLLs. The integration test
app invokes it on startup (see `IntegrationTester.cs`, which logs
`"Dependencies say hi"`) so the unaliased assemblies are actually linked into the
build next to Sentry's aliased copies.

## Layout

- `DependencyConflictPackage.csproj` — this build project. Intentionally isolated from
  the repo-root MSBuild config (see its `Directory.Build.props`).
- `DependencyConflict/` — the resulting UPM package, mirroring `package-dev`'s `Runtime/`
  layout. **Not aliased.** Only the Unity metadata (`.meta`, asmdef,
  `package.json`) is committed; `DependencyConflict/Runtime/*.dll` is gitignored and rebuilt.

## Rebuild

```bash
dotnet build test/Scripts.Integration.Test/DependencyConflictPackage
```

Output lands directly in `DependencyConflict/Runtime/`.

## How it runs in CI

1. `build.yml` builds the package (it has the pinned .NET SDK) and uploads it as
   the `dependency-conflict-package` artifact.
2. Each integration build job (`ci.yml`, `test-build-*.yml`) downloads that
   artifact and runs `add-dependency-conflict.ps1`, which copies `DependencyConflict/` into
   the test project's `Packages/` folder as an embedded package — right after
   Sentry is added.
3. The existing **Build Project** step is the assertion: if assembly aliasing
   ever regresses, the duplicate unaliased assemblies collide and the build
   fails. **A red integration build signals an aliasing regression.**

To run the whole thing locally, the package is installed by
[`add-dependency-conflict.ps1`](../add-dependency-conflict.ps1), which defaults to
the in-repo `DependencyConflict/` folder when no `-PackagePath` is given.
