* [Setup](#setup)
* [Development](#development)
	* [Package](#package)
	* [Tests](#tests)
* [Release](#release)

# Setup

* clone the repo `git clone https://github.com/getsentry/sentry-unity.git` and `cd` into it
* restore submodules `git submodule update --init --recursive`
> Several projects are used as submodules - [sentry-dotnet](https://github.com/getsentry/sentry-dotnet), [Ben.Demystifier](https://github.com/benaadams/Ben.Demystifier)
* make sure you have `UnityPath` configured in `src/Directory.Build.props` for your operating system
```xml
<Project>
  <!-- Other propertes & groups -->
  <PropertyGroup>
    <UnityPath Condition="<YOUR_PATH_CONDITION>">YOUR_PATH</UnityPath>
  </PropertyGroup>
</Project>
```
> There is a configuration in place already. Just make sure it works for you or reconfigure for your needs.

Overall, there are two "flows" to take into account - `Development` and `Release`. Each has its corresponding structures and things to consider.

# Development

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
  * open `Component -> Sentry` and insert your `DSN` or [Sentry SDK](https://sentry.io/settings/sentry-sdks/projects/sentry-unity/) one `https://94677106febe46b88b9b9ae5efd18a00@o447951.ingest.sentry.io/5439417`
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

# Release

The release folder is `src/package`. In order to make a release, the contents of `src/package-dev/Editor` and `src/package-dev/Runtime` folders should be copied into `src/package`.

> **Don't** copy `package-dev` specific files like `package.json`, `Runtime/*.asmdef`, `Editor/*.asmdef` into `package`. Those files contain package specific information.

The release can be tested via `github`. `UPM` supports various installation options.

* install `release` package - `https://github.com/getsentry/sentry-unity.git?path=/package` from `package` folder, `main` branch 
* install `release` package - `https://github.com/getsentry/sentry-unity.git?path=/package#feature/package-updated` from `pacakge` folder, `feature/package-updated` branch
* install `dev` package - `https://github.com/getsentry/sentry-unity.git?path=/package-dev` from `package-dev` folder, `main` branch
