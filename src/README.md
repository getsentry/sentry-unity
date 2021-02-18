# Development setup

All build variables and settings are stored in `Directory.Build.props`.

## Sentry.Unity

TBD

## Sentry.Unity.Editor

TBD

## Sentry.Unity.Tests

We need to support `Unity` [coroutines](https://docs.unity3d.com/Manual/Coroutines.html) in `nunit` tests. This is done via `UnityTestAttribute` which is **NOT** included in `nunit`. The needed infrastructure for the attribute (and the attribute itself) is located inside `UnityEngine.TestRunner.dll`.

`UnityEngine.TestRunner.dll` is not located in convenient manner as `UnityEngine.dll`. It comes from `UPM` testing package `com.unity.test-framework`.

Currently, we have `../samples/unity-of-bugs` configured and use the libraries from it. Last thing to be aware of, the libraries are stored inside `../samples/unity-of-bugs/Library/ScriptAssemblies` folder which is **empty** if you pull the project. Therefore, we need to run Unity project first and then the libraries will appear.

Run manually or via `CLI`

* manual run - just open the project in `Unity` (`../samples/unity-of-bugs`)
* `CLI` run - `Unity -batchmode -projectPath <project_path> -exit`
    * Windows example - `Unity.exe -batchmode -projectPath c:\Projects\Unity\sentry-unity\samples\unity-of-bugs\ -exit`

> If you want to use another project (not the one used as an example) just make sure the project you assign dependencies from, has UPM `com.unity.test-framework` in `<project_path>/Packages/manifest.json` (`dependencies` property).

## sentry-dotnet

TBD
