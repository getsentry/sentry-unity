# Changelog

### Features

- Main thread data ([#245](https://github.com/getsentry/sentry-unity/pull/245))
- New config window ([#243](https://github.com/getsentry/sentry-unity/pull/243))
- Bump Sentry .NET SDK 3.8.0 ([#255](https://github.com/getsentry/sentry-unity/pull/255))
  - [changelog](https://github.com/getsentry/sentry-dotnet/blob/main/CHANGELOG.md#380)
  - [diff](https://github.com/getsentry/sentry-dotnet/compare/3.5.0...3.8.0)

## 0.4.0

- No documented changes.

## 0.3.3

### Fixes

- Release process cleanup

## 0.3.2

### Fixes

- Link.xml file exist check ([#248](https://github.com/getsentry/sentry-unity/pull/248))

## 0.3.1

### Fixes

- Logger now attaching again ([#244](https://github.com/getsentry/sentry-unity/pull/244))

## 0.3.0

### Features

- Sentry config is now a scriptable object ([#220](https://github.com/getsentry/sentry-unity/pull/220))
- Unity protocol ([#234](https://github.com/getsentry/sentry-unity/pull/234))
- Release health integration & Event-listener ([#225](https://github.com/getsentry/sentry-unity/pull/225))

### Fixes

- Default options values ([#241](https://github.com/getsentry/sentry-unity/pull/241))
- Un-embedding the link.xml to fix code stripping ([#237](https://github.com/getsentry/sentry-unity/pull/237))
- Setting IsEnvironmentUser to false by default ([#230](https://github.com/getsentry/sentry-unity/pull/230))

## 0.2.0

### Features

- Offline caching ([#208](https://github.com/getsentry/sentry-unity/pull/208))
- Breadcrumb categories added ([#206](https://github.com/getsentry/sentry-unity/pull/206))
- Bump Sentry .NET SDK 3.5.0 ([#218](https://github.com/getsentry/sentry-unity/pull/218))
  - [changelog](https://github.com/getsentry/sentry-dotnet/blob/main/CHANGELOG.md#350)
  - [diff](https://github.com/getsentry/sentry-dotnet/compare/3.3.4...3.5.0)

### Fixes

- Force 'Auto' from sentry window ([#219](https://github.com/getsentry/sentry-unity/pull/219))
- Hub.IsEnabled check in logging integration ([#210](https://github.com/getsentry/sentry-unity/pull/210))

## 0.1.1

### Fixes

- Fallback for missing/empty product name ([#205](https://github.com/getsentry/sentry-unity/pull/205))
- Add product name to release as default ([#202](https://github.com/getsentry/sentry-unity/pull/202))
- normalize line endings ([#204](https://github.com/getsentry/sentry-unity/pull/204))

## 0.1.0

### Features

- Simplified scene breadcrumbs messages ([#197](https://github.com/getsentry/sentry-unity/pull/197))
- Embedded link.xml in assembly ([#194](https://github.com/getsentry/sentry-unity/pull/194))
- Transition scene to test scene loading events (breadcrumbs) ([#185](https://github.com/getsentry/sentry-unity/pull/185))

### Fixes

- Check/create directory before saving ([#196](https://github.com/getsentry/sentry-unity/pull/196))
- Exclude SentryOptions.json from release package ([#195](https://github.com/getsentry/sentry-unity/pull/195))
- default env and version ([#199](https://github.com/getsentry/sentry-unity/pull/199))
- SentryEvent.ServerName forced to 'null' ([#201](https://github.com/getsentry/sentry-unity/pull/201))

## 0.0.14

### Features

- Simulator is set only when Application.isEditor is true ([#190](https://github.com/getsentry/sentry-unity/pull/190))
- Sentry UnityLogger aligned to Unity Debug API ([#163](https://github.com/getsentry/sentry-unity/pull/163))
- Scene manager integration for breadcrumbs ([#170](https://github.com/getsentry/sentry-unity/pull/170))

### Fixes

- Flag simulator based on Application.isEditor ([#184](https://github.com/getsentry/sentry-unity/pull/184))
- il2cpp remove zeroes from path ([#179](https://github.com/getsentry/sentry-unity/pull/179))
- SDK version format correction ([#120](https://github.com/getsentry/sentry-unity/pull/120))
- Auto compression option is part of drop down (no extra checkbox) ([#160](https://github.com/getsentry/sentry-unity/pull/160))
- Rename DiagnosticsLogger to DiagnosticLogger ([#168](https://github.com/getsentry/sentry-unity/pull/168))
- SentryOptions config proper check ([#176](https://github.com/getsentry/sentry-unity/pull/176))
- Diagnostic logger writes to console that it was disabled ([#183](https://github.com/getsentry/sentry-unity/pull/183))

## 0.0.13

### Fixes

- Missing meta files warnings ([#146](https://github.com/getsentry/sentry-unity/pull/146))

## 0.0.12

### Fixes

- Release process improvements

## 0.0.11

### Feature

- Craft Release

## 0.0.10

### Fixes

- Add missing meta files (cd9c7fd)

## 0.0.9

### Features

- Unity Sentry SDK programmatic setup ([#130](https://github.com/getsentry/sentry-unity/pull/130))
  - SentryWindow updated

### Fixes

- UPM meta updated ([#124](https://github.com/getsentry/sentry-unity/pull/124))
- Bump dotnet 3.3.4 ([#132](https://github.com/getsentry/sentry-unity/pull/132))
  - https://github.com/getsentry/sentry-dotnet/blob/main/CHANGELOG.md#334
  - Bug fixes for performance monitoring
  - Ability to keep failed envelopes for troubleshooting when they are too large

## 0.0.8

### Fixes

- Bump sentry-dotnet to 8ca0906 - fix IL2CPP crash ([#122](https://github.com/getsentry/sentry-unity/pull/122))
- IL2CPP players crash due to startup time detection ([#123](https://github.com/getsentry/sentry-unity/pull/123))

## 0.0.7

### Features

- Strip zeroes for ill2cpp builds ([#108](https://github.com/getsentry/sentry-unity/pull/108))
- Proper sdk name reporting for sentry event ([#111](https://github.com/getsentry/sentry-unity/pull/111))
- Bump .NET SDK to 3.3.1 ([#115](https://github.com/getsentry/sentry-unity/pull/115))
- Release package samples ([#113](https://github.com/getsentry/sentry-unity/pull/113))

## 0.0.6

### First release through UPM

- .NET SDK setup for Unity with Unity editor configuration.
