# Packaging And Releases

## Package Layout

| Path | Purpose |
| --- | --- |
| `package-dev/` | Development UPM package, including build output and plugins. |
| `package/` | Release overlay: package metadata and release-only files. |
| `package-release/` | Generated staging package. |
| `package-release.zip` | Generated distributable package. |

## Local Release Preparation

```pwsh
pwsh scripts/repack.ps1
```

This mutates `package-dev` alias state and Unity metadata, recreates `package-release/` and `package-release.zip`, and rewrites the package snapshot. Do not use it as read-only validation.

## Aliasing

```pwsh
pwsh scripts/alias-assemblies.ps1
```

- Requires `assemblyalias` on `PATH`.
- Runtime aliases `Microsoft*;System*`; editor aliases `Microsoft*;Mono.Cecil*`.
- Both use `Sentry.` prefix and internalization.
- CI installs `Sentry.AssemblyAlias` before invoking this script.

## Packaging And Validation

```pwsh
pwsh scripts/pack.ps1
pwsh ./test/Scripts.Tests/test-pack-contents.ps1
pwsh ./test/Scripts.Tests/test-pack-contents.ps1 accept
```

- `pack.ps1` recreates the staging directory, copies filtered `package-dev`, overlays `package/`, copies root changelog/license and sample assets, then writes the zip.
- It does not build or alias assemblies.
- Package-content validation requires an existing zip and normalizes generated BCSymbolMap GUIDs.

## CI And Release Ownership

- `build.yml` owns CI packaging and publishes the `package-release` artifact.
- `release.yml` is manual Craft release preparation.
- `.craft.yml` publishes to UPM (`getsentry/unity`), GitHub, and registry using the `package-release` artifact.
- `scripts/bump-version.sh` updates `Directory.Build.props`, `package/package.json`, and root/package READMEs.
