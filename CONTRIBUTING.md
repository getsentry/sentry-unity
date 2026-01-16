# Contributing to Sentry Unity SDK

## Requirements

The following tools are **required** before you can build and develop the SDK:

| Tool | Notes |
|------|-------|
| [Unity Hub](https://unity3d.com/get-unity/download) | Required for managing Unity installations |
| Unity with iOS Build Support | The iOS module is required by `Sentry.Unity.Editor.iOS`. Install via Unity Hub. |
| [.NET SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) | Version pinned in [`global.json`](global.json) |
| PowerShell | Install via `dotnet tool install --global PowerShell` |
| [GitHub CLI](https://github.com/cli/cli/releases) | Required for downloading prebuilt native SDKs. On macOS: `brew install gh` |

After installing the .NET SDK and PowerShell, restore the required workloads:

```sh
dotnet workload restore
```

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

### 2. Download Prebuilt Native SDKs

This step downloads prebuilt native libraries for Android, Linux, and Windows from the latest successful CI build. This is the fastest way to get started.

```sh
dotnet msbuild /t:DownloadNativeSDKs src/Sentry.Unity
```

### 3. Build

```sh
dotnet build
```

That's it! The SDK is now built and ready for development.

> **Note:** Submodules ([sentry-dotnet](https://github.com/getsentry/sentry-dotnet), [Ben.Demystifier](https://github.com/benaadams/Ben.Demystifier)) are restored automatically. If this fails, run `git submodule update --init --recursive`.

> **Note:** The build also downloads and caches Sentry CLI and the Sentry SDK for Cocoa automatically.

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

Run from the command line:

```sh
dotnet msbuild /t:"UnityPlayModeTest;UnityEditModeTest" /p:Configuration=Release test/Sentry.Unity.Tests
```

Or use the TestRunner window inside the Unity Editor.

### Integration and Smoke Tests

Run integration tests locally using the same scripts as CI:

```pwsh
pwsh ./test/Scripts.Integration.Test/integration-test.ps1 -Platform "Android" -UnityVersion "6000"
```

See the script for additional optional parameters. Supported platforms include Android, iOS, macOS, Windows, and Linux.

## Development Workflow

### Project Structure

- `package-dev/` - Development UPM package
- `package/` - Release package template (used for publishing)
- `samples/unity-of-bugs/` - Sample Unity project for local testing
- `src/` - Source code
- `test/` - Tests and integration test scripts

### Making Changes

1. Open `src/Sentry.Unity.sln` in your IDE (e.g., Rider, Visual Studio)
2. Build the solution — artifacts are placed in `package-dev/`
3. Open `samples/unity-of-bugs` via Unity Hub
4. Configure via Tools → Sentry and enter your DSN
5. Click Play and test your changes

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

Releases are published by pushing CI-built artifacts to the [unity package repo](https://github.com/getsentry/unity). The `package` directory contains template files used during this process.

> Do not copy `package-dev` specific files (`package.json`, `*.asmdef`) into `package`.
