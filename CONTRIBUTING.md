# Prerequisites

Before you start, you need to install the following tools and dependencies.

## Install Unity

1. Install [Unity Hub](https://unity3d.com/get-unity/download)
2. Install Unity
   * [Optional] Pick the Unity version specified [here](https://github.com/getsentry/sentry-unity/blob/main/samples/unity-of-bugs/ProjectSettings/ProjectVersion.txt#L1) so you don't have to update the sample project
   * If you do install a different version or you want to build against a specific version, add it as `UNITY_VERSION` to the path (i.e. `export UNITY_VERSION=2022.3.44f1`)
3. Install iOS Build Module - Required by `Sentry.Unity.Editor.iOS`
4. Optional modules to install, depending on which platfor you're going to target
   * Android
   * Desktop Platforms
   * WebGL

## Install .NET

You can find the pinned version in the `global.json` and download it from [here](https://dotnet.microsoft.com/en-us/download/dotnet/9.0).

After you've downloaded and installed the correct version of the .NET SDK install the PowerShell tool

```
dotnet tool install --global PowerShell
```

and follow this up by restoring the workload

```
dotnet workload restore
```

## Install GitHub CLI

You can either download the newest release of [here](https://github.com/cli/cli/releases), or if you're on macOS use `brew install gh`. You'll need to log in through the commandline.

## (Optional) Setup for Building Android SDK

Only required if you plan to build the Android SDK yourself (instead of using prebuilt artifacts).

* Install Git and ensure is accessible from the `PATH`
* Install [Android Studio](https://developer.android.com/studio)
  * Open Android Studio and go to Customize -> All settings...
  * Search for "SDK" in the Seachbar
  * Select System Settings -> Android SDK
  * Install the Android SDK
  * Swap tab to SDK Tools
  * Check "Show Package Details"
  * Under Android SDK Build-Tools check "34"
  * Apply
* Install JDK 17
  * [Using sdkman](https://sdkman.io/) which manage versions for you. (i.e. `sdk install java 17.0.5-ms`)
  * Or [download the OpenJDK](https://openjdk.java.net/install/) directly.
* Additional setup:
  * Add `ANDROID_HOME` to your environment variables
    * macOS zsh: `export ANDROID_HOME="$HOME/Library/Android/sdk"`
    * Windows: `setx ANDROID_HOME "%localappdata%\Android\Sdk"` for a user level install.
  * Make sure `java` is on the path. You can check by calling `java --version`
    * Windows: Add the `bin` to the path i.e. `$env:PATH = "$env:PATH;$env:JAVA_HOME\bin`

## (Optional) Setup for Building sentry-native

Only required if you plan to build sentry-native yourself (instead of using prebuilt artifacts).

Sentry Native is a submodule from Sentry SDK for Unity and to building it following tools are required:

* Install [CMake](https://cmake.org/download/).
* A supported C/C++ compiler.

# Getting Started

## Clone the Repository

Clone the repo `git clone https://github.com/getsentry/sentry-unity.git` and `cd` into it.

## (Recommended) Download Prebuilt Native SDKs

Instead of building the native SDKs for Android, Linux, and Windows yourself, you can save time by downloading prebuilt artifacts from the last successful build of the `main` branch. This requires [GH CLI](https://cli.github.com/) to be installed.

Run `dotnet msbuild /t:DownloadNativeSDKs src/Sentry.Unity -v:d` to download the native SDKs.

# Building the SDK

## Build the Sentry SDK for Unity

The build process attempts to look up the Unity version to use at `./samples/unity-of-bugs/ProjectSettings/ProjectVersion.txt`. If you've got a different version installed you can overwrite this behaviour by setting the `UNITY_VERSION` on the path, i.e. adding `export UNITY_VERSION=2022.3.44f1` to your `.zshenv`.

To build the project either run `dotnet build -v:d` from the commandline or open `src/Sentry.Unity.sln` via the IDE of your choice and build the solution from there.

> Several projects are used as submodules - [sentry-dotnet](https://github.com/getsentry/sentry-dotnet), [Ben.Demystifier](https://github.com/benaadams/Ben.Demystifier)
> The submodule will be automatically restored as a result. If this fails, you can manually do so by calling `git submodule update --init --recursive`.

> The Sentry SDK for Unity has a dependency on Unity's `TestRunner.dll`. The target `LocateTestRunner` in `Directory.Build.targets` attempts to locate this library inside one of the templates that come with a default installation of Unity via the Hub. If you do not have the templates installed you can unblock yourself from this dependency by unloading the test projects.

Building the SDK will also download and cache Sentry CLI, and the Sentry SDK for Cocoa.

# Testing

## PlayMode and EditMode Tests

You can run tests either from the TestRunner window inside the Editor or from commandline via

```sh
dotnet msbuild /t:"UnityPlayModeTest;UnityEditModeTest" /p:Configuration=Release test/Sentry.Unity.Tests -v:d
```

## Integration and Smoke Tests

CI makes use of a handful of scripts for creating, exporting, building and smoke-testing builds for desktop and mobile platforms. We've added a script to make use of that functionality to emulate (and debug) our integration tests locally.

```pwsh
 pwsh ./test/Scripts.Integration.Test/integration-test.ps1 -Platform "Android" -UnityVersion "6000"
```

Please refer to the script to make use of any optional parameters.

# Development Workflow

## Project Structure

The relevant structure is as follows:
  * The `UPM` package
    * `package-dev` is the dev `UPM` package
    > The package details/info is in `package.json` [manifest file](https://docs.unity3d.com/Manual/upm-manifestPkg.html). Please, check [Unity package layout](https://docs.unity3d.com/Manual/cus-layout.html) docs for deeper understanding of the package structure.
    * `package` contains some prepared meta files used for packaging
  * The sample - `samples/unity-of-bugs` is a Unity project used to local testing. The SDK is installed as a local package pointing at `package-dev`
  * The source
    * `src` contains the source code
    * `test` contains the tests and integration test relevants scripts

## Making Changes and Testing

Here's the typical workflow for `UPM` package development:

1. Open `src/Sentry.Unity.sln` in your editor of choice, i.e. Rider
   > Make sure the projects are restored properly and you have zero errors, otherwise you probably misconfigured `src/Directory.Build.props` or restoring the submodules failed

2. Build the solution: Artifacts (`.dll`s) will be placed inside `src/package-dev` folder

3. Check `src/package-dev` folder, it should be populated with the outlined content
   * `Editor` - `Sentry.Unity.Editor.dll`
   * `Runtime` - `Sentry.Unity.dll` and all its dependencies like `Sentry.dll`, `System.Text.Json` and so on
   * `Tests`
     * `Editor` - `Sentry.Unity.Editor.Tests.dll`
     * `Runtime` - `Sentry.Unity.Tests.dll`

4. Open `samples/unity-of-bugs` via the Hub

5. Configure `Sentry Unity (dev)` package
   * Open `Tools` -> `Sentry` and insert your `DSN`

6. Run the project in the `Unity` Editor by clicking `Play`

7. Click `ThrowNull` or any other button and check errors in `Sentry` web UI

# Advanced Topics

## Package Validation

In CI, a workflow validates that the content of the package doesn't change accidentally.
If you intentially want to add or remove files in the final UPM package. You need to accept the diff:

```pwsh
 pwsh ./test/Scripts.Tests/test-pack-contents.ps1 accept
```

> There is some automation in place that allows you to build, alias, package, and update the snapshot by running `pwsh ./scripts/repack.ps1`. Make sure the repository is in a clean state before doing so as to not add pending changes.

## Release

The release is done by pushing the artifact built in CI [to a new repo](https://github.com/getsentry/unity). The artifact is built by using the template files in the `package` directory. The release process automatically moves specific contents of `package-dev` into `package`.
> **Don't** copy `package-dev` specific files like `package.json`, `Runtime/*.asmdef`, `Editor/*.asmdef` into `package`. Those files contain package specific information.
