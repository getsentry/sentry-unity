* [Setup](#setup)
* [Development](#development)
	* [Package](#package)
	* [Tests](#tests)
* [Release](#release)

# Basics

## Machine Setup

### Install Unity

We recommend using [Unity Hub](https://unity3d.com/get-unity/download). The specific version to download can be found [here](https://github.com/getsentry/sentry-unity/blob/main/samples/unity-of-bugs/ProjectSettings/ProjectVersion.txt#L1).

You'll need the following modules to be added in order to use Sentry Unity:
 * Android Build Support.
 * iOS Build Support.
 * IL2CPP Build Support for your platform.
 * (optional) WebGL Build Support

### Install PowerShell Global tool

```
dotnet tool install --global PowerShell
```

### Setup for building the Java SDK

* Install Git and ensure is accessible from the PATH
* Install Java 17
  * [Using sdkman](https://sdkman.io/) which manage versions for you. (i.e. `sdk install java 17.0.5-ms`)
  * Or [download the OpenJDK](https://openjdk.java.net/install/) directly.
* Add JAVA_HOME to your environment variables (if not using sdkman):
  * Windows: `setx JAVA_HOME "C:\Program Files\Java\jdk-17.0.5"`
* Install [Android Studio](https://developer.android.com/studio)
  * Open Android Studio and go to Customize -> All settings...
  * Search for "SDK" in the Seachbar
  * Select System Settings -> Android SDK
  * Install the Android SDK
  * Swap tab to SDK Tools
  * Check "Show Package Details"
  * Under Android SDK Build-Tools check "30.0.3"
  * Apply
* Add ANDROID_HOME to your environment variables
  * macOS zsh: `export ANDROID_HOME="$HOME/Library/Android/sdk"`
  * Windows: `setx ANDROID_HOME "C:\Program Files (x86)\Android\android-sdk"` for a machine wide install, `setx ANDROID_HOME "%localappdata%\Android\Sdk"` for a user level install.

### Setup for building the Cocoa SDK

* Install Xcode
* Install Carthage (i.e. `brew install carthage`)

### Setup for building Sentry Native

Sentry Native is a sub module from Sentry Unity and for building it, currently requires the following tools:

* Install [CMake](https://cmake.org/download/).
* A supported C/C++ compiler.

## Get the code

Clone the repo `git clone https://github.com/getsentry/sentry-unity.git` and `cd` into it

## Build the project

[Optional] The build process tries to infer the Unity version by looking up the unity-of-bugs `ProjectVersion.txt`. You can overwrite this behaviour by setting the `UNITY_VERSION` on the path, i.e. adding `export UNITY_VERSION=2022.3.44f1` to your .zshenv 

[Optional] You can save some time on the initial build by downloading the prebuild Native SDK artifacts from the last successful build of the `main` branch (requires [GH CLI](https://cli.github.com/) to be installed locally).

`dotnet msbuild /t:DownloadNativeSDKs src/Sentry.Unity`

To build the whole project (including native SDKs if you've skipped the previous step), run:

`dotnet build`

> Several projects are used as submodules - [sentry-dotnet](https://github.com/getsentry/sentry-dotnet), [Ben.Demystifier](https://github.com/benaadams/Ben.Demystifier)
> The submodule will be restored as a result of `dotnet build`.

### Run tests

```sh
dotnet msbuild /t:"UnityPlayModeTest;UnityEditModeTest" /p:Configuration=Release test/Sentry.Unity.Tests
```

### Smoke test by building and running a player with IL2CPP:

```sh
 dotnet msbuild /t:"Build;UnityBuildStandalonePlayerIL2CPP;UnitySmokeTestStandalonePlayerIL2CPP"  test/Sentry.Unity.Tests
```

After this you can open your IDE (i.e: Visual Studio or Rider) and Unity Editor for development.

# Advanced and Troubleshooting

## Finding the Unity installation

The `UnityPath` in `src/Directory.Build.targets` does a lookup at different locations to find Unity.
This is different per operating system. The repository is configured for Windows and macOS. You can adjust it as needed:

```xml
<Project>
  <!-- Other properties & groups -->
  <PropertyGroup>
    <UnityPath Condition="<YOUR_PATH_CONDITION>">YOUR_PATH</UnityPath>
  </PropertyGroup>
</Project>
```
> There is a configuration in place already. Just make sure it works for you or reconfigure for your needs.

## Project Structure

There are two projects involved in `sentry-unity` development. `UPM` package (`src` and `package-dev` folders) and `Unity` project (`samples/unity-of-bugs` folder, `BugFarmScene.unity`) to test the package in.

## Package

Folders involved in this stage `src`, `package-dev`, `samples` where

* `src` - package source code
* `package-dev` - dev `UPM` package

> The package details/info is in `package.json` [manifest file](https://docs.unity3d.com/Manual/upm-manifestPkg.html). Please, check [Unity package layout](https://docs.unity3d.com/Manual/cus-layout.html) docs for deeper understanding of the package structure.

* `samples` - `Unity` sample projects, for dev flow we use `unity-of-bugs`

Let's outline the needed steps for `UPM` package development flow

* open `samples/unity-of-bugs` in `Unity` or run in silent mode via CLI - `Unity -batchmode -projectPath <YOUR_PATH>/samples/unity-of-bugs -exit`

> The first run will take some time as `Unity` downloads and caches a bunch of pre-included packages and resources.

> We need to run the project first, so it downloads needed dependencies (namely `UnityEngine.TestRunner.dll` and `UnityEditor.TestRunner.dll`) for `src/tests` projects from `com.unity.test-framework@1.1.20` library. After this package is restored, the actual dlls are placed inside `samples/unity-of-bugs/Library/ScriptAssemblies` folder.

* open `src/Sentry.Unity.sln` in your editor of choice
> Make sure the projects are restored properly and you have zero errors, otherwise you probably misconfigured `src/Directory.Build.props` or didn't restore submodules properly
* build solution, artifacts (`.dll`s) will be placed inside `src/package-dev` folder
* check `src/package-dev` folder, it should be populated with the outlined content
  * `Editor` - `Sentry.Unity.Editor.dll`
  * `Runtime` - `Sentry.Unity.dll` and all its dependencies like `Sentry.dll`, `System.Text.Json` and so on
  * `Tests`
    * `Editor` - `Sentry.Unity.Editor.Tests.dll`
    * `Runtime` - `Sentry.Unity.Tests.dll`
* open `samples/unity-of-bugs` project in `Unity`, then `Scenes/BugFarmScene` scene
* configure `Sentry Unity (dev)` package
  * on the tab `Tools`, select `Sentry` and insert your `DSN` or [Sentry SDK](https://sentry.io/settings/sentry-sdks/projects/sentry-unity/) one `https://e9ee299dbf554dfd930bc5f3c90d5d4b@o447951.ingest.sentry.io/4504604988538880`
  * configure other settings for your needs
* run the project in `Unity` via clicking `Play`
* click `ThrowNull` or any other button and check errors in `Sentry` web UI

## Tests

The project has `PlayMode` and `EditMode` tests. They should be available (after you finished `Package` stage outlined above) when your open `samples/unity-of-bugs` project.

The tests project are inside `src/test` folder where

* `Directory.Build.props` specific variables and settings (inherits `src/Directory.Build.props`) for test projects
* `Sentry.Unity.Tests` project for `Runtime` Unity tests
* `Sentry.Unity.Editor.Tests` project for `Editor` Unity tests

Build artifacts from the test projects will be placed inside `package-dev/Tests/Editor` and `package-dev/Tests/Runtime` folders.

In order to run the tests

* open `samples/unity-of-bugs` Unity project
* open `TestRunner` via `Windows -> General -> Test Runner`
* run `PlayMode` or `EditMode` tests

### Package validation

In CI, a workflow validates that the content of the package doesn't change accidentally.
If you intentially want to add or remove files in the final UPM package. You need to accept the diff:

```
pwsh ./test/Scripts.Tests/test-pack-contents.ps1 accept
```

## Release

The release is done by pushing the artifact built in CI [to a new repo](https://github.com/getsentry/unity). The artifact is built by using the template files in the `package` directory. In order to make a release, the contents of `package-dev/Editor` and `package-dev/Runtime` folders should be copied into `package`.

> **Don't** copy `package-dev` specific files like `package.json`, `Runtime/*.asmdef`, `Editor/*.asmdef` into `package`. Those files contain package specific information.
