# Contributing to Sentry Unity SDK

## Requirements

The following tools are **required** before you can build and develop the SDK:

| Tool | Notes |
|------|-------|
| [Unity Hub](https://unity3d.com/get-unity/download) | Required for managing Unity installations |
| Unity with iOS Build Support | The iOS module is required by `Sentry.Unity.Editor.iOS`. Install via Unity Hub. |
| [.NET SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) | Version pinned in [`global.json`](global.json) |
| PowerShell | Install via `dotnet tool install --global PowerShell` |
| [GitHub CLI](https://github.com/cli/cli/releases) | Recommended for downloading prebuilt native SDKs. On macOS: `brew install gh` |
| Unity CLI | Required for the Unity test harness; add `unity` to `PATH` |

### Optional Unity Modules

Depending on which platforms you're targeting, you may also need:
- Android Build Support
- Desktop Platforms (Windows, macOS, Linux)
- WebGL

## Getting Started

### 1. Clone the Repository

```sh
git clone https://github.com/getsentry/sentry-unity.git
cd sentry-unity
```

### 2. Bootstrap

Run the bootstrap script to initialize submodules, restore workloads, download prebuilt native SDKs and Sentry CLI, build the managed SDK, and configure optional sample settings.

```pwsh
pwsh bootstrap.ps1
```

Bootstrap continues after recoverable failures and prints the command needed to retry each step. Set `APPLE_ID` to configure Apple signing and `SENTRY_AUTH_TOKEN` to configure CLI symbol upload before running it.

### 3. Build Changes

```sh
dotnet build
```

`dotnet build` compiles the managed SDK only. It does not download dependencies or build native SDKs.

> **Note:** Bootstrap initializes submodules. To recover from a failed submodule update, run `git submodule update --init --recursive`.

## Building Native SDKs Locally (Optional)

If you need to build the native SDKs yourself instead of using prebuilt artifacts, follow the setup instructions below.

### Building sentry-native

Required tools:
- [CMake](https://cmake.org/download/)
- A supported C/C++ compiler

### Building the Android SDK (sentry-java)

Required tools:
- Git (accessible from `PATH`)
- [Android Studio](https://developer.android.com/studio)
- JDK 17 (via [sdkman](https://sdkman.io/) or [OpenJDK](https://openjdk.java.net/install/))

**Android Studio Setup:**
1. Open Android Studio → Customize → All settings...
2. Search for "SDK" → System Settings → Android SDK
3. Install the Android SDK
4. Switch to SDK Tools tab
5. Check "Show Package Details"
6. Under Android SDK Build-Tools, check "34"
7. Apply

**Environment Variables:**
- Set `ANDROID_HOME`:
  - macOS: `export ANDROID_HOME="$HOME/Library/Android/sdk"`
  - Windows: `setx ANDROID_HOME "%localappdata%\Android\Sdk"`
- Ensure `java` is on your PATH (verify with `java --version`)
  - Windows: Add the JDK `bin` folder to PATH

## Testing

### Unit Tests (PlayMode and EditMode)

The harness uses Pipeline when `samples/unity-of-bugs-local` is open, otherwise it runs
tests headlessly through Unity CLI. Add `unity` to `PATH`; to reuse an Editor, open the
sample in Unity 6.6+ with `com.unity.pipeline` and leave play mode stopped.

```pwsh
pwsh scripts/run-tests.ps1
pwsh scripts/run-tests.ps1 -Mode PlayMode -Filter "MyTest"
pwsh scripts/run-tests.ps1 -Mode EditMode
```

Run `dotnet build` before tests after SDK changes. `Filter` narrows selected tests.

### Local Sample Builds

Build the Unity 6 sample for a specific platform through Unity CLI:

```pwsh
pwsh scripts/build-sample.ps1 -Target Android
```

When the local sample is open in a Pipeline-enabled Unity Editor, the CLI uses
that Editor. Otherwise it starts a batch-mode Editor without opening its UI.

Supported targets are `StandaloneWindows64`, `StandaloneOSX`, `StandaloneLinux64`,
`Android`, `iOS`, and `WebGL`. Build output is under
`samples/unity-of-bugs-local/Builds/<Target>/`.

### Integration Tests

Run integration tests locally using the same scripts as CI:

```pwsh
pwsh ./test/Scripts.Integration.Test/dev-integration-test.ps1 `
  -UnityVersion "6000.5.0f1" `
  -Platform "MacOS" `
  -Repack
```

The wrapper locates Unity, builds and packages the SDK, then calls the core integration test script. See the script for additional parameters. Automated tests cover desktop, Android, iOS, WebGL, and Xbox; Switch and PS5 are build-only.

## Development Workflow

### Project Structure

- `package-dev/` - Development UPM package
- `package/` - Release package template (used for publishing)
- `samples/unity-of-bugs/` - Unity 2021 compatibility sample project
- `samples/unity-of-bugs-local/` - Unity 6 development sample project with shared assets
- `src/` - Source code
- `test/` - Tests and integration test scripts

### Making Changes

1. Open `Sentry.Unity.sln` in your IDE (e.g., Rider, Visual Studio)
2. Build the solution — artifacts are placed in `package-dev/`
3. Open `samples/unity-of-bugs-local` via Unity Hub
4. Configure via Tools → Sentry and enter your DSN
5. Click Play and test your changes

Do not edit generated assemblies or downloaded native artifacts in `package-dev/`.

### Unity Version

The build uses the Unity version from `samples/unity-of-bugs/ProjectSettings/ProjectVersion.txt`. To use a different version:

```sh
export UNITY_VERSION=2022.3.44f1
```

## Advanced Topics

### Package Validation

CI validates that package contents don't change accidentally. To accept intentional changes:

```pwsh
pwsh ./test/Scripts.Tests/test-pack-contents.ps1 accept
```

To build, alias, package, and update the snapshot in one step:

```pwsh
pwsh ./scripts/repack.ps1
```

> Ensure the repository is clean before running `repack.ps1`.

### Release

Releases are prepared manually through `release.yml` with Craft. CI builds the `package-release` artifact; Craft publishes it to the [unity package repo](https://github.com/getsentry/unity), GitHub, and the registry. The `package` directory contains template files used during this process.

> Do not copy `package-dev` specific files (`package.json`, `*.asmdef`) into `package`.
