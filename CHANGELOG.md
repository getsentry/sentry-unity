# Changelog

## 4.0.0-beta.0

### Breaking Changes

- Removed Unity 2019 support, which reached End of Life in 2022. Minimum supported version now is 2020. ([#2231](https://github.com/getsentry/sentry-unity/pull/2231))

- **Breaking Change**: The Unity SDK's static API has been moved from `Sentry.Unity.SentryUnity` and `Sentry.SentrySdk` to `Sentry.Unity.SentrySdk`. 
  This change enables [manual/programatic SDK initialization](https://docs.sentry.io/platforms/unity/configuration/options/programmatic-configuration/) with full functionality, previously only available through auto-initialization.
  The underlying .NET SDK's `SentrySdk` class is now internal, and several previously public classes like `SentryInitialization` 
  and `SentryIntegrations` are now internal.
  
  **Migration**: Update your `using` directives from `using Sentry;` to `using Sentry.Unity;`. IDEs like Rider can automatically 
  import the missing references. In some cases, you may need both `using Sentry.Unity;` (for the static API) and `using Sentry;` 
  (for types like `SentryId`). No changes are required to your actual SDK method calls (e.g., `SentrySdk.CaptureException()` 
  remains the same). ([#2227](https://github.com/getsentry/sentry-unity/pull/2227), [#2239](https://github.com/getsentry/sentry-unity/pull/2239))

### Features

- The SDK now comes with a `SentryUserFeedback` prefab ready to be used. You can drag and drop it into your scene or 
  customize it by creating your own variant. The user feedback feature allows your users to provide feedback in form
  of a written message that can optionally have a screenshot attached. Read more about it ([here](https://docs.sentry.io/product/user-feedback/)). ([#2220](https://github.com/getsentry/sentry-unity/pull/2220))

### Dependencies

- Bump Java SDK from v8.14.0 to v8.17.0 ([#2218](https://github.com/getsentry/sentry-unity/pull/2218), [#2223](https://github.com/getsentry/sentry-unity/pull/2223), [#2238](https://github.com/getsentry/sentry-unity/pull/2238))
  - [changelog](https://github.com/getsentry/sentry-java/blob/main/CHANGELOG.md#8170)
  - [diff](https://github.com/getsentry/sentry-java/compare/8.14.0...8.17.0)
- Bump Native SDK from v0.9.0 to v0.9.1 ([#2217](https://github.com/getsentry/sentry-unity/pull/2217))
  - [changelog](https://github.com/getsentry/sentry-native/blob/master/CHANGELOG.md#091)
  - [diff](https://github.com/getsentry/sentry-native/compare/0.9.0...0.9.1)
- Bump .NET SDK from v5.11.2 to v5.12.0 ([#2242](https://github.com/getsentry/sentry-unity/pull/2242))
  - [changelog](https://github.com/getsentry/sentry-dotnet/blob/main/CHANGELOG.md#5120)
  - [diff](https://github.com/getsentry/sentry-dotnet/compare/5.11.2...5.12.0)
- Bump CLI from v2.46.0 to v2.47.1 ([#2232](https://github.com/getsentry/sentry-unity/pull/2232), [#2241](https://github.com/getsentry/sentry-unity/pull/2241))
  - [changelog](https://github.com/getsentry/sentry-cli/blob/master/CHANGELOG.md#2471)
  - [diff](https://github.com/getsentry/sentry-cli/compare/2.46.0...2.47.1)

## 3.2.3

### Significant change in behavior

- The SDK no longer attaches screenshots when capturing errors in the Unity Editor. ([#2163](https://github.com/getsentry/sentry-unity/pull/2163))

### Fixes

- When targeting Android, the SDK no longer causes `SIGABORT` crashes due to `attempting to detach while still running code`. ([#2215](https://github.com/getsentry/sentry-unity/pull/2215))
- The SDK no longer causes crashes with `EXCEPTION_ACCESS_VIOLATION_READ` when using the Mono scripting backend. The SDK now adds the active scene name to the context in IL2CPP builds only ([#2206](https://github.com/getsentry/sentry-unity/pull/2206))

### Dependencies

- Bump .NET SDK from v5.7.0-beta.0 to v5.11.2 ([#2154](https://github.com/getsentry/sentry-unity/pull/2154), [#2188](https://github.com/getsentry/sentry-unity/pull/2188), [#2207](https://github.com/getsentry/sentry-unity/pull/2207), [#2214](https://github.com/getsentry/sentry-unity/pull/2214))
  - [changelog](https://github.com/getsentry/sentry-dotnet/blob/main/CHANGELOG.md#5112)
  - [diff](https://github.com/getsentry/sentry-dotnet/compare/5.7.0-beta.0...5.11.2)
- Bump Java SDK from v8.11.1 to v8.14.0 ([#2155](https://github.com/getsentry/sentry-unity/pull/2155), [#2199](https://github.com/getsentry/sentry-unity/pull/2199))
  - [changelog](https://github.com/getsentry/sentry-java/blob/main/CHANGELOG.md#8140)
  - [diff](https://github.com/getsentry/sentry-java/compare/8.11.1...8.14.0)
- Bump Cocoa SDK from v8.50.1 to v8.51.0 ([#2160](https://github.com/getsentry/sentry-unity/pull/2160), [#2166](https://github.com/getsentry/sentry-unity/pull/2166))
  - [changelog](https://github.com/getsentry/sentry-cocoa/blob/main/CHANGELOG.md#8510)
  - [diff](https://github.com/getsentry/sentry-cocoa/compare/8.50.1...8.51.0)
- Bump Native SDK from v0.8.4 to v0.9.0 ([#2159](https://github.com/getsentry/sentry-unity/pull/2159), [#2186](https://github.com/getsentry/sentry-unity/pull/2186))
  - [changelog](https://github.com/getsentry/sentry-native/blob/master/CHANGELOG.md#090)
  - [diff](https://github.com/getsentry/sentry-native/compare/0.8.4...0.9.0)
- Bump CLI from v2.45.0 to v2.46.0 ([#2176](https://github.com/getsentry/sentry-unity/pull/2176))
  - [changelog](https://github.com/getsentry/sentry-cli/blob/master/CHANGELOG.md#2460)
  - [diff](https://github.com/getsentry/sentry-cli/compare/2.45.0...2.46.0)

## 3.2.2

### Fixes

- When targeting WebGL with an unsupported configuration (i.e. when the exception support is set to `none`), the SDK now shows an error at build time instead of a runtime failure ([#2141](https://github.com/getsentry/sentry-unity/pull/2141))
- When targeting Desktop Platforms, Sentry CLI now respects the SDK's debug logging verbosity ([#2138](https://github.com/getsentry/sentry-unity/pull/2138))
- When targeting iOS and setting `IgnoreCliErrors = true`, the Xcode build will now succeed even if the symbol upload itself failed. This is aimed to allow users to unblock themselves ([#2136](https://github.com/getsentry/sentry-unity/pull/2136)) 
- When targeting iOS, the SDK now correctly updates the Sentry CLI options used for debug symbol upload when appending builds ([#2146](https://github.com/getsentry/sentry-unity/pull/2146))
- Sentry CLI no longer requires the 'Organisation' option, and they have been removed from the configuration window. If you're providing an Organisation right now, nothing changes. Fresh setups will have the option omitted  ([#2137](https://github.com/getsentry/sentry-unity/pull/2137))

### Dependencies

- Bump CLI from v2.43.0 to v2.45.0 ([#2133](https://github.com/getsentry/sentry-unity/pull/2133), [#2143](https://github.com/getsentry/sentry-unity/pull/2143), [#2150](https://github.com/getsentry/sentry-unity/pull/2150))
  - [changelog](https://github.com/getsentry/sentry-cli/blob/master/CHANGELOG.md#2450)
  - [diff](https://github.com/getsentry/sentry-cli/compare/2.43.0...2.45.0)
- Bump Cocoa SDK from v8.49.1 to v8.50.1 ([#2134](https://github.com/getsentry/sentry-unity/pull/2134), [#2142](https://github.com/getsentry/sentry-unity/pull/2142), [#2151](https://github.com/getsentry/sentry-unity/pull/2151))
  - [changelog](https://github.com/getsentry/sentry-cocoa/blob/main/CHANGELOG.md#8501)
  - [diff](https://github.com/getsentry/sentry-cocoa/compare/8.49.1...8.50.1)
- Bump Java SDK from v8.9.0 to v8.11.1 ([#2135](https://github.com/getsentry/sentry-unity/pull/2135), [#2140](https://github.com/getsentry/sentry-unity/pull/2140))
  - [changelog](https://github.com/getsentry/sentry-java/blob/main/CHANGELOG.md#8111)
  - [diff](https://github.com/getsentry/sentry-java/compare/8.9.0...8.11.1)
- Bump .NET SDK from v5.6.0 to v5.7.0-beta.0 ([#2152](https://github.com/getsentry/sentry-unity/pull/2152))
  - [changelog](https://github.com/getsentry/sentry-dotnet/blob/main/CHANGELOG.md#570-beta0)
  - [diff](https://github.com/getsentry/sentry-dotnet/compare/5.6.0...5.7.0-beta0)

## 3.2.1

### Fixes

- Updated the SDK's `proguard.cfg` file to preserve the relevant Java SDK API to keep observing and synching the scope. The SDK no longer fails to set the trace on the native layer ([#2132](https://github.com/getsentry/sentry-unity/pull/2132))

## 3.2.0

### Fixes

- Drastically improved performance of scope sync when targeting Android ([#2107](https://github.com/getsentry/sentry-unity/pull/2107))
- When targeting macOS, the SDK no longer fails to sync the scope to native events ([#2104](https://github.com/getsentry/sentry-unity/pull/2104))
- Updated the options grouping in the configuration window ([#2121](https://github.com/getsentry/sentry-unity/pull/2121))

### Features

- The SDK now links errors and events (managed and native errors) via `trace ID`. This allows you to correlate events captured from different layers of your game ([#1997](https://github.com/getsentry/sentry-unity/pull/1997), [#2089](https://github.com/getsentry/sentry-unity/pull/2089), [#2106](https://github.com/getsentry/sentry-unity/pull/2106))
- The trace used to connect errors on different layers of your game gets regenerated every time the app gains focus, or the active scene changes ([#2123](https://github.com/getsentry/sentry-unity/pull/2123))
- The SDK now reports the game's name as part of the app context ([2083](https://github.com/getsentry/sentry-unity/pull/2083))
- The SDK now reports the active scene's name as part of the `Unity Context` ([2084](https://github.com/getsentry/sentry-unity/pull/2084))

### Dependencies

- Bump Cocoa SDK from v8.45.0 to v8.49.1 ([#2063](https://github.com/getsentry/sentry-unity/pull/2063), [#2071](https://github.com/getsentry/sentry-unity/pull/2071), [#2105](https://github.com/getsentry/sentry-unity/pull/2105), [#2106](https://github.com/getsentry/sentry-unity/pull/2106))
  - [changelog](https://github.com/getsentry/sentry-cocoa/blob/main/CHANGELOG.md#8491)
  - [diff](https://github.com/getsentry/sentry-cocoa/compare/8.45.0...8.49.1)
- Bump Java SDK from v8.3.0 to v8.9.0 ([#2066](https://github.com/getsentry/sentry-unity/pull/2066), [#2075](https://github.com/getsentry/sentry-unity/pull/2075), [#2092](https://github.com/getsentry/sentry-unity/pull/2092), [#2103](https://github.com/getsentry/sentry-unity/pull/2103), [#2111](https://github.com/getsentry/sentry-unity/pull/2111), [#2125](https://github.com/getsentry/sentry-unity/pull/2125))
  - [changelog](https://github.com/getsentry/sentry-java/blob/main/CHANGELOG.md#890)
  - [diff](https://github.com/getsentry/sentry-java/compare/8.3.0...8.9.0)
- Bump CLI from v2.42.2 to v2.43.0 ([#2065](https://github.com/getsentry/sentry-unity/pull/2065), [#2082](https://github.com/getsentry/sentry-unity/pull/2082), [#2085](https://github.com/getsentry/sentry-unity/pull/2085))
  - [changelog](https://github.com/getsentry/sentry-cli/blob/master/CHANGELOG.md#2430)
  - [diff](https://github.com/getsentry/sentry-cli/compare/2.42.2...2.43.0)
- Bump .NET SDK from v5.2.0 to v5.6.0 ([#2067](https://github.com/getsentry/sentry-unity/pull/2067), [#2093](https://github.com/getsentry/sentry-unity/pull/2093), [#2102](https://github.com/getsentry/sentry-unity/pull/2102), [#2126](https://github.com/getsentry/sentry-unity/pull/2126))
  - [changelog](https://github.com/getsentry/sentry-dotnet/blob/main/CHANGELOG.md#560)
  - [diff](https://github.com/getsentry/sentry-dotnet/compare/5.2.0...5.6.0)
- Bump Native SDK from v0.8.1 to v0.8.4 ([#2077](https://github.com/getsentry/sentry-unity/pull/2077), [#2087](https://github.com/getsentry/sentry-unity/pull/2087), [#2117](https://github.com/getsentry/sentry-unity/pull/2117))
  - [changelog](https://github.com/getsentry/sentry-native/blob/master/CHANGELOG.md#084)
  - [diff](https://github.com/getsentry/sentry-native/compare/0.8.1...0.8.4)

## 3.1.0

### Features

- Added `SentryUnity.CrashedLastRun()`. This allows you to check whether the SDK captured a crash the last time the game ran. ([#2049](https://github.com/getsentry/sentry-unity/pull/2049))

### Fixes

- Fixed a potential race condition in the ANR watchdog integration ([2045](https://github.com/getsentry/sentry-unity/pull/2045))

### Dependencies

- Bump .NET SDK from v5.1.1 to v5.2.0 ([#2040](https://github.com/getsentry/sentry-unity/pull/2040))
  - [changelog](https://github.com/getsentry/sentry-dotnet/blob/main/CHANGELOG.md#520)
  - [diff](https://github.com/getsentry/sentry-dotnet/compare/5.1.1...5.2.0)
- Bump Java SDK from v8.2.0 to v8.3.0 ([#2039](https://github.com/getsentry/sentry-unity/pull/2039))
  - [changelog](https://github.com/getsentry/sentry-java/blob/main/CHANGELOG.md#830)
  - [diff](https://github.com/getsentry/sentry-java/compare/8.2.0...8.3.0)
- Bump Native SDK from v0.8.0 to v0.8.1 ([#2047](https://github.com/getsentry/sentry-unity/pull/2047))
  - [changelog](https://github.com/getsentry/sentry-native/blob/master/CHANGELOG.md#081)
  - [diff](https://github.com/getsentry/sentry-native/compare/0.8.0...0.8.1)

## 3.0.2

### Fixes

- Resolved an issue where the SDK would not properly annotate the ViewHierarchy's `AttachmentType`. This would cause it to not render properly on the issues-page on Sentry ([#2036](https://github.com/getsentry/sentry-unity/pull/2036))
- The SDK now ensures that the correct version of the Android SDK gets used during the build. This prevents dependency conflicts and no longer requires "clean" builds to resolve ([#2031](https://github.com/getsentry/sentry-unity/pull/2031))

### Dependencies

- Bump Native SDK from v0.7.20 to v0.8.0 ([#2034](https://github.com/getsentry/sentry-unity/pull/2034))
  - [changelog](https://github.com/getsentry/sentry-native/blob/master/CHANGELOG.md#080)
  - [diff](https://github.com/getsentry/sentry-native/compare/0.7.20...0.8.0)
- Bump CLI from v2.42.1 to v2.42.2 ([#2033](https://github.com/getsentry/sentry-unity/pull/2033))
  - [changelog](https://github.com/getsentry/sentry-cli/blob/master/CHANGELOG.md#2422)
  - [diff](https://github.com/getsentry/sentry-cli/compare/2.42.1...2.42.2)

## 3.0.1

### Fixes

- Then targeting desktop platforms, the SDK now also uploads debugging symbols when native support is disabled. ([#2021](https://github.com/getsentry/sentry-unity/pull/2021))
- The SDK will now upload debugging symobol files for macOS mono builds.([#2021](https://github.com/getsentry/sentry-unity/pull/2021))

### Dependencies

- Bump CLI from v2.41.1 to v2.42.1 ([#2026](https://github.com/getsentry/sentry-unity/pull/2026))
  - [changelog](https://github.com/getsentry/sentry-cli/blob/master/CHANGELOG.md#2421)
  - [diff](https://github.com/getsentry/sentry-cli/compare/2.41.1...2.42.1)

## 3.0.0

### API Changes

- The native layer on mobile platforms (iOS and Android) no longer self-initializes before the Unity game starts.
  Previously, the SDK would use the options at build-time and bake them into the native layer.
  Instead, the SDK will now take the options passed into the `Configure` callback and use those to initialize the native SDKs.
  This allows users to modify the native SDK's options at runtime programmatically.
  The initialization behaviour is controlled by `IosNativeInitializationType` and `AndroidNativeInitializationType` options. These can be set from `Runtime` (default) to `BuildTime` to restore the previous flow and bake the options into the native projects. ([#1915](https://github.com/getsentry/sentry-unity/pull/1915), [#1924](https://github.com/getsentry/sentry-unity/pull/1924))

### Features

- Added `SetBeforeCaptureScreenshot` and `SetBeforeCaptureViewHierarchy` to the options. Users can now choose whether to capture those as attachment on an individual event basis. ([#2023](https://github.com/getsentry/sentry-unity/pull/2023))
- When capturing events via `Debug.LogError`, the SDK now provides stacktraces. Note, that the SDK is currently not able to provide line numbers for these events. ([#1965](https://github.com/getsentry/sentry-unity/pull/1965))
- Added option to enable/disable automatic capture of `Debug.LogError` as event. ([#2009](https://github.com/getsentry/sentry-unity/pull/2009))
- The `Ignore CLI Errors` checkbox in the Debug Symbols tab now applies to all supported platforms. ([#2008](https://github.com/getsentry/sentry-unity/pull/2008))

### Fixes

- The SDK no longer fails to attach the `ViewHierarchy` when the scope has previously been cleared. ([#2020](https://github.com/getsentry/sentry-unity/pull/2020))
- The SDK no longer fails to attach a screenshot when the scope has previously been cleared. ([#2019](https://github.com/getsentry/sentry-unity/pull/2019))
- The SDK's build logs when targeting Android are not a lot less noisy. The SDK will also no longer omit the sentry-cli logs from the gradle build output. ([#1995](https://github.com/getsentry/sentry-unity/pull/1995))
- When targeting iOS and disabling native support, the SDK no longer causes builds to fail with an `Undefined symbol: _SentryNativeBridgeIsEnabled` error. ([#1983](https://github.com/getsentry/sentry-unity/pull/1983))
- The SDK now sets the supported platforms in the `.asmdef` explicitely, preventing runtime issues on currently non-supported platforms ([#1974](https://github.com/getsentry/sentry-unity/pull/1974))
- Fixed iOS native SDK initialization that could cause memory management issues ([#1964](https://github.com/getsentry/sentry-unity/pull/1964))
- The SDK now  properly sets up logging by respecting the debug settings set during the configure callback. Logs created during the configuration of the native SDKs no longer get lost ([#1959](https://github.com/getsentry/sentry-unity/pull/1959))
- ANR events now include the relevant mechanism they have been captured from ([#1955](https://github.com/getsentry/sentry-unity/pull/1955))
- On Android, the SDK no longer freezes the game when failing to sync with the native SDK ([#1927](https://github.com/getsentry/sentry-unity/pull/1927))

### Dependencies

- Bump Native SDK from v0.7.15 to v0.7.20 ([#1928](https://github.com/getsentry/sentry-unity/pull/1928), [#1939](https://github.com/getsentry/sentry-unity/pull/1939), [#1967](https://github.com/getsentry/sentry-unity/pull/1967), [#1981](https://github.com/getsentry/sentry-unity/pull/1981), [#2003](https://github.com/getsentry/sentry-unity/pull/2003))
    - [changelog](https://github.com/getsentry/sentry-native/blob/master/CHANGELOG.md#0720)
    - [diff](https://github.com/getsentry/sentry-native/compare/0.7.15...0.7.20)
- Bump CLI from v2.39.0 to v2.41.1 ([#1922](https://github.com/getsentry/sentry-unity/pull/1922), [#1948](https://github.com/getsentry/sentry-unity/pull/1948), [#1984](https://github.com/getsentry/sentry-unity/pull/1984))
    - [changelog](https://github.com/getsentry/sentry-cli/blob/master/CHANGELOG.md#2411)
    - [diff](https://github.com/getsentry/sentry-cli/compare/2.39.0...2.41.1)
- Bump Cocoa SDK from v8.41.0 to v8.45.0 ([#1937](https://github.com/getsentry/sentry-unity/pull/1937), [#1945](https://github.com/getsentry/sentry-unity/pull/1945), [#1949](https://github.com/getsentry/sentry-unity/pull/1949), [#2001](https://github.com/getsentry/sentry-unity/pull/2001), [#2017](https://github.com/getsentry/sentry-unity/pull/2017))
    - [changelog](https://github.com/getsentry/sentry-cocoa/blob/main/CHANGELOG.md#8450)
    - [diff](https://github.com/getsentry/sentry-cocoa/compare/8.41.0...8.45.0)
- Bump .NET SDK from v4.13.0 to v5.1.1 ([#1940](https://github.com/getsentry/sentry-unity/pull/1940), [#1953](https://github.com/getsentry/sentry-unity/pull/1953), [#2005](https://github.com/getsentry/sentry-unity/pull/2005), [#2018](https://github.com/getsentry/sentry-unity/pull/2018))
    - [changelog](https://github.com/getsentry/sentry-dotnet/blob/main/CHANGELOG.md#511)
    - [diff](https://github.com/getsentry/sentry-dotnet/compare/4.13.0...5.1.1)
- Bump Java SDK from v7.18.0 to v8.2.0 ([#1926](https://github.com/getsentry/sentry-unity/pull/1926), [#1934](https://github.com/getsentry/sentry-unity/pull/1934), [#1946](https://github.com/getsentry/sentry-unity/pull/1946), [#1947](https://github.com/getsentry/sentry-unity/pull/1947), [#2014](https://github.com/getsentry/sentry-unity/pull/2014))
    - [changelog](https://github.com/getsentry/sentry-java/blob/main/CHANGELOG.md#820)
    - [diff](https://github.com/getsentry/sentry-java/compare/7.18.0...8.2.0)

## 2.4.0

### API Changes

- The Runtime- and BuildTime-Configuration have been merged into a single `OptionsConfiguration` script. This allows for programmatic configuration of the SDK in one place using precompile directives. Note, that the current limitation of options for Android and iOS being baked into the project at build time still remains. ([#1888](https://github.com/getsentry/sentry-unity/pull/1888))
This is part of a larger effort to simplify the configuration of the native SDKs and enable users to modify their options at runtime. For more information see https://github.com/getsentry/sentry-unity/issues/1907

### Features

- The SDK now provides a dedicated sentry-cli scriptable config, available on the `Debug Symbols` tab. This allows for programmatic configuration of the used cli-options during build. ([#1887](https://github.com/getsentry/sentry-unity/pull/1887))

### Fixes

- Fix potential NullReferenceException when trying to attach a view hierarchy to an event  ([#1919](https://github.com/getsentry/sentry-unity/pull/1919))
- The SDK no longer sends events when it fails to initialize the native SDK on Windows and Linux and logs those instead. It also suppresses `EntryPointNotFoundException` if sentry-native is not available at runtime. Native crashes won't get capture but it'll continue to capture C# errors. ([#1898](https://github.com/getsentry/sentry-unity/pull/1898))
- The SDK no longer closes the underlying native SDK during the games shutdown if native support has not been enabled. This allows the SDK to support and capture errors in case of building the game as a library on a mobile (Android or iOS) game. ([#1897](https://github.com/getsentry/sentry-unity/pull/1897))

### Dependencies

- Bump .NET SDK from v4.12.1 to v4.13.0 ([#1879](https://github.com/getsentry/sentry-unity/pull/1879), [#1885](https://github.com/getsentry/sentry-unity/pull/1885))
  - [changelog](https://github.com/getsentry/sentry-dotnet/blob/main/CHANGELOG.md#4130)
  - [diff](https://github.com/getsentry/sentry-dotnet/compare/4.12.1...4.13.0)
- Bump CLI from v2.38.1 to v2.39.0 ([#1883](https://github.com/getsentry/sentry-unity/pull/1883), [#1917](https://github.com/getsentry/sentry-unity/pull/1917))
  - [changelog](https://github.com/getsentry/sentry-cli/blob/master/CHANGELOG.md#2390)
  - [diff](https://github.com/getsentry/sentry-cli/compare/2.38.1...2.39.0)
- Bump Native SDK from v0.7.11 to v0.7.15 ([#1886](https://github.com/getsentry/sentry-unity/pull/1886), [#1906](https://github.com/getsentry/sentry-unity/pull/1906), [#1910](https://github.com/getsentry/sentry-unity/pull/1910), [#1913](https://github.com/getsentry/sentry-unity/pull/1913))
  - [changelog](https://github.com/getsentry/sentry-native/blob/master/CHANGELOG.md#0715)
  - [diff](https://github.com/getsentry/sentry-native/compare/0.7.11...0.7.15)
- Bump Cocoa SDK from v8.39.0 to v8.41.0 ([#1890](https://github.com/getsentry/sentry-unity/pull/1890), [#1893](https://github.com/getsentry/sentry-unity/pull/1893), [#1916](https://github.com/getsentry/sentry-unity/pull/1916))
  - [changelog](https://github.com/getsentry/sentry-cocoa/blob/main/CHANGELOG.md#8410)
  - [diff](https://github.com/getsentry/sentry-cocoa/compare/8.39.0...8.41.0)
- Bump Java SDK from v7.16.0 to v7.18.0 ([#1899](https://github.com/getsentry/sentry-unity/pull/1899), [#1905](https://github.com/getsentry/sentry-unity/pull/1905))
  - [changelog](https://github.com/getsentry/sentry-java/blob/main/CHANGELOG.md#7180)
  - [diff](https://github.com/getsentry/sentry-java/compare/7.16.0...7.18.0)

## 2.3.0

### Fixes

- Fixed an issue where the SDK would write the `SampleRate` as an `int` instead of a `float` to the Android Manifest, causing issues during the Android SDK's initialization ([#1872](https://github.com/getsentry/sentry-unity/pull/1872))
- The SDK no longer calls into `Application.persistentDataPath` on unknown platforms. This prevents crashes during startup on platforms with restricted disk access like the Nintendo Switch ([#1870](https://github.com/getsentry/sentry-unity/pull/1870))
- `sentry-native` DLL is now statically linked against Windows CRT. This allows the SDK to provide native support on some Windows systems that do not have CRT installed locally ([#1875](https://github.com/getsentry/sentry-unity/pull/1875))

### Features

- The SDK now also supports capturing native crashes via the WER handler on Windows ([#1873](https://github.com/getsentry/sentry-unity/pull/1873))

### Dependencies

- Bump Java SDK from v7.15.0 to v7.16.0 ([#1862](https://github.com/getsentry/sentry-unity/pull/1862))
  - [changelog](https://github.com/getsentry/sentry-java/blob/main/CHANGELOG.md#7160)
  - [diff](https://github.com/getsentry/sentry-java/compare/7.15.0...7.16.0)
- Bump Native SDK from v0.7.10 to v0.7.11 ([#1867](https://github.com/getsentry/sentry-unity/pull/1867))
  - [changelog](https://github.com/getsentry/sentry-native/blob/master/CHANGELOG.md#0711)
  - [diff](https://github.com/getsentry/sentry-native/compare/0.7.10...0.7.11)
- Bump Cocoa SDK from v8.38.0 to v8.39.0 ([#1868](https://github.com/getsentry/sentry-unity/pull/1868))
  - [changelog](https://github.com/getsentry/sentry-cocoa/blob/main/CHANGELOG.md#8390)
  - [diff](https://github.com/getsentry/sentry-cocoa/compare/8.38.0...8.39.0)
- Bump CLI from v2.37.0 to v2.38.1 ([#1860](https://github.com/getsentry/sentry-unity/pull/1860), [#1874](https://github.com/getsentry/sentry-unity/pull/1874))
  - [changelog](https://github.com/getsentry/sentry-cli/blob/master/CHANGELOG.md#2381)
  - [diff](https://github.com/getsentry/sentry-cli/compare/2.37.0...2.38.1)

## 2.2.2

### Fixes

- The SDK no longer fails to report contexts like `device` and `gpu` ([#1850](https://github.com/getsentry/sentry-unity/pull/1850))

### Dependencies

- Bump Cocoa SDK from v8.37.0 to v8.38.0 ([#1857](https://github.com/getsentry/sentry-unity/pull/1857))
  - [changelog](https://github.com/getsentry/sentry-cocoa/blob/main/CHANGELOG.md#8380)
  - [diff](https://github.com/getsentry/sentry-cocoa/compare/8.37.0...8.38.0)

## 2.2.1

### Dependencies

- Bump .NET SDK from v4.12.0 to v4.12.1 ([#1836](https://github.com/getsentry/sentry-unity/pull/1836))
  - [changelog](https://github.com/getsentry/sentry-dotnet/blob/main/CHANGELOG.md#4121)
  - [diff](https://github.com/getsentry/sentry-dotnet/compare/4.12.0...4.12.1)
- Bump Java SDK from v7.14.0 to v7.15.0 ([#1837](https://github.com/getsentry/sentry-unity/pull/1837))
  - [changelog](https://github.com/getsentry/sentry-java/blob/main/CHANGELOG.md#7150)
  - [diff](https://github.com/getsentry/sentry-java/compare/7.14.0...7.15.0)

## 2.2.0

### API Changes

- The `SentrySdk.Metrics` module is deprecated and will be removed in the next major release.
  Sentry will reject all metrics sent after October 7, 2024.
  Learn more: https://sentry.zendesk.com/hc/en-us/articles/26369339769883-Upcoming-API-Changes-to-Metrics  ([#3619](https://github.com/getsentry/sentry-dotnet/pull/3619))

### Fixes

- The SDK now uses the .NET SDK's `Options.FileWriteDisabled` to opt-out on any writing operations on 'unknown' platforms such as the Nintendo Switch ([1814](https://github.com/getsentry/sentry-unity/pull/1814))
- The SDK no longer freezes the game during shutdown when targeting WebGL ([#3619](https://github.com/getsentry/sentry-dotnet/pull/3619))
- The SDK no longer passes a mangled `gpu.vendorId` to the Android native layer ([#1813](https://github.com/getsentry/sentry-unity/pull/1813))
- Access to the `StackTraceMode` was intended to be restricted due to incompatibility between IL2CPP and `StackTraceMode.Enhanced`. The access modifier has been changed to `private` to avoid pitfall and potential crashes ([#1806](https://github.com/getsentry/sentry-unity/pull/1806))

### Features

- Contexts, such as `device` and `gpu` that the SDK retrieves during the game's startup is now available even earlier and irrespective whether an error was captured on the main or on a background thread ([#1802](https://github.com/getsentry/sentry-unity/pull/1802))
- Added an `ApplicationNotRespondingException` type that allows filtering of ANR events ([#1800](https://github.com/getsentry/sentry-unity/pull/1800))

### Dependencies

- Bump Native SDK from v0.7.9 to v0.7.10 ([#1809](https://github.com/getsentry/sentry-unity/pull/1809))
  - [changelog](https://github.com/getsentry/sentry-native/blob/master/CHANGELOG.md#0710)
  - [diff](https://github.com/getsentry/sentry-native/compare/0.7.9...0.7.10)
- Bump .NET SDK from v4.10.2 to v4.12.0 ([#1823](https://github.com/getsentry/sentry-unity/pull/1823))
  - [changelog](https://github.com/getsentry/sentry-dotnet/blob/main/CHANGELOG.md#4120)
  - [diff](https://github.com/getsentry/sentry-dotnet/compare/4.10.2...4.12.0)
- Bump CLI from v2.34.1 to v2.37.0 ([#1788](https://github.com/getsentry/sentry-unity/pull/1788), [#1792](https://github.com/getsentry/sentry-unity/pull/1792), [#1796](https://github.com/getsentry/sentry-unity/pull/1796), [#1810](https://github.com/getsentry/sentry-unity/pull/1810), [#1815](https://github.com/getsentry/sentry-unity/pull/1815), [#1819](https://github.com/getsentry/sentry-unity/pull/1819), [#1820](https://github.com/getsentry/sentry-unity/pull/1820), [#1822](https://github.com/getsentry/sentry-unity/pull/1822), [#1829](https://github.com/getsentry/sentry-unity/pull/1829))
  - [changelog](https://github.com/getsentry/sentry-cli/blob/master/CHANGELOG.md#2370)
  - [diff](https://github.com/getsentry/sentry-cli/compare/2.34.1...2.37.0)

## 2.1.5

# Fixes

- The update of the Cocoa dependency resolves various crashes on Apple platforms

### Dependencies

- Bump CLI from v2.33.1 to v2.34.1 ([#1781](https://github.com/getsentry/sentry-unity/pull/1781))
  - [changelog](https://github.com/getsentry/sentry-cli/blob/master/CHANGELOG.md#2341)
  - [diff](https://github.com/getsentry/sentry-cli/compare/2.33.1...2.34.1)
- Bump Cocoa SDK from v8.34.0 to v8.36.0 ([#1770](https://github.com/getsentry/sentry-unity/pull/1770), [#1776](https://github.com/getsentry/sentry-unity/pull/1776), [#1778](https://github.com/getsentry/sentry-unity/pull/1778))
  - [changelog](https://github.com/getsentry/sentry-cocoa/blob/main/CHANGELOG.md#8360)
  - [diff](https://github.com/getsentry/sentry-cocoa/compare/8.34.0...8.36.0)
- Bump Native SDK from v0.7.8 to v0.7.9 ([#1780](https://github.com/getsentry/sentry-unity/pull/1780))
  - [changelog](https://github.com/getsentry/sentry-native/blob/master/CHANGELOG.md#079)
  - [diff](https://github.com/getsentry/sentry-native/compare/0.7.8...0.7.9)

## 2.1.4

### Fixes

- The SDK no longer deadlocks during the game shutting down when targeting WebGL ([#1771](https://github.com/getsentry/sentry-unity/pull/1771))
- The SDK no longer fails to report errors after switching scenes when targeting WebGL ([#1754](https://github.com/getsentry/sentry-unity/pull/1754))

### Dependencies

- Bump Java SDK from v7.12.0 to v7.14.0 ([#1751](https://github.com/getsentry/sentry-unity/pull/1751), [#1762](https://github.com/getsentry/sentry-unity/pull/1762))
  - [changelog](https://github.com/getsentry/sentry-java/blob/main/CHANGELOG.md#7140)
  - [diff](https://github.com/getsentry/sentry-java/compare/7.12.0...7.14.0)
- Bump Cocoa SDK from v8.32.0 to v8.34.0 ([#1760](https://github.com/getsentry/sentry-unity/pull/1760), [#1766](https://github.com/getsentry/sentry-unity/pull/1766))
  - [changelog](https://github.com/getsentry/sentry-cocoa/blob/main/CHANGELOG.md#8340)
  - [diff](https://github.com/getsentry/sentry-cocoa/compare/8.32.0...8.34.0)
- Bump Native SDK from v0.7.7 to v0.7.8 ([#1756](https://github.com/getsentry/sentry-unity/pull/1756))
  - [changelog](https://github.com/getsentry/sentry-native/blob/master/CHANGELOG.md#078)
  - [diff](https://github.com/getsentry/sentry-native/compare/0.7.7...0.7.8)
- Bump CLI from v2.33.0 to v2.33.1 ([#1753](https://github.com/getsentry/sentry-unity/pull/1753))
  - [changelog](https://github.com/getsentry/sentry-cli/blob/master/CHANGELOG.md#2331)
  - [diff](https://github.com/getsentry/sentry-cli/compare/2.33.0...2.33.1)
- Bump .NET SDK from v4.9.0 to v4.10.2 ([#1765](https://github.com/getsentry/sentry-unity/pull/1765), [#1768](https://github.com/getsentry/sentry-unity/pull/1768), [#1771](https://github.com/getsentry/sentry-unity/pull/1771))
  - [changelog](https://github.com/getsentry/sentry-dotnet/blob/main/CHANGELOG.md#4102)
  - [diff](https://github.com/getsentry/sentry-dotnet/compare/4.9.0...4.10.2)

## 2.1.3

### Fixes

- The SDK no longer throws `System.DllNotFoundException` during initialization when running a headless Linux server build ([#1748](https://github.com/getsentry/sentry-unity/pull/1748))
- The SDK now correctly uses forward slashes when modifying the gradle project when setting up debug symbol and mapping upload ([#1747](https://github.com/getsentry/sentry-unity/pull/1747))

### Dependencies

- Bump Native SDK from v0.7.6 to v0.7.7 ([#1744](https://github.com/getsentry/sentry-unity/pull/1744))
  - [changelog](https://github.com/getsentry/sentry-native/blob/master/CHANGELOG.md#077)
  - [diff](https://github.com/getsentry/sentry-native/compare/0.7.6...0.7.7)
- Bump Cocoa SDK from v8.31.1 to v8.32.0 ([#1739](https://github.com/getsentry/sentry-unity/pull/1739))
  - [changelog](https://github.com/getsentry/sentry-cocoa/blob/main/CHANGELOG.md#8320)
  - [diff](https://github.com/getsentry/sentry-cocoa/compare/8.31.1...8.32.0)

## 2.1.2

### Fixes

- The SDK no longer causes games to crash out of the box on platforms that have disk access restrictions (i.e. Nintendo Switch) ([#1728](https://github.com/getsentry/sentry-unity/pull/1728))
- The SDK no longer causes crashes on Unity 6 in an attempt to provide line numbers ([#1735](https://github.com/getsentry/sentry-unity/pull/1735))

### Dependencies

- Bump Cocoa SDK from v8.30.1 to v8.31.1 ([#1726](https://github.com/getsentry/sentry-unity/pull/1726))
  - [changelog](https://github.com/getsentry/sentry-cocoa/blob/main/CHANGELOG.md#8311)
  - [diff](https://github.com/getsentry/sentry-cocoa/compare/8.30.1...8.31.1)
- Bump Java SDK from v7.11.0 to v7.12.0 ([#1725](https://github.com/getsentry/sentry-unity/pull/1725))
  - [changelog](https://github.com/getsentry/sentry-java/blob/main/CHANGELOG.md#7120)
  - [diff](https://github.com/getsentry/sentry-java/compare/7.11.0...7.12.0)
- Bump CLI from v2.32.2 to v2.33.0 ([#1733](https://github.com/getsentry/sentry-unity/pull/1733))
  - [changelog](https://github.com/getsentry/sentry-cli/blob/master/CHANGELOG.md#2330)
  - [diff](https://github.com/getsentry/sentry-cli/compare/2.32.2...2.33.0)
- Bump CLI from v2.32.1 to v2.32.2 ([#1724](https://github.com/getsentry/sentry-unity/pull/1724))
  - [changelog](https://github.com/getsentry/sentry-cli/blob/master/CHANGELOG.md#2322)
  - [diff](https://github.com/getsentry/sentry-cli/compare/2.32.1...2.32.2)

## 2.1.1

### Fixes

- Added a fallback for user.id on Android and iOS in case none could be extracted from the native layer ([#1710](https://github.com/getsentry/sentry-unity/pull/1710))
- The IL2CPP exception processor no longer fails when the native support has been disabled ([#1708](https://github.com/getsentry/sentry-unity/pull/1708))
- The SDK now checks whether the Android SDK is available before attempting to initialize it. This prevents `AndroidJavaException: java.lang.ClassNotFoundException: io.sentry.Sentry` from being thrown ([#1714](https://github.com/getsentry/sentry-unity/pull/1714))

### Dependencies

- Bump Cocoa SDK from v8.29.1 to v8.30.1 ([#1702](https://github.com/getsentry/sentry-unity/pull/1702), [#1720](https://github.com/getsentry/sentry-unity/pull/1720))
  - [changelog](https://github.com/getsentry/sentry-cocoa/blob/main/CHANGELOG.md#8301)
  - [diff](https://github.com/getsentry/sentry-cocoa/compare/8.29.1...8.30.1)
- Bump .NET SDK from v4.7.0 to v4.9.0 ([#1704](https://github.com/getsentry/sentry-unity/pull/1704), [#1711](https://github.com/getsentry/sentry-unity/pull/1711), [#1723](https://github.com/getsentry/sentry-unity/pull/1723))
  - [changelog](https://github.com/getsentry/sentry-dotnet/blob/main/CHANGELOG.md#490)
  - [diff](https://github.com/getsentry/sentry-dotnet/compare/4.7.0...4.9.0)
- Bump Java SDK from v7.10.0 to v7.11.0 ([#1709](https://github.com/getsentry/sentry-unity/pull/1709))
  - [changelog](https://github.com/getsentry/sentry-java/blob/main/CHANGELOG.md#7110)
  - [diff](https://github.com/getsentry/sentry-java/compare/7.10.0...7.11.0)

## 2.1.0

### Fixes

- The SDK no longer throws an exception when failing to initialize the native SDK ([#1693](https://github.com/getsentry/sentry-unity/pull/1695))
- On iOS, UnityWebRequests no longer break due to the native SDK appending the request URL a second time ([#1699](https://github.com/getsentry/sentry-unity/pull/1699))
- The SDKs loglevel now also applies to sentry-cli logging ([#1693](https://github.com/getsentry/sentry-unity/pull/1693))
- When targeting Android, sentry-cli will now log to the editor console ([#1693](https://github.com/getsentry/sentry-unity/pull/1691))
- Fixed an issue with the SDK failing to automatically uploading debug symbols when exporting an `.aab` when targeting Android ([#1698](https://github.com/getsentry/sentry-unity/pull/1698))

### Features

- Added an `IgnoreCliErrors` to the Sentry-CLI options, allowing you to ignore errors during symbol and mapping upload ([#1687](https://github.com/getsentry/sentry-unity/pull/1687))

### Fixes

- When exporting Android projects, the SDK will now correctly add the symbol upload at the end of bundling ([#1692](https://github.com/getsentry/sentry-unity/pull/1692))

### Dependencies

- Bump Cocoa SDK from v8.27.0 to v8.29.1 ([#1685](https://github.com/getsentry/sentry-unity/pull/1685), [#1690](https://github.com/getsentry/sentry-unity/pull/1690), [#1694](https://github.com/getsentry/sentry-unity/pull/1694))
  - [changelog](https://github.com/getsentry/sentry-cocoa/blob/main/CHANGELOG.md#8291)
  - [diff](https://github.com/getsentry/sentry-cocoa/compare/8.27.0...8.29.1)
- Bump Java SDK from v7.9.0 to v7.10.0 ([#1686](https://github.com/getsentry/sentry-unity/pull/1686))
  - [changelog](https://github.com/getsentry/sentry-java/blob/main/CHANGELOG.md#7100)
  - [diff](https://github.com/getsentry/sentry-java/compare/7.9.0...7.10.0)
- Bump Native SDK from v0.7.5 to v0.7.6 ([#1688](https://github.com/getsentry/sentry-unity/pull/1688))
  - [changelog](https://github.com/getsentry/sentry-native/blob/master/CHANGELOG.md#076)
  - [diff](https://github.com/getsentry/sentry-native/compare/0.7.5...0.7.6)

## 2.0.7

### Fixes

- The SDK no longer causes deadlocks during synchronization with the native layer on Android ([#1679](https://github.com/getsentry/sentry-unity/pull/1679))
- When targeting Android, builds no longer fail due to errors during symbol upload. These get logged to the console instead ([#1672](https://github.com/getsentry/sentry-unity/pull/1672))

### Dependencies

- Bump .NET SDK from v4.6.2 to v4.7.0 ([#1665](https://github.com/getsentry/sentry-unity/pull/1665))
  - [changelog](https://github.com/getsentry/sentry-dotnet/blob/main/CHANGELOG.md#470)
  - [diff](https://github.com/getsentry/sentry-dotnet/compare/4.6.2...4.7.0)
- Bump Native SDK from v0.7.4 to v0.7.5 ([#1668](https://github.com/getsentry/sentry-unity/pull/1668))
  - [changelog](https://github.com/getsentry/sentry-native/blob/master/CHANGELOG.md#075)
  - [diff](https://github.com/getsentry/sentry-native/compare/0.7.4...0.7.5)
- Bump CLI from v2.31.2 to v2.32.1 ([#1669](https://github.com/getsentry/sentry-unity/pull/1669))
  - [changelog](https://github.com/getsentry/sentry-cli/blob/master/CHANGELOG.md#2321)
  - [diff](https://github.com/getsentry/sentry-cli/compare/2.31.2...2.32.1)
- Bump Cocoa SDK from v8.25.0-6-g8aec30eb to v8.27.0 ([#1680](https://github.com/getsentry/sentry-unity/pull/1680))
  - [changelog](https://github.com/getsentry/sentry-cocoa/blob/main/CHANGELOG.md#8270)
  - [diff](https://github.com/getsentry/sentry-cocoa/compare/8.25.0-6-g8aec30eb...8.27.0)

## 2.0.6

### Fixes

- For Android, we updated the debug symbol upload task on Gradle, to be guaranteed to run last ([#1657](https://github.com/getsentry/sentry-unity/pull/1657))
- The SDK now has improved stacktraces for C++ exceptions on iOS ([#1655](https://github.com/getsentry/sentry-unity/pull/1655))
- The SDK no longer crashes on Android versions 5 and 6 with native support enabled ([#1652](https://github.com/getsentry/sentry-unity/pull/1652))

### Dependencies

- Bump Cocoa SDK from v8.25.2 to v8.26.0 ([#1648](https://github.com/getsentry/sentry-unity/pull/1648))
  - [changelog](https://github.com/getsentry/sentry-cocoa/blob/main/CHANGELOG.md#8260)
  - [diff](https://github.com/getsentry/sentry-cocoa/compare/8.25.2...8.26.0)
- Bump .NET SDK from v4.6.0 to v4.6.2 ([#1653](https://github.com/getsentry/sentry-unity/pull/1653))
  - [changelog](https://github.com/getsentry/sentry-dotnet/blob/main/CHANGELOG.md#462)
  - [diff](https://github.com/getsentry/sentry-dotnet/compare/4.6.0...4.6.2)
- Bump Native SDK from v0.7.2 to v0.7.4 ([#1660](https://github.com/getsentry/sentry-unity/pull/1660))
  - [changelog](https://github.com/getsentry/sentry-native/blob/master/CHANGELOG.md#074)
  - [diff](https://github.com/getsentry/sentry-native/compare/0.7.2...0.7.4)

## 2.0.5

### Fixes

- The automatic debug symbol upload for Android builds now also picks up the ProGuard mapping file. ([#1626](https://github.com/getsentry/sentry-unity/pull/1626))

### Dependencies

- Bump .NET SDK from v4.4.0 to v4.6.0 ([#1635](https://github.com/getsentry/sentry-unity/pull/1635), [#1645](https://github.com/getsentry/sentry-unity/pull/1645))
  - [changelog](https://github.com/getsentry/sentry-dotnet/blob/main/CHANGELOG.md#460)
  - [diff](https://github.com/getsentry/sentry-dotnet/compare/4.4.0...4.6.0)
- Bump CLI from v2.31.0 to v2.31.2 ([#1639](https://github.com/getsentry/sentry-unity/pull/1639))
  - [changelog](https://github.com/getsentry/sentry-cli/blob/master/CHANGELOG.md#2312)
  - [diff](https://github.com/getsentry/sentry-cli/compare/2.31.0...2.31.2)
- Bump Cocoa SDK from v8.24.0 to v8.25.2 ([#1642](https://github.com/getsentry/sentry-unity/pull/1642))
  - [changelog](https://github.com/getsentry/sentry-cocoa/blob/main/CHANGELOG.md#8252)
  - [diff](https://github.com/getsentry/sentry-cocoa/compare/8.24.0...8.25.2)
- Bump Java SDK from v7.8.0 to v7.9.0 ([#1643](https://github.com/getsentry/sentry-unity/pull/1643))
  - [changelog](https://github.com/getsentry/sentry-java/blob/main/CHANGELOG.md#790)
  - [diff](https://github.com/getsentry/sentry-java/compare/7.8.0...7.9.0)

## 2.0.4

### Fixes

- Tweaked the SDK reinstalling the backend to capture native crashes on Windows. C++ exceptions are now getting properly captured again ([#1622](https://github.com/getsentry/sentry-unity/pull/1622))

### Fixes

- Added options to control the order of the SDK's PostGeneratedGradle callback ([#1624](https://github.com/getsentry/sentry-unity/pull/1624))
- The SDK now has a more robust way of dealing with custom gradle templates ([#1624](https://github.com/getsentry/sentry-unity/pull/1624))

### Dependencies

- Bump Java SDK from v7.6.0 to v7.7.0 ([#1610](https://github.com/getsentry/sentry-unity/pull/1610))
  - [changelog](https://github.com/getsentry/sentry-java/blob/main/CHANGELOG.md#770)
  - [diff](https://github.com/getsentry/sentry-java/compare/7.6.0...7.7.0)
- Bump Cocoa SDK from v8.21.0 to v8.24.0 ([#1616](https://github.com/getsentry/sentry-unity/pull/1616))
  - [changelog](https://github.com/getsentry/sentry-cocoa/blob/main/CHANGELOG.md#8240)
  - [diff](https://github.com/getsentry/sentry-cocoa/compare/8.21.0...8.24.0)
- Bump Java SDK from v7.6.0 to v7.8.0 ([#1610](https://github.com/getsentry/sentry-unity/pull/1610), [#1613](https://github.com/getsentry/sentry-unity/pull/1613))
  - [changelog](https://github.com/getsentry/sentry-java/blob/main/CHANGELOG.md#780)
  - [diff](https://github.com/getsentry/sentry-java/compare/7.6.0...7.8.0)
- Bump .NET SDK from v4.2.1 to v4.4.0 ([#1618](https://github.com/getsentry/sentry-unity/pull/1618))
  - [changelog](https://github.com/getsentry/sentry-dotnet/blob/main/CHANGELOG.md#440)
  - [diff](https://github.com/getsentry/sentry-dotnet/compare/4.2.1...4.4.0)

## 2.0.3

### Fixes

- The SDK no longer fails to capture native crashes on Windows. It now properly reinstalls the capturing backend during the initialization ([#1603](https://github.com/getsentry/sentry-unity/pull/1603))
- When building for iOS, the SDK no longer adds its dependencies multiple times (#1558) ([#1595](https://github.com/getsentry/sentry-unity/pull/1595))

### Dependencies

- Bump CLI from v2.30.1 to v2.31.0 ([#1589](https://github.com/getsentry/sentry-unity/pull/1589), [#1600](https://github.com/getsentry/sentry-unity/pull/1600))
  - [changelog](https://github.com/getsentry/sentry-cli/blob/master/CHANGELOG.md#2310)
  - [diff](https://github.com/getsentry/sentry-cli/compare/2.30.1...2.31.0)
- Bump Native SDK from v0.7.0 to v0.7.2 ([#1596](https://github.com/getsentry/sentry-unity/pull/1596), [#1605](https://github.com/getsentry/sentry-unity/pull/1605))
  - [changelog](https://github.com/getsentry/sentry-native/blob/master/CHANGELOG.md#072)
  - [diff](https://github.com/getsentry/sentry-native/compare/0.7.0...0.7.2)

## 2.0.2

### Fixes

- The SDK no longer throws `InvalidOperationExceptions` on platforms that rely on the `UnityWebRequestTransport` (i.e. Switch, Hololens) ([#1587](https://github.com/getsentry/sentry-unity/pull/1587))

### Dependencies

- Bump Cocoa SDK from v8.20.0 to v8.21.0 ([#1575](https://github.com/getsentry/sentry-unity/pull/1575))
  - [changelog](https://github.com/getsentry/sentry-cocoa/blob/main/CHANGELOG.md#8210)
  - [diff](https://github.com/getsentry/sentry-cocoa/compare/8.20.0...8.21.0)
- Bump CLI from v2.28.6 to v2.30.1 ([#1574](https://github.com/getsentry/sentry-unity/pull/1574), [#1578](https://github.com/getsentry/sentry-unity/pull/1578), [#1585](https://github.com/getsentry/sentry-unity/pull/1585))
  - [changelog](https://github.com/getsentry/sentry-cli/blob/master/CHANGELOG.md#2301)
  - [diff](https://github.com/getsentry/sentry-cli/compare/2.28.6...2.30.1)
- Bump Java SDK from v7.5.0 to v7.6.0 ([#1581](https://github.com/getsentry/sentry-unity/pull/1581))
  - [changelog](https://github.com/getsentry/sentry-java/blob/main/CHANGELOG.md#760)
  - [diff](https://github.com/getsentry/sentry-java/compare/7.5.0...7.6.0)
- Bump .NET SDK from v4.1.2 to v4.2.1 ([#1586](https://github.com/getsentry/sentry-unity/pull/1586))
  - [changelog](https://github.com/getsentry/sentry-dotnet/blob/main/CHANGELOG.md#421)
  - [diff](https://github.com/getsentry/sentry-dotnet/compare/4.1.2...4.2.1)

## 2.0.1

### Fixes

- Fixed potential crashes on Android devices by removing the use of the persistent scope observer ([#1555](https://github.com/getsentry/sentry-unity/pull/1570))

### Dependencies

- Bump Java SDK from v7.3.0 to v7.5.0 ([#1569](https://github.com/getsentry/sentry-unity/pull/1569), [#1555](https://github.com/getsentry/sentry-unity/pull/1570))
  - [changelog](https://github.com/getsentry/sentry-java/blob/main/CHANGELOG.md#750)
  - [diff](https://github.com/getsentry/sentry-java/compare/7.3.0...7.5.0)

## 2.0.0

This major release is based on the .NET 4.0 release and includes features like [Metrics](https://docs.sentry.io/platforms/dotnet/metrics/)(preview) and [Spotlight](https://spotlightjs.com/).

### Significant change in behavior

- Transactions' spans are no longer automatically finished with the status `deadline_exceeded` by the transaction. This is now handled by the [Relay](https://github.com/getsentry/relay).
  - Customers self hosting Sentry must use verion 22.12.0 or later ([#3013](https://github.com/getsentry/sentry-dotnet/pull/3013))
- The `User.IpAddress` is now set to `{{auto}}` by default, even when sendDefaultPII is disabled ([#2981](https://github.com/getsentry/sentry-dotnet/pull/2981))
  - The "Prevent Storing of IP Addresses" option in the "Security & Privacy" project settings on sentry.io can be used to control this instead
- The `DiagnosticLogger` signature for `LogWarning` changed to take the `exception` as the first parameter. That way it no longer gets mixed up with the TArgs. ([#2987](https://github.com/getsentry/sentry-dotnet/pull/2987))

### API breaking Changes

If you have compilation errors you can find the affected types or overloads missing in the changelog entries below.

#### Changed APIs

- Class renamed from `Sentry.Attachment` to `Sentry.SentryAttachment` ([#3116](https://github.com/getsentry/sentry-dotnet/pull/3116))
- Class renamed from `Sentry.Constants` to `Sentry.SentryConstants` ([#3125](https://github.com/getsentry/sentry-dotnet/pull/3125))
- Class renamed from `Sentry.Context` to `Sentry.SentryContext` ([#3121](https://github.com/getsentry/sentry-dotnet/pull/3121))
- Class renamed from `Sentry.Hint` to `Sentry.SentryHint` ([#3116](https://github.com/getsentry/sentry-dotnet/pull/3116))
- Class renamed from `Sentry.Package` to `Sentry.SentryPackage` ([#3121](https://github.com/getsentry/sentry-dotnet/pull/3121))
- Class renamed from `Sentry.Request` to `Sentry.SentryRequest` ([#3121](https://github.com/getsentry/sentry-dotnet/pull/3121))
- Class renamed from `Sentry.Runtime` to `Sentry.SentryRuntime` ([#3016](https://github.com/getsentry/sentry-dotnet/pull/3016))
- Class renamed from `Sentry.Session` to `Sentry.SentrySession` ([#3110](https://github.com/getsentry/sentry-dotnet/pull/3110))
- Class renamed from `Sentry.Span` to `Sentry.SentrySpan` ([#3021](https://github.com/getsentry/sentry-dotnet/pull/3021))
- Class renamed from `Sentry.Transaction` to `Sentry.SentryTransaction` ([#3023](https://github.com/getsentry/sentry-dotnet/pull/3023))
- Class renamed from `Sentry.User` to `Sentry.SentryUser` ([#3015](https://github.com/getsentry/sentry-dotnet/pull/3015))
- Interface renamed from `Sentry.IJsonSerializable` to `Sentry.ISentryJsonSerializable` ([#3116](https://github.com/getsentry/sentry-dotnet/pull/3116))
- Interface renamed from `Sentry.ISession` to `Sentry.ISentrySession` ([#3110](https://github.com/getsentry/sentry-dotnet/pull/3110))
- `SentryClient.Dispose` is no longer obsolete ([#2842](https://github.com/getsentry/sentry-dotnet/pull/2842))
- `ISentryClient.CaptureEvent` overloads have been replaced by a single method accepting optional `Hint` and `Scope` parameters. You will need to pass `hint` as a named parameter from code that calls `CaptureEvent` without passing a `scope` argument. ([#2749](https://github.com/getsentry/sentry-dotnet/pull/2749))
- `ITransaction` has been renamed to `ITransactionTracer`. You will need to update any references to these interfaces in your code to use the new interface names ([#2731](https://github.com/getsentry/sentry-dotnet/pull/2731), [#2870](https://github.com/getsentry/sentry-dotnet/pull/2870))
- `TransactionContext` and `SpanContext` constructors were updated. If you're constructing instances of these classes, you will need to adjust the order in which you pass parameters to these. ([#2694](https://github.com/getsentry/sentry-dotnet/pull/2694), [#2696](https://github.com/getsentry/sentry-dotnet/pull/2696))
- The `DiagnosticLogger` signature for `LogError` and `LogFatal` changed to take the `exception` as the first parameter. That way it no longer gets mixed up with the TArgs. The `DiagnosticLogger` now also receives an overload for `LogError` and `LogFatal` that accepts a message only. ([#2715](https://github.com/getsentry/sentry-dotnet/pull/2715))
- `Distribution` added to `IEventLike`. ([#2660](https://github.com/getsentry/sentry-dotnet/pull/2660))
- `StackFrame`'s `ImageAddress`, `InstructionAddress`, and `FunctionId` changed to `long?`. ([#2691](https://github.com/getsentry/sentry-dotnet/pull/2691))
- `DebugImage` and `DebugMeta` moved to `Sentry.Protocol` namespace. ([#2815](https://github.com/getsentry/sentry-dotnet/pull/2815))
- `DebugImage.ImageAddress` changed to `long?`. ([#2725](https://github.com/getsentry/sentry-dotnet/pull/2725))
- Contexts now inherit from `IDictionary` rather than `ConcurrentDictionary`. The specific dictionary being used is an implementation detail. ([#2729](https://github.com/getsentry/sentry-dotnet/pull/2729))

#### Removed APIs

- SentrySinkExtensions.ConfigureSentrySerilogOptions is now internal. If you were using this method, please use one of the `SentrySinkExtensions.Sentry` extension methods instead. ([#2902](https://github.com/getsentry/sentry-dotnet/pull/2902))
- A number of `[Obsolete]` options have been removed ([#2841](https://github.com/getsentry/sentry-dotnet/pull/2841))
  - `BeforeSend` - use `SetBeforeSend` instead.
  - `BeforeSendTransaction` - use `SetBeforeSendTransaction` instead.
  - `BeforeBreadcrumb` - use `SetBeforeBreadcrumb` instead.
  - `CreateHttpClientHandler` - use `CreateHttpMessageHandler` instead.
  - `DisableTaskUnobservedTaskExceptionCapture` method has been renamed to `DisableUnobservedTaskExceptionCapture`.
  - `DebugDiagnosticLogger` - use `TraceDiagnosticLogger` instead.
  - `KeepAggregateException` - this property is no longer used and has no replacement.
  - `ReportAssemblies` - use `ReportAssembliesMode` instead.
- Obsolete `SystemClock` constructor removed, use `SystemClock.Clock` instead. ([#2856](https://github.com/getsentry/sentry-dotnet/pull/2856))
- Obsolete `Runtime.Clone()` removed, this shouldn't have been public in the past and has no replacement. ([#2856](https://github.com/getsentry/sentry-dotnet/pull/2856))
- Obsolete `SentryException.Data` removed, use `SentryException.Mechanism.Data` instead. ([#2856](https://github.com/getsentry/sentry-dotnet/pull/2856))
- Obsolete `AssemblyExtensions` removed, this shouldn't have been public in the past and has no replacement. ([#2856](https://github.com/getsentry/sentry-dotnet/pull/2856))
- Obsolete `SentryDatabaseLogging.UseBreadcrumbs()` removed, it is called automatically and has no replacement. ([#2856](https://github.com/getsentry/sentry-dotnet/pull/2856))
- Obsolete `Scope.GetSpan()` removed, use `Span` property instead. ([#2856](https://github.com/getsentry/sentry-dotnet/pull/2856))
- Obsolete `IUserFactory` removed, use `ISentryUserFactory` instead. ([#2856](https://github.com/getsentry/sentry-dotnet/pull/2856), [#2840](https://github.com/getsentry/sentry-dotnet/pull/2840))
- `IHasMeasurements` has been removed, use `ISpanData` instead. ([#2659](https://github.com/getsentry/sentry-dotnet/pull/2659))
- `IHasBreadcrumbs` has been removed, use `IEventLike` instead. ([#2670](https://github.com/getsentry/sentry-dotnet/pull/2670))
- `ISpanContext` has been removed, use `ITraceContext` instead. ([#2668](https://github.com/getsentry/sentry-dotnet/pull/2668))
- `IHasTransactionNameSource` has been removed, use `ITransactionContext` instead. ([#2654](https://github.com/getsentry/sentry-dotnet/pull/2654))
- ([#2694](https://github.com/getsentry/sentry-dotnet/pull/2694))
- The unused `StackFrame.InstructionOffset` has been removed. ([#2691](https://github.com/getsentry/sentry-dotnet/pull/2691))
- The unused `Scope.Platform` property has been removed. ([#2695](https://github.com/getsentry/sentry-dotnet/pull/2695))
- The obsolete setter `Sentry.PlatformAbstractions.Runtime.Identifier` has been removed ([2764](https://github.com/getsentry/sentry-dotnet/pull/2764))
- `Sentry.Values<T>` is now internal as it is never exposed in the public API ([#2771](https://github.com/getsentry/sentry-dotnet/pull/2771))
- The `TracePropagationTarget` class has been removed, use the `SubstringOrRegexPattern` class instead. ([#2763](https://github.com/getsentry/sentry-dotnet/pull/2763))
- The `WithScope` and `WithScopeAsync` methods have been removed. We have discovered that these methods didn't work correctly in certain desktop contexts, especially when using a global scope. ([#2717](https://github.com/getsentry/sentry-dotnet/pull/2717))
  Replace your usage of `WithScope` with overloads of `Capture*` methods:

  - `SentrySdk.CaptureEvent(SentryEvent @event, Action<Scope> scopeCallback)`
  - `SentrySdk.CaptureMessage(string message, Action<Scope> scopeCallback)`
  - `SentrySdk.CaptureException(Exception exception, Action<Scope> scopeCallback)`

  ```c#
  // Before
  SentrySdk.WithScope(scope =>
  {
    scope.SetTag("key", "value");
    SentrySdk.CaptureEvent(new SentryEvent());
  });

  // After
  SentrySdk.CaptureEvent(new SentryEvent(), scope =>
  {
    // Configure your scope here
    scope.SetTag("key", "value");
  });
  ```

### Features

- Experimental pre-release availability of Metrics. We're exploring the use of Metrics in Sentry. The API will very likely change and we don't yet have any documentation. ([#2949](https://github.com/getsentry/sentry-dotnet/pull/2949))
  - `SentrySdk.Metrics.Set` now additionally accepts `string` as value ([#3092](https://github.com/getsentry/sentry-dotnet/pull/3092))
  - Timing metrics can now be captured with `SentrySdk.Metrics.StartTimer` ([#3075](https://github.com/getsentry/sentry-dotnet/pull/3075))
- Support for [Spotlight](https://spotlightjs.com/), a debug tool for local development. ([#2961](https://github.com/getsentry/sentry-dotnet/pull/2961))
  - Enable it with the option `EnableSpotlight`
  - Optionally configure the URL to connect via `SpotlightUrl`. Defaults to `http://localhost:8969/stream`.

### Dependencies

- Bump .NET SDK from v3.41.3 to v4.0.0 [#1505](https://github.com/getsentry/sentry-unity/pull/1488)
  - [changelog](https://github.com/getsentry/sentry-dotnet/blob/main/CHANGELOG.md#400)
  - [diff](https://github.com/getsentry/sentry-dotnet/compare/3.41.3...4.0.0)
- Bump CLI from v2.24.1 to v2.28.6 ([#1534](https://github.com/getsentry/sentry-unity/pull/1534), [#1539](https://github.com/getsentry/sentry-unity/pull/1539), [#1540](https://github.com/getsentry/sentry-unity/pull/1540), [#1542](https://github.com/getsentry/sentry-unity/pull/1542), [#1547](https://github.com/getsentry/sentry-unity/pull/1547), [#1560](https://github.com/getsentry/sentry-unity/pull/1560), [#1562](https://github.com/getsentry/sentry-unity/pull/1562))
  - [changelog](https://github.com/getsentry/sentry-cli/blob/master/CHANGELOG.md#2286)
  - [diff](https://github.com/getsentry/sentry-cli/compare/2.24.1...2.28.6)
- Bump Native SDK from v0.6.7 to v0.7.0 ([#1535](https://github.com/getsentry/sentry-unity/pull/1535))
  - [changelog](https://github.com/getsentry/sentry-native/blob/master/CHANGELOG.md#070)
  - [diff](https://github.com/getsentry/sentry-native/compare/0.6.7...0.7.0)
- Bump Java SDK from v7.1.0 to v7.3.0 ([#1538](https://github.com/getsentry/sentry-unity/pull/1538), [#1548](https://github.com/getsentry/sentry-unity/pull/1548))
  - [changelog](https://github.com/getsentry/sentry-java/blob/main/CHANGELOG.md#730)
  - [diff](https://github.com/getsentry/sentry-java/compare/7.1.0...7.3.0)
- Bump .NET SDK from v3.41.3 to v3.41.4 ([#1544](https://github.com/getsentry/sentry-unity/pull/1544))
  - [changelog](https://github.com/getsentry/sentry-dotnet/blob/main/CHANGELOG.md#3414)
  - [diff](https://github.com/getsentry/sentry-dotnet/compare/3.41.3...3.41.4)
- Bump Cocoa SDK from v8.18.0 to v8.20.0 ([#1545](https://github.com/getsentry/sentry-unity/pull/1545), [#1549](https://github.com/getsentry/sentry-unity/pull/1549))
  - [changelog](https://github.com/getsentry/sentry-cocoa/blob/main/CHANGELOG.md#8200)
  - [diff](https://github.com/getsentry/sentry-cocoa/compare/8.18.0...8.20.0)

## 1.8.0

### Features

- The static `SentryMonoBehaviour` now has it's `UpdatePauseStatus` public, allowing users to manually update the SDK's internal pause status. This helps work around false positive ANR events when using plugins that take away control from the game i.e. ad frameworks like AppLovin or Ironsource ([#1529](https://github.com/getsentry/sentry-unity/pull/1529))
- It's now possible enable to `CaptureFailedRequests` and the statuscode ranges via the editor. These also apply to the native SDK on `iOS` ([#1514](https://github.com/getsentry/sentry-unity/pull/1514))

### Fixes

- The SDK no longer fails to resolve the debug symbol type on dedicated server builds ([#1522](https://github.com/getsentry/sentry-unity/pull/1522))
- Fixed screenshots not being attached to iOS native crashes ([#1517](https://github.com/getsentry/sentry-unity/pull/1517))

### Dependencies

- Bump CLI from v2.21.2 to v2.24.1 ([#1501](https://github.com/getsentry/sentry-unity/pull/1501), [#1502](https://github.com/getsentry/sentry-unity/pull/1502), [#1525](https://github.com/getsentry/sentry-unity/pull/1525), [#1528](https://github.com/getsentry/sentry-unity/pull/1528), [#1532](https://github.com/getsentry/sentry-unity/pull/1532))
  - [changelog](https://github.com/getsentry/sentry-cli/blob/master/CHANGELOG.md#2241)
  - [diff](https://github.com/getsentry/sentry-cli/compare/2.21.2...2.24.1)
- Bump Cocoa SDK from v8.16.1 to v8.17.1 [#1503](https://github.com/getsentry/sentry-unity/pull/1503, [#1508](https://github.com/getsentry/sentry-unity/pull/1508), [#1520](https://github.com/getsentry/sentry-unity/pull/1520), [#1530](https://github.com/getsentry/sentry-unity/pull/1530))
  - [changelog](https://github.com/getsentry/sentry-cocoa/blob/main/CHANGELOG.md#8180)
  - [diff](https://github.com/getsentry/sentry-cocoa/compare/8.16.1...8.18.0)
- Bump .NET SDK from v3.41.2 to v3.41.3 [#1505](https://github.com/getsentry/sentry-unity/pull/1505)
  - [changelog](https://github.com/getsentry/sentry-dotnet/blob/main/CHANGELOG.md#3413)
  - [diff](https://github.com/getsentry/sentry-dotnet/compare/3.40.1...3.41.2)
- Bump Java SDK from v6.27.0 to v7.1.0 ([#1506](https://github.com/getsentry/sentry-unity/pull/1506), [#1523](https://github.com/getsentry/sentry-unity/pull/1523))
  - [changelog](https://github.com/getsentry/sentry-java/blob/main/CHANGELOG.md#710)
  - [diff](https://github.com/getsentry/sentry-java/compare/6.27.0...7.1.0)

## 1.7.1

### Fixes

- Fix SIGSEV, SIGABRT and SIGBUS crashes happening after/around the August Google Play System update, see [#2955](https://github.com/getsentry/sentry-java/issues/2955) for more details (fix provided by Native SDK bump) ([#1491](https://github.com/getsentry/sentry-unity/pull/1491))
- Fixed an issue with the SDK failing to properly detect application pause and focus lost events and creating false positive ANR events (specifically on Android) ([#1484](https://github.com/getsentry/sentry-unity/pull/1484))

### Dependencies

- Bump CLI from v2.21.2 to v2.21.5 ([#1485](https://github.com/getsentry/sentry-unity/pull/1485), [#1494](https://github.com/getsentry/sentry-unity/pull/1494), [#1495](https://github.com/getsentry/sentry-unity/pull/1495))
  - [changelog](https://github.com/getsentry/sentry-cli/blob/master/CHANGELOG.md#2215)
  - [diff](https://github.com/getsentry/sentry-cli/compare/2.21.2...2.21.5)
- Bump Cocoa SDK from v8.15.0 to v8.16.1 ([#1486](https://github.com/getsentry/sentry-unity/pull/1486), [#1489](https://github.com/getsentry/sentry-unity/pull/1489), [#1497](https://github.com/getsentry/sentry-unity/pull/1497), [#1499](https://github.com/getsentry/sentry-unity/pull/1499))
  - [changelog](https://github.com/getsentry/sentry-cocoa/blob/main/CHANGELOG.md#8161)
  - [diff](https://github.com/getsentry/sentry-cocoa/compare/8.15.0...8.16.1)
- Bump .NET SDK from v3.40.1 to v3.41.2 ([#1487](https://github.com/getsentry/sentry-unity/pull/1487), [#1498](https://github.com/getsentry/sentry-unity/pull/1498), [#1500](https://github.com/getsentry/sentry-unity/pull/1500))
  - [changelog](https://github.com/getsentry/sentry-dotnet/blob/main/CHANGELOG.md#3412)
  - [diff](https://github.com/getsentry/sentry-dotnet/compare/3.40.1...3.41.2)
- Bump Native SDK from v0.6.6 to v0.6.7 ([#1493](https://github.com/getsentry/sentry-unity/pull/1493))
  - [changelog](https://github.com/getsentry/sentry-native/blob/master/CHANGELOG.md#067)
  - [diff](https://github.com/getsentry/sentry-native/compare/0.6.6...0.6.7)

## 1.7.0

### Feature

- Added the dedicated server platforms to the known platforms to prevent the SDK from interpreting them as restricted platforms (i.e. disabling offline caching, session tracking) ([#1468](https://github.com/getsentry/sentry-unity/pull/1468))

### Dependencies

- Bump CLI from v2.21.1 to v2.21.2 ([#1454](https://github.com/getsentry/sentry-unity/pull/1454))
  - [changelog](https://github.com/getsentry/sentry-cli/blob/master/CHANGELOG.md#2212)
  - [diff](https://github.com/getsentry/sentry-cli/compare/2.21.1...2.21.2)
- Bump Native SDK from v0.6.5 to v0.6.6 ([#1457](https://github.com/getsentry/sentry-unity/pull/1457))
  - [changelog](https://github.com/getsentry/sentry-native/blob/master/CHANGELOG.md#066)
  - [diff](https://github.com/getsentry/sentry-native/compare/0.6.5...0.6.6)
- Bump Cocoa SDK from v8.13.0 to v8.15.0 ([#1466](https://github.com/getsentry/sentry-unity/pull/1466), [#1472](https://github.com/getsentry/sentry-unity/pull/1472), [#1473](https://github.com/getsentry/sentry-unity/pull/1473), [#1479](https://github.com/getsentry/sentry-unity/pull/1479))
  - [changelog](https://github.com/getsentry/sentry-cocoa/blob/main/CHANGELOG.md#8150)
  - [diff](https://github.com/getsentry/sentry-cocoa/compare/8.13.0...8.15.0)
- Bump .NET SDK from v3.39.1 to v3.40.1 ([#1464](https://github.com/getsentry/sentry-unity/pull/1464))
  - [changelog](https://github.com/getsentry/sentry-dotnet/blob/main/CHANGELOG.md#3401)
  - [diff](https://github.com/getsentry/sentry-dotnet/compare/3.39.1...3.40.1)

## 1.6.0

### Feature

- The SDK now surfaces options to opt out of the Android `NDK integration` and `NDK Scope Sync`([#1452](https://github.com/getsentry/sentry-unity/pull/1452))

### Fixes

- Fixed IL2CPP line number processor to no longer crash in Unity 2023 builds ([#1450](https://github.com/getsentry/sentry-unity/pull/1450))
- Fixed an issue with the Android dependency setup when using a custom `mainTemplate.gradle` ([#1446](https://github.com/getsentry/sentry-unity/pull/1446))

### Dependencies

- Bump Cocoa SDK from v8.10.0 to v8.13.0 ([#1433](https://github.com/getsentry/sentry-unity/pull/1433), [#1445](https://github.com/getsentry/sentry-unity/pull/1445), [#1451](https://github.com/getsentry/sentry-unity/pull/1451))
  - [changelog](https://github.com/getsentry/sentry-cocoa/blob/main/CHANGELOG.md#8130)
  - [diff](https://github.com/getsentry/sentry-cocoa/compare/8.10.0...8.13.0)
- Bump .NET SDK from v3.36.0 to v3.39.1 ([#1436](https://github.com/getsentry/sentry-unity/pull/1436), [#1443](https://github.com/getsentry/sentry-unity/pull/1443))
  - [changelog](https://github.com/getsentry/sentry-dotnet/blob/main/CHANGELOG.md#3391)
  - [diff](https://github.com/getsentry/sentry-dotnet/compare/3.36.0...3.39.1)
- Bump CLI from v2.20.6 to v2.21.1 ([#1437](https://github.com/getsentry/sentry-unity/pull/1437), [#1447](https://github.com/getsentry/sentry-unity/pull/1447), [#1449](https://github.com/getsentry/sentry-unity/pull/1449))
  - [changelog](https://github.com/getsentry/sentry-cli/blob/master/CHANGELOG.md#2211)
  - [diff](https://github.com/getsentry/sentry-cli/compare/2.20.6...2.21.1)

## 1.5.2

### Fixes

- The SDK no longer creates transactions with their start date set to Jan 01, 001 [#1423](https://github.com/getsentry/sentry-unity/pull/1423)
- The screenshot capture no longer leaks memory ([#1427](https://github.com/getsentry/sentry-unity/pull/1427))

### Dependencies

- Bump Cocoa SDK from v8.9.4 to v8.10.0 ([#1422](https://github.com/getsentry/sentry-unity/pull/1422), [#1424](https://github.com/getsentry/sentry-unity/pull/1424), [#1425](https://github.com/getsentry/sentry-unity/pull/1425))
  - [changelog](https://github.com/getsentry/sentry-cocoa/blob/main/CHANGELOG.md#8100)
  - [diff](https://github.com/getsentry/sentry-cocoa/compare/8.9.4...8.10.0)
- Bump .NET SDK from v3.35.0 to v3.36.0 ([#1423](https://github.com/getsentry/sentry-unity/pull/1423), [#1426](https://github.com/getsentry/sentry-unity/pull/1426))
  - [changelog](https://github.com/getsentry/sentry-dotnet/blob/main/CHANGELOG.md#3360)
  - [diff](https://github.com/getsentry/sentry-dotnet/compare/3.35.0...3.36.0)
- Bump CLI from v2.20.5 to v2.20.6 ([#1430](https://github.com/getsentry/sentry-unity/pull/1430))
  - [changelog](https://github.com/getsentry/sentry-cli/blob/master/CHANGELOG.md#2206)
  - [diff](https://github.com/getsentry/sentry-cli/compare/2.20.5...2.20.6)

## 1.5.1

### Fixes

- Resolved the internal dependency issue with the Android SDK that lead to build time issues like `runtime.jar is missing` and `ClassNotFoundException` during runtime ([#1417](https://github.com/getsentry/sentry-unity/pull/1417))
- The SDK now handles proguardfiles sections indicated by both `consumerProguardFiles` and `proguardFiles` ([#1401](https://github.com/getsentry/sentry-unity/pull/1401))

### Dependencies

- Bump CLI from v2.19.1 to v2.20.5 ([#1387](https://github.com/getsentry/sentry-unity/pull/1387), [#1388](https://github.com/getsentry/sentry-unity/pull/1388), [#1405](https://github.com/getsentry/sentry-unity/pull/1405), [#1408](https://github.com/getsentry/sentry-unity/pull/1408), [#1410](https://github.com/getsentry/sentry-unity/pull/1410), [#1412](https://github.com/getsentry/sentry-unity/pull/1412), [#1419](https://github.com/getsentry/sentry-unity/pull/1419))
  - [changelog](https://github.com/getsentry/sentry-cli/blob/master/CHANGELOG.md#2205)
  - [diff](https://github.com/getsentry/sentry-cli/compare/2.19.1...2.20.5)
- Bump Java SDK from v6.24.0 to v6.27.0 ([#1390](https://github.com/getsentry/sentry-unity/pull/1390), [#1396](https://github.com/getsentry/sentry-unity/pull/1396), [#1400](https://github.com/getsentry/sentry-unity/pull/1400), [#1403](https://github.com/getsentry/sentry-unity/pull/1403), [#1407](https://github.com/getsentry/sentry-unity/pull/1407))
  - [changelog](https://github.com/getsentry/sentry-java/blob/main/CHANGELOG.md#6270)
  - [diff](https://github.com/getsentry/sentry-java/compare/6.24.0...6.27.0)
- Bump Native SDK from v0.6.4 to v0.6.5 ([#1392](https://github.com/getsentry/sentry-unity/pull/1392))
  - [changelog](https://github.com/getsentry/sentry-native/blob/master/CHANGELOG.md#065)
  - [diff](https://github.com/getsentry/sentry-native/compare/0.6.4...0.6.5)
- Bump Cocoa SDK from v8.8.0 to v8.9.4 ([#1397](https://github.com/getsentry/sentry-unity/pull/1397), [#1399](https://github.com/getsentry/sentry-unity/pull/1399), [#1404](https://github.com/getsentry/sentry-unity/pull/1404), [#1406](https://github.com/getsentry/sentry-unity/pull/1406), [#1413](https://github.com/getsentry/sentry-unity/pull/1413))
  - [changelog](https://github.com/getsentry/sentry-cocoa/blob/main/CHANGELOG.md#894)
  - [diff](https://github.com/getsentry/sentry-cocoa/compare/8.8.0...8.9.4)
- Bump .NET SDK from v3.33.1 to v3.35.0 ([#1398](https://github.com/getsentry/sentry-unity/pull/1398), [#1418](https://github.com/getsentry/sentry-unity/pull/1418))
  - [changelog](https://github.com/getsentry/sentry-dotnet/blob/main/CHANGELOG.md#3350)
  - [diff](https://github.com/getsentry/sentry-dotnet/compare/3.33.1...3.35.0)

## 1.5.0

### Fixes

- Fixed an Android build issue where Sentry options would be cached with the first build until Editor restart ([#1379](https://github.com/getsentry/sentry-unity/pull/1379))
- Adding a remote repository filter to the gradle project ([#1367](https://github.com/getsentry/sentry-unity/pull/1367))
- Setting Android SDK version explicit to prevent version conflicts with remote repositories ([#1378](https://github.com/getsentry/sentry-unity/pull/1387))
- Set debug symbol upload logging to debug verbosity ([#1373](https://github.com/getsentry/sentry-unity/pull/1373))
- The SDK no longer causes an exception during initialiation on Android API level 32 and newer ([#1365](https://github.com/getsentry/sentry-unity/pull/1365))
- Suspending Android native support for Mono builds to prevent C# exceptions form causing crashes ([#1362](https://github.com/getsentry/sentry-unity/pull/1362))
- Fixed an issue where the debug image UUID normalization would malform the UUID leading to a failed symbolication ([#1361](https://github.com/getsentry/sentry-unity/pull/1361))

### Feature

- When building for iOS, the debug symbol upload now works irrespective of whether `Bitcode` is enabled or not ([#1381](https://github.com/getsentry/sentry-unity/pull/1381))

### Dependencies

- Bump .NET SDK from v3.33.0 to v3.33.1 ([#1370](https://github.com/getsentry/sentry-unity/pull/1370))
  - [changelog](https://github.com/getsentry/sentry-dotnet/blob/main/CHANGELOG.md#3331)
  - [diff](https://github.com/getsentry/sentry-dotnet/compare/3.33.0...3.33.1)
- Bump CLI from v2.18.1 to v2.19.1 ([#1372](https://github.com/getsentry/sentry-unity/pull/1372), [#1374](https://github.com/getsentry/sentry-unity/pull/1374))
  - [changelog](https://github.com/getsentry/sentry-cli/blob/master/CHANGELOG.md#2191)
  - [diff](https://github.com/getsentry/sentry-cli/compare/2.18.1...2.19.1)
- Bump Cocoa SDK from v8.7.3 to v8.8.0 ([#1371](https://github.com/getsentry/sentry-unity/pull/1371), [#1376](https://github.com/getsentry/sentry-unity/pull/1376))
  - [changelog](https://github.com/getsentry/sentry-cocoa/blob/main/CHANGELOG.md#880)
  - [diff](https://github.com/getsentry/sentry-cocoa/compare/8.7.3...8.8.0)
- Bump Java SDK from v6.21.0 to v6.24.0 ([#1363](https://github.com/getsentry/sentry-unity/pull/1363), [#1375](https://github.com/getsentry/sentry-unity/pull/1375), [#1382](https://github.com/getsentry/sentry-unity/pull/1382))
  - [changelog](https://github.com/getsentry/sentry-java/blob/main/CHANGELOG.md#6240)
  - [diff](https://github.com/getsentry/sentry-java/compare/6.21.0...6.24.0)
- Bump Native SDK from v0.6.3 to v0.6.4 ([#1384](https://github.com/getsentry/sentry-unity/pull/1384))
  - [changelog](https://github.com/getsentry/sentry-native/blob/master/CHANGELOG.md#064)
  - [diff](https://github.com/getsentry/sentry-native/compare/0.6.3...0.6.4)

## 1.4.1

### Fixes

- Fixed Xcode linking error with the SDK disabled ([#1352](https://github.com/getsentry/sentry-unity/pull/1352))
- Fixed an issue triggering an error `Failed to find the closing bracket` when using custom gradle files ([#1359](https://github.com/getsentry/sentry-unity/pull/1359))
- Fixed the Android native integration for builds with Unity 2022.3 and newer ([#1354](https://github.com/getsentry/sentry-unity/pull/1354))

### Dependencies

- Bump Native SDK from v0.6.2 to v0.6.3 ([#1349](https://github.com/getsentry/sentry-unity/pull/1349))
  - [changelog](https://github.com/getsentry/sentry-native/blob/master/CHANGELOG.md#063)
  - [diff](https://github.com/getsentry/sentry-native/compare/0.6.2...0.6.3)
- Bump Java SDK from v6.20.0 to v6.21.0 ([#1353](https://github.com/getsentry/sentry-unity/pull/1353))
  - [changelog](https://github.com/getsentry/sentry-java/blob/main/CHANGELOG.md#6210)
  - [diff](https://github.com/getsentry/sentry-java/compare/6.20.0...6.21.0)

## 1.4.0

### Fixes

- Updated sample configure callback to use the new `BeforeSend`methods that allow the use of `Hints` ([#1341](https://github.com/getsentry/sentry-unity/pull/1341))
- Updated the Sentry CLI command to upload debug symbols ([#1336](https://github.com/getsentry/sentry-unity/pull/1336))

### Feature

- Added automatic filtering of `BadGatewayExceptions` originating from Unity telemetry on the native iOS layer ([#1345](https://github.com/getsentry/sentry-unity/pull/1345))

### Dependencies

- Bump Cocoa SDK from v8.7.2 to v8.7.3 ([#1342](https://github.com/getsentry/sentry-unity/pull/1342))
  - [changelog](https://github.com/getsentry/sentry-cocoa/blob/main/CHANGELOG.md#873)
  - [diff](https://github.com/getsentry/sentry-cocoa/compare/8.7.2...8.7.3)
- Bump Java SDK from v6.18.1 to v6.20.0 ([#1344](https://github.com/getsentry/sentry-unity/pull/1344))
  - [changelog](https://github.com/getsentry/sentry-java/blob/main/CHANGELOG.md#6200)
  - [diff](https://github.com/getsentry/sentry-java/compare/6.18.1...6.20.0)

## 1.3.0

### Fixes

- Fixed native support build integration for Android ([#1318](https://github.com/getsentry/sentry-unity/pull/1318))
- The SDK filters `System.Net.WebException` and `System.Net.Sockets.SocketException` by default ([#1294](https://github.com/getsentry/sentry-unity/pull/1294))
- The SDK no longer runs performance auto instrumentation with the SDK disabled ([#1314](https://github.com/getsentry/sentry-unity/pull/1314))
- Fixed an issue where the SDK would throw a `NullReferenceException` when trying to capture a log message ([#1309](https://github.com/getsentry/sentry-unity/pull/1309))
- Fixed the `BreadcrumbsForErrors` checkbox on the config window ([#1306](https://github.com/getsentry/sentry-unity/pull/1306))
- The SDK filters `Bad Gateway` Exceptions of type `Exception` by default ([#1293](https://github.com/getsentry/sentry-unity/pull/1293))

### Features

- Surfaced debounce times to the options ([#1310](https://github.com/getsentry/sentry-unity/pull/1310))

### Dependencies

- Bump CLI from v2.16.1 to v2.18.1 ([#1288](https://github.com/getsentry/sentry-unity/pull/1288), [#1289](https://github.com/getsentry/sentry-unity/pull/1289), [#1299](https://github.com/getsentry/sentry-unity/pull/1299), [#1329](https://github.com/getsentry/sentry-unity/pull/1329), [#1332](https://github.com/getsentry/sentry-unity/pull/1332))
  - [changelog](https://github.com/getsentry/sentry-cli/blob/master/CHANGELOG.md#2181)
  - [diff](https://github.com/getsentry/sentry-cli/compare/2.16.1...2.18.1)
- Bump Cocoa SDK from v8.3.3 to v8.7.2 ([#1285](https://github.com/getsentry/sentry-unity/pull/1285), [#1298](https://github.com/getsentry/sentry-unity/pull/1298), [#1316](https://github.com/getsentry/sentry-unity/pull/1316), [#1326](https://github.com/getsentry/sentry-unity/pull/1326))
  - [changelog](https://github.com/getsentry/sentry-cocoa/blob/main/CHANGELOG.md#872)
  - [diff](https://github.com/getsentry/sentry-cocoa/compare/8.3.3...8.7.2)
- Bump Java SDK from v6.14.0 to v6.18.1 ([#1300](https://github.com/getsentry/sentry-unity/pull/1300))
  - [changelog](https://github.com/getsentry/sentry-java/blob/main/CHANGELOG.md#6181)
  - [diff](https://github.com/getsentry/sentry-java/compare/6.14.0...6.18.1)
- Bump Native SDK from v0.6.1 to v0.6.2 ([#1308](https://github.com/getsentry/sentry-unity/pull/1308))
  - [changelog](https://github.com/getsentry/sentry-native/blob/master/CHANGELOG.md#062)
  - [diff](https://github.com/getsentry/sentry-native/compare/0.6.1...0.6.2)
- Bump .NET SDK from v3.29.1 to v3.33.0 ([#1307](https://github.com/getsentry/sentry-unity/pull/1307), [#1331](https://github.com/getsentry/sentry-unity/pull/1331))
  - [changelog](https://github.com/getsentry/sentry-dotnet/blob/main/CHANGELOG.md#3330)
  - [diff](https://github.com/getsentry/sentry-dotnet/compare/3.29.1...3.33.0)

## 1.2.0

### Fixes

- Resolved the `Microsoft.Extensions.FileSystemGlobbing.dll` dependency conflict ([#1253](https://github.com/getsentry/sentry-unity/pull/1253))
- Sentry CLI integration when exporting an Android project ([#1242](https://github.com/getsentry/sentry-unity/pull/1242))

### Features

- The SDK now supports exporting the project to iOS without any modifications to Xcode ([#1233](https://github.com/getsentry/sentry-unity/pull/1233))
- The SDK now supports exporting the project to Android without any modifications to gradle ([#1263](https://github.com/getsentry/sentry-unity/pull/1263))

### Dependencies

- Bump CLI from v2.14.4 to v2.16.1 ([#1231](https://github.com/getsentry/sentry-unity/pull/1231), [#1236](https://github.com/getsentry/sentry-unity/pull/1236), [#1251](https://github.com/getsentry/sentry-unity/pull/1251), [#1255](https://github.com/getsentry/sentry-unity/pull/1255))
  - [changelog](https://github.com/getsentry/sentry-cli/blob/master/CHANGELOG.md#2161)
  - [diff](https://github.com/getsentry/sentry-cli/compare/2.14.4...2.16.1)
- Bump Cocoa SDK from v8.3.1 to v8.3.3 ([#1250](https://github.com/getsentry/sentry-unity/pull/1250))
  - [changelog](https://github.com/getsentry/sentry-cocoa/blob/main/CHANGELOG.md#833)
  - [diff](https://github.com/getsentry/sentry-cocoa/compare/8.3.1...8.3.3)
- Bump Native SDK from v0.6.0 to v0.6.1 ([#1254](https://github.com/getsentry/sentry-unity/pull/1254))
  - [changelog](https://github.com/getsentry/sentry-native/blob/master/CHANGELOG.md#061)
  - [diff](https://github.com/getsentry/sentry-native/compare/0.6.0...0.6.1)

## 1.1.0

### Fixes

- Fixed missing debug file upload for assembly definitions for Mono builds ([#1226](https://github.com/getsentry/sentry-unity/pull/1226))
- The ANR detection is now unaffected by changes to `Time.timescale` ([#1225](https://github.com/getsentry/sentry-unity/pull/1225))

### Features

- Added `View Hierarchy` as an opt-in attachment. This will capture the scene hierarchy at the moment an event occurs and send it to Sentry ([#1169](https://github.com/getsentry/sentry-unity/pull/1169))

### Dependencies

- Bump CLI from v2.13.0 to v2.14.4 ([#1213](https://github.com/getsentry/sentry-unity/pull/1213), [#1217](https://github.com/getsentry/sentry-unity/pull/1217))
  - [changelog](https://github.com/getsentry/sentry-cli/blob/master/CHANGELOG.md#2144)
  - [diff](https://github.com/getsentry/sentry-cli/compare/2.13.0...2.14.4)
- Bump .NET SDK from v3.28.1 to v3.29.1 ([#1218](https://github.com/getsentry/sentry-unity/pull/1218), [#1223](https://github.com/getsentry/sentry-unity/pull/1223))
  - [changelog](https://github.com/getsentry/sentry-dotnet/blob/main/CHANGELOG.md#3291)
  - [diff](https://github.com/getsentry/sentry-dotnet/compare/3.28.1...3.29.1)
- Bump Cocoa SDK from v8.2.0 to v8.3.1 ([#1219](https://github.com/getsentry/sentry-unity/pull/1219), [#1228](https://github.com/getsentry/sentry-unity/pull/1228))
  - [changelog](https://github.com/getsentry/sentry-cocoa/blob/main/CHANGELOG.md#831)
  - [diff](https://github.com/getsentry/sentry-cocoa/compare/8.2.0...8.3.1)

## 1.0.0

### Breaking Changes

- Renamed `OptionsConfiguration` to `RuntimeOptionsConfiguration` on the ScriptableSentryOptions ([#1196](https://github.com/getsentry/sentry-unity/pull/1196))\
   If you make use of programmatic runtime options configuration, you will need to reassign the scriptable object in the configuration tab
- Renamed `SentryBuildtimeOptionsConfiguration` to `SentryBuildTimeOptionsConfiguration` ([#1187](https://github.com/getsentry/sentry-unity/pull/1187))\
   If you make use of the programmatic build time configuration, you will need to update your implementation with the base class
- Removed `Override Sentry URL` from editor window ([#1188](https://github.com/getsentry/sentry-unity/pull/1188))\
   The option is still available from within the `SentryBuildTimeOptionsConfiguration`

### Fixes

- The SDK no longer logs a warning due to a missing log file on non-windows player platforms ([#1195](https://github.com/getsentry/sentry-unity/pull/1195))
- Preventing `LoggingIntegration` from registering multiple times ([#1178](https://github.com/getsentry/sentry-unity/pull/1178))
- Fixed the logging integration only capturing tags and missing the message ([#1150](https://github.com/getsentry/sentry-unity/pull/1150))

### Features

- Added Performance Integration options to editor window ([#1198](https://github.com/getsentry/sentry-unity/pull/1198))
- Much improved line numbers for IL2CPP builds by setting the `instruction_addr_adjustment` appropriately ([#1165](https://github.com/getsentry/sentry-unity/pull/1165))
- Added ANR options to the editor window and made ANR timeout accessible on the options object ([#1181](https://github.com/getsentry/sentry-unity/pull/1181))

### Dependencies

- Bump Java SDK from v6.12.1 to v6.14.0 ([#1156](https://github.com/getsentry/sentry-unity/pull/1156), [#1171](https://github.com/getsentry/sentry-unity/pull/1171), [#1184](https://github.com/getsentry/sentry-unity/pull/1184))
  - [changelog](https://github.com/getsentry/sentry-java/blob/main/CHANGELOG.md#6140)
  - [diff](https://github.com/getsentry/sentry-java/compare/6.12.1...6.14.0)
- Bump Native SDK from v0.5.3 to v0.6.0 ([#1157](https://github.com/getsentry/sentry-unity/pull/1157), [#1182](https://github.com/getsentry/sentry-unity/pull/1182))
  - [changelog](https://github.com/getsentry/sentry-native/blob/master/CHANGELOG.md#060)
  - [diff](https://github.com/getsentry/sentry-native/compare/0.5.3...0.6.0)
- Bump CLI from v2.11.0 to v2.13.0 ([#1163](https://github.com/getsentry/sentry-unity/pull/1163), [#1186](https://github.com/getsentry/sentry-unity/pull/1186))
  - [changelog](https://github.com/getsentry/sentry-cli/blob/master/CHANGELOG.md#2130)
  - [diff](https://github.com/getsentry/sentry-cli/compare/2.11.0...2.13.0)
- Bump .NET SDK from v3.26.2 to v3.28.1 ([#1164](https://github.com/getsentry/sentry-unity/pull/1164), [#1170](https://github.com/getsentry/sentry-unity/pull/1170), [#1172](https://github.com/getsentry/sentry-unity/pull/1172), [#1175](https://github.com/getsentry/sentry-unity/pull/1175))
  - [changelog](https://github.com/getsentry/sentry-dotnet/blob/main/CHANGELOG.md#3281)
  - [diff](https://github.com/getsentry/sentry-dotnet/compare/3.26.2...3.28.1)
- Bump Cocoa SDK from v7.31.5 to v8.2.0 ([#1162](https://github.com/getsentry/sentry-unity/pull/1162), [#1199](https://github.com/getsentry/sentry-unity/pull/1199))
  - [changelog](https://github.com/getsentry/sentry-cocoa/blob/main/CHANGELOG.md#820)
  - [diff](https://github.com/getsentry/sentry-cocoa/compare/7.31.5...8.2.0)

## 0.28.1

### Fixes

- Fixed conflicting default name for scriptable options configuration scripts ([#1146](https://github.com/getsentry/sentry-unity/pull/1146))
- Made inlined helpers on the macOS bridge static ([#1148](https://github.com/getsentry/sentry-unity/pull/1148))

### Dependencies

- Bump Java SDK from v6.11.0 to v6.12.1 ([#1143](https://github.com/getsentry/sentry-unity/pull/1143))
  - [changelog](https://github.com/getsentry/sentry-java/blob/main/CHANGELOG.md#6121)
  - [diff](https://github.com/getsentry/sentry-java/compare/6.11.0...6.12.1)
- Bump .NET SDK from v3.26.0 to v3.26.2 ([#1142](https://github.com/getsentry/sentry-unity/pull/1142), [#1152](https://github.com/getsentry/sentry-unity/pull/1152))
  - [changelog](https://github.com/getsentry/sentry-dotnet/blob/main/CHANGELOG.md#3262)
  - [diff](https://github.com/getsentry/sentry-dotnet/compare/3.26.0...3.26.2)

## 0.28.0

### Breaking Changes

- Fixed the SDK creating warnings before initializing when loading the options.
  - `Sentry.Unity.ScriptableOptionsConfiguration` changed to `SentryRuntimeOptionsConfiguration`
  - and `Sentry.Unity.Editor.ScriptableOptionsConfiguration` changed to `SentryBuildtimeOptionsConfiguration`
    If you make use of the programmatic configuration, you will need to update your implementation with those base classes ([#1128](https://github.com/getsentry/sentry-unity/pull/1128))

### Fixes

- Removed build GUID from automated release creation to keep events from different layers in the same release ([#1127](https://github.com/getsentry/sentry-unity/pull/1127))
- Fixed an issue related to the IL2CPP line number feature where a C# exception could lead to a crash ([#1126](https://github.com/getsentry/sentry-unity/pull/1126))
- No longer log warnings about missing IL2CPP methods when running in the Editor ([#1132](https://github.com/getsentry/sentry-unity/pull/1132))

### Features

- Mono PDB files upload during build ([#1106](https://github.com/getsentry/sentry-unity/pull/1106))

### Dependencies

- Bump Java SDK from v6.9.1 to v6.11.0 ([#1107](https://github.com/getsentry/sentry-unity/pull/1107), [#1122](https://github.com/getsentry/sentry-unity/pull/1122), [#1133](https://github.com/getsentry/sentry-unity/pull/1133))
  - [changelog](https://github.com/getsentry/sentry-java/blob/main/CHANGELOG.md#6110)
  - [diff](https://github.com/getsentry/sentry-java/compare/6.9.1...6.11.0)
- Bump Native SDK from v0.5.2 to v0.5.3 ([#1109](https://github.com/getsentry/sentry-unity/pull/1109))
  - [changelog](https://github.com/getsentry/sentry-native/blob/master/CHANGELOG.md#053)
  - [diff](https://github.com/getsentry/sentry-native/compare/0.5.2...0.5.3)
- Bump Cocoa SDK from v7.31.3 to v7.31.5 ([#1115](https://github.com/getsentry/sentry-unity/pull/1115), [#1129](https://github.com/getsentry/sentry-unity/pull/1129))
  - [changelog](https://github.com/getsentry/sentry-cocoa/blob/8.0.0/CHANGELOG.md#7315)
  - [diff](https://github.com/getsentry/sentry-cocoa/compare/7.31.3...7.31.5)
- Bump .NET SDK from v3.24.0 to v3.26.0 ([#1121](https://github.com/getsentry/sentry-unity/pull/1121), [#1137](https://github.com/getsentry/sentry-unity/pull/1137))
  - [changelog](https://github.com/getsentry/sentry-dotnet/blob/main/CHANGELOG.md#3260)
  - [diff](https://github.com/getsentry/sentry-dotnet/compare/3.24.0...3.26.0)
- Bump CLI from v2.10.0 to v2.11.0 ([#1124](https://github.com/getsentry/sentry-unity/pull/1124))
  - [changelog](https://github.com/getsentry/sentry-cli/blob/master/CHANGELOG.md#2110)
  - [diff](https://github.com/getsentry/sentry-cli/compare/2.10.0...2.11.0)

## 0.27.0

### Features

- Added missing SDK closing functionality ([#1092](https://github.com/getsentry/sentry-unity/pull/1092))

### Fixes

- Fixed logging for automated debug symbol upload for iOS ([#1091](https://github.com/getsentry/sentry-unity/pull/1091))

### Dependencies

- Bump Cocoa SDK from v7.30.2 to v7.31.3 ([#1079](https://github.com/getsentry/sentry-unity/pull/1079), [#1082](https://github.com/getsentry/sentry-unity/pull/1082), [#1089](https://github.com/getsentry/sentry-unity/pull/1089), [#1097](https://github.com/getsentry/sentry-unity/pull/1097))
  - [changelog](https://github.com/getsentry/sentry-cocoa/blob/master/CHANGELOG.md#7313)
  - [diff](https://github.com/getsentry/sentry-cocoa/compare/7.30.2...7.31.3)
- Bump CLI from v2.8.1 to v2.10.0 ([#1080](https://github.com/getsentry/sentry-unity/pull/1080), [#1101](https://github.com/getsentry/sentry-unity/pull/1101))
  - [changelog](https://github.com/getsentry/sentry-cli/blob/master/CHANGELOG.md#2100)
  - [diff](https://github.com/getsentry/sentry-cli/compare/2.8.1...2.10.0)
- Bump .NET SDK from v3.23.1 to v3.24.0 ([#1090](https://github.com/getsentry/sentry-unity/pull/1090))
  - [changelog](https://github.com/getsentry/sentry-dotnet/blob/main/CHANGELOG.md#3240)
  - [diff](https://github.com/getsentry/sentry-dotnet/compare/3.23.1...3.24.0)
- Bump Java SDK from v6.7.0 to v6.9.1 ([#1083](https://github.com/getsentry/sentry-unity/pull/1083), [#1088](https://github.com/getsentry/sentry-unity/pull/1088), [#1098](https://github.com/getsentry/sentry-unity/pull/1098))
  - [changelog](https://github.com/getsentry/sentry-java/blob/main/CHANGELOG.md#691)
  - [diff](https://github.com/getsentry/sentry-java/compare/6.7.0...6.9.1)

## 0.26.0

### Features

- Added Unity version to event context ([#1072](https://github.com/getsentry/sentry-unity/pull/1072))
- Add build-time `ScriptableOptionsConfiguration` scripting interface to support changing settings for native integrations and CLI ([#1046](https://github.com/getsentry/sentry-unity/pull/1046))

### Fixes

- Auto Instrumentation now correctly resolves prebuilt assemblies ([#1066](https://github.com/getsentry/sentry-unity/pull/1066))
- Newly created `ScriptableOptionsConfiguration` script not being set in editor window UI ([#1046](https://github.com/getsentry/sentry-unity/pull/1046))

### Dependencies

- Bump Cocoa SDK from v7.30.1 to v7.30.2 ([#1075](https://github.com/getsentry/sentry-unity/pull/1075))
  - [changelog](https://github.com/getsentry/sentry-cocoa/blob/master/CHANGELOG.md#7302)
  - [diff](https://github.com/getsentry/sentry-cocoa/compare/7.30.1...7.30.2)

## 0.25.1

### Fixes

- Resolved conflicting dependencies for Mono.Cecil ([#1064](https://github.com/getsentry/sentry-unity/pull/1064))

### Dependencies

- Bump .NET SDK from v3.23.0 to v3.23.1 ([#1062](https://github.com/getsentry/sentry-unity/pull/1062))
  - [changelog](https://github.com/getsentry/sentry-dotnet/blob/main/CHANGELOG.md#3231)
  - [diff](https://github.com/getsentry/sentry-dotnet/compare/3.23.0...3.23.1)
- Bump CLI from v2.8.0 to v2.8.1 ([#1061](https://github.com/getsentry/sentry-unity/pull/1061))
  - [changelog](https://github.com/getsentry/sentry-cli/blob/master/CHANGELOG.md#281)
  - [diff](https://github.com/getsentry/sentry-cli/compare/2.8.0...2.8.1)
- Bump Cocoa SDK from v7.29.0 to v7.30.1 ([#1067](https://github.com/getsentry/sentry-unity/pull/1067), [#1069](https://github.com/getsentry/sentry-unity/pull/1069))
  - [changelog](https://github.com/getsentry/sentry-cocoa/blob/master/CHANGELOG.md#7301)
  - [diff](https://github.com/getsentry/sentry-cocoa/compare/v7.29.0...7.30.1)
- Bump Java SDK from v6.6.0 to v6.7.0 ([#1070](https://github.com/getsentry/sentry-unity/pull/1070))
  - [changelog](https://github.com/getsentry/sentry-java/blob/main/CHANGELOG.md#670)
  - [diff](https://github.com/getsentry/sentry-java/compare/6.6.0...6.7.0)

## 0.25.0

### Fixes

- Removing Sentry.framework from being copied as ResourceBundle ([#1056](https://github.com/getsentry/sentry-unity/pull/1056))

### Features

- Automated Performance Instrumentation for MonoBehaviour.Awake methods ([#998](https://github.com/getsentry/sentry-unity/pull/998))

### Dependencies

- Bump Java SDK from v6.5.0 to v6.6.0 ([#1042](https://github.com/getsentry/sentry-unity/pull/1042))
  - [changelog](https://github.com/getsentry/sentry-java/blob/main/CHANGELOG.md#660)
  - [diff](https://github.com/getsentry/sentry-java/compare/6.5.0...6.6.0)
- Bump .NET SDK from v3.22.0 to v3.23.0 ([#1054](https://github.com/getsentry/sentry-unity/pull/1054))
  - [changelog](https://github.com/getsentry/sentry-dotnet/blob/main/CHANGELOG.md#3230)
  - [diff](https://github.com/getsentry/sentry-dotnet/compare/3.22.0...3.23.0)

## 0.24.0

### Fixes

- Disabled Android lifecycle breadcrumbs ([#1038](https://github.com/getsentry/sentry-unity/pull/1038))
- Additional IL2CPP arguments get added only once ([#997](https://github.com/getsentry/sentry-unity/pull/997))
- Releasing temp render texture after capturing a screenshot ([#983](https://github.com/getsentry/sentry-unity/pull/983))
- Automatic screenshot mirroring on select devices. ([#1019](https://github.com/getsentry/sentry-unity/pull/1019))
- Hide `StackTraceMode` from options to avoid accidental errors when used with IL2CPP ([#1033](https://github.com/getsentry/sentry-unity/pull/1033))

### Features

- Added flags per LogType for automatically adding breadcrumbs ([#1030](https://github.com/getsentry/sentry-unity/pull/1030))
- The Cocoa SDK is now bundled as `.xcframework` ([#1002](https://github.com/getsentry/sentry-unity/pull/1002))
- Automated Performance Instrumentation for Runtime Initialization ([#991](https://github.com/getsentry/sentry-unity/pull/991))
- Automated Performance Instrumentation for Scene Loading ([#768](https://github.com/getsentry/sentry-unity/pull/768))

### Dependencies

- Bump Java SDK from v6.4.1 to v6.5.0 ([#980](https://github.com/getsentry/sentry-unity/pull/980), [#1005](https://github.com/getsentry/sentry-unity/pull/1005), [#1011](https://github.com/getsentry/sentry-unity/pull/1011))
  - [changelog](https://github.com/getsentry/sentry-java/blob/main/CHANGELOG.md#650)
  - [diff](https://github.com/getsentry/sentry-java/compare/6.4.1...6.5.0)
- Bump CLI from v2.5.2 to v2.8.0 ([#986](https://github.com/getsentry/sentry-unity/pull/986), [#999](https://github.com/getsentry/sentry-unity/pull/999), [#1043](https://github.com/getsentry/sentry-unity/pull/1043))
  - [changelog](https://github.com/getsentry/sentry-cli/blob/master/CHANGELOG.md#280)
  - [diff](https://github.com/getsentry/sentry-cli/compare/2.5.2...2.8.0)
- Bump Cocoa SDK from v7.25.1 to v7.29.0 ([#988](https://github.com/getsentry/sentry-unity/pull/988), [#996](https://github.com/getsentry/sentry-unity/pull/996), [#1004](https://github.com/getsentry/sentry-unity/pull/1004), [#1016](https://github.com/getsentry/sentry-unity/pull/1016), [#1041](https://github.com/getsentry/sentry-unity/pull/1041))
  - [changelog](https://github.com/getsentry/sentry-cocoa/blob/master/CHANGELOG.md#7290)
  - [diff](https://github.com/getsentry/sentry-cocoa/compare/7.25.1...7.29.0)
- Bump .NET SDK from v3.21.0 to v3.22.0 ([#1008](https://github.com/getsentry/sentry-unity/pull/1008))
  - [changelog](https://github.com/getsentry/sentry-dotnet/blob/main/CHANGELOG.md#3220)
  - [diff](https://github.com/getsentry/sentry-dotnet/compare/3.21.0...3.22.0)
- Bump Native SDK from v0.5.0 to v0.5.2 ([#1022](https://github.com/getsentry/sentry-unity/pull/1022), [#1032](https://github.com/getsentry/sentry-unity/pull/1032))
  - [changelog](https://github.com/getsentry/sentry-native/blob/master/CHANGELOG.md#052)
  - [diff](https://github.com/getsentry/sentry-native/compare/0.5.0...0.5.2)

## 0.23.1

### Fixes

- Don't access Unity `AnalyticsSessionInfo.userId` on unknown platforms ([#971](https://github.com/getsentry/sentry-unity/pull/971))
- Keep previously set IL2CPP compiler arguments (i.e append instead of overwriting) ([#972](https://github.com/getsentry/sentry-unity/pull/972))
- Add transaction processor ([#978](https://github.com/getsentry/sentry-unity/pull/978))

### Dependencies

- Bump Cocoa SDK from v7.24.1 to v7.25.1 ([#967](https://github.com/getsentry/sentry-unity/pull/967), [#974](https://github.com/getsentry/sentry-unity/pull/974))
  - [changelog](https://github.com/getsentry/sentry-cocoa/blob/master/CHANGELOG.md#7251)
  - [diff](https://github.com/getsentry/sentry-cocoa/compare/7.24.1...7.25.1)

## 0.23.0

### Fixes

- Fixed a crash during startup due in connection to the Google Ads Unity package ([#953](https://github.com/getsentry/sentry-unity/pull/953))
- The SDK failing to reinstall the backend will no longer lead to events being sent to Sentry ([#962](https://github.com/getsentry/sentry-unity/pull/962))
- Don't access Unity `AnalyticsSessionInfo.userId` on unknown platforms ([#971](https://github.com/getsentry/sentry-unity/pull/971))

### Features

- IL2CPP line number support is enabled by default ([#963](https://github.com/getsentry/sentry-unity/pull/963))

### Dependencies

- Bump Java SDK from v6.4.0 to v6.4.1 ([#954](https://github.com/getsentry/sentry-unity/pull/954))
  - [changelog](https://github.com/getsentry/sentry-java/blob/main/CHANGELOG.md#641)
  - [diff](https://github.com/getsentry/sentry-java/compare/6.4.0...6.4.1)
- Bump Cocoa SDK from v7.23.0 to v7.24.1 ([#957](https://github.com/getsentry/sentry-unity/pull/957), [#961](https://github.com/getsentry/sentry-unity/pull/961))
  - [changelog](https://github.com/getsentry/sentry-cocoa/blob/master/CHANGELOG.md#7241)
  - [diff](https://github.com/getsentry/sentry-cocoa/compare/7.23.0...7.24.1)
- Bump .NET SDK from v3.20.1-33-g76b13448 to v3.21.0 ([#958](https://github.com/getsentry/sentry-unity/pull/958))
  - [changelog](https://github.com/getsentry/sentry-dotnet/blob/main/CHANGELOG.md#3210)
  - [diff](https://github.com/getsentry/sentry-dotnet/compare/3.20.1-33-g76b13448...3.21.0)

## 0.22.2

### Fixes

- Fixed an 'Undefined symbols' issue within the Sentry Native Bridge when building for iOS ([#932](https://github.com/getsentry/sentry-unity/pull/932))
- ANR detection no longer creates an error by trying to capture a screenshot from a background thread ([#937](https://github.com/getsentry/sentry-unity/pull/937))
- Screenshots quality no longer scales off of current resolution but tries match thresholds instead ([#939](https://github.com/getsentry/sentry-unity/pull/939))

### Features

- Bump CLI from v2.5.0 to v2.5.2 ([#938](https://github.com/getsentry/sentry-unity/pull/938))
  - [changelog](https://github.com/getsentry/sentry-cli/blob/master/CHANGELOG.md#252)
  - [diff](https://github.com/getsentry/sentry-cli/compare/2.5.0...2.5.2)
- Bump Java SDK from v6.3.1 to v6.4.0 ([#943](https://github.com/getsentry/sentry-unity/pull/943))
  - [changelog](https://github.com/getsentry/sentry-java/blob/main/CHANGELOG.md#640)
  - [diff](https://github.com/getsentry/sentry-java/compare/6.3.1...6.4.0)

## 0.22.1

### Fixes

- iOS samples were missing the Objective-C plugin ([#921](https://github.com/getsentry/sentry-unity/pull/921))
- Save SampleRate to Options.asset ([#916](https://github.com/getsentry/sentry-unity/pull/916))
- Increase CLI file upload limit to 10 MiB ([#922](https://github.com/getsentry/sentry-unity/pull/922))

### Features

- Bump Cocoa SDK to v7.23.0 ([#918](https://github.com/getsentry/sentry-unity/pull/918))
  - [changelog](https://github.com/getsentry/sentry-cocoa/blob/master/CHANGELOG.md#7230)
  - [diff](https://github.com/getsentry/sentry-cocoa/compare/7.22.0...7.23.0)
- Bump Java SDK to v6.3.1 ([#926](https://github.com/getsentry/sentry-unity/pull/926))
  - [changelog](https://github.com/getsentry/sentry-java/blob/main/CHANGELOG.md#631)
  - [diff](https://github.com/getsentry/sentry-java/compare/6.3.0...6.3.1)
- Bump Native SDK to v0.5.0 ([#924](https://github.com/getsentry/sentry-unity/pull/924))
  - [changelog](https://github.com/getsentry/sentry-native/blob/master/CHANGELOG.md#050)
  - [diff](https://github.com/getsentry/sentry-native/compare/0.4.18...0.5.0)

## 0.22.0

### Fixes

- Correctly byte-swap ELF build-ids ([#872](https://github.com/getsentry/sentry-unity/pull/872))
- Wizard autoselects with only one option available ([#891](https://github.com/getsentry/sentry-unity/pull/891))
- Reenabled IL2CPP line number support for mobile ([#902](https://github.com/getsentry/sentry-unity/pull/902))

### Features

- Added parameters to the options to control screenshot quality ([#912](https://github.com/getsentry/sentry-unity/pull/912))
- Added highlights and info messages to editor configuration window ([#910](https://github.com/getsentry/sentry-unity/pull/910))
- Added programmatic access to enable the experimental IL2CPP line numbers support ([#905](https://github.com/getsentry/sentry-unity/pull/905))
- Bump Java SDK to v6.3.0 ([#887](https://github.com/getsentry/sentry-unity/pull/887), [#894](https://github.com/getsentry/sentry-unity/pull/894))
  - [changelog](https://github.com/getsentry/sentry-java/blob/main/CHANGELOG.md#630)
  - [diff](https://github.com/getsentry/sentry-java/compare/6.1.4...6.3.0)
- Bump CLI to v2.5.0 ([#889](https://github.com/getsentry/sentry-unity/pull/889), [#898](https://github.com/getsentry/sentry-unity/pull/898))
  - [changelog](https://github.com/getsentry/sentry-cli/blob/master/CHANGELOG.md#250)
  - [diff](https://github.com/getsentry/sentry-cli/compare/2.3.1...2.5.0)
- Bump Cocoa SDK to v7.22.0 ([#892](https://github.com/getsentry/sentry-unity/pull/892), [#909](https://github.com/getsentry/sentry-unity/pull/909))
  - [changelog](https://github.com/getsentry/sentry-cocoa/blob/master/CHANGELOG.md#7220)
  - [diff](https://github.com/getsentry/sentry-cocoa/compare/7.20.0...7.22.0)
- Bump .NET SDK to v3.20.1 ([#907](https://github.com/getsentry/sentry-unity/pull/907), [#911](https://github.com/getsentry/sentry-unity/pull/911))
  - [changelog](https://github.com/getsentry/sentry-dotnet/blob/main/CHANGELOG.md#3201)
  - [diff](https://github.com/getsentry/sentry-dotnet/compare/3.19.0...3.20.1)

## 0.21.0

### Fixes

- Fix ProGuard setup if build.gradle uses CRLF (Windows) line breaks ([#885](https://github.com/getsentry/sentry-unity/pull/885))

### Features

- Attach screenshots to native errors on iOS ([#878](https://github.com/getsentry/sentry-unity/pull/878))
- Bump CLI to v2.3.1 ([#875](https://github.com/getsentry/sentry-unity/pull/875))
  - [changelog](https://github.com/getsentry/sentry-cli/blob/master/CHANGELOG.md#231)
  - [diff](https://github.com/getsentry/sentry-cli/compare/2.3.0...2.3.1)
- Bump Cocoa SDK to v7.20.0 ([#877](https://github.com/getsentry/sentry-unity/pull/877))
  - [changelog](https://github.com/getsentry/sentry-cocoa/blob/master/CHANGELOG.md#7200)
  - [diff](https://github.com/getsentry/sentry-cocoa/compare/7.19.0...7.20.0)

## 0.20.1

### Fixes

- Explicitly disable Windows x86 native-error tracking and IL2CPP processing integration ([#871](https://github.com/getsentry/sentry-unity/pull/871))

## 0.20.0

### Features

- Generate and upload IL2CPP line mappings alongside debug files ([#790](https://github.com/getsentry/sentry-unity/pull/790))
- Launch a setup wizard after installation ([#780](https://github.com/getsentry/sentry-unity/pull/780))
- Reduced automated screenshot attachment controls to a simple toggle ([#784](https://github.com/getsentry/sentry-unity/pull/784))
- Disable AutoSessionTracking on unknown platforms ([#840](https://github.com/getsentry/sentry-unity/pull/840))
- Support Android apps minified with Proguard ([#844](https://github.com/getsentry/sentry-unity/pull/844))
- Bump Cocoa SDK to v7.19.0 ([#802](https://github.com/getsentry/sentry-unity/pull/802), [#821](https://github.com/getsentry/sentry-unity/pull/821), [#835](https://github.com/getsentry/sentry-unity/pull/835), [#854](https://github.com/getsentry/sentry-unity/pull/854), [#868](https://github.com/getsentry/sentry-unity/pull/868))
  - [changelog](https://github.com/getsentry/sentry-cocoa/blob/master/CHANGELOG.md#7190)
  - [diff](https://github.com/getsentry/sentry-cocoa/compare/7.16.0...7.19.0)
- Bump .NET SDK to v3.19.0 ([#807](https://github.com/getsentry/sentry-unity/pull/807), [#860](https://github.com/getsentry/sentry-unity/pull/860))
  - [changelog](https://github.com/getsentry/sentry-dotnet/blob/main/CHANGELOG.md#3190)
  - [diff](https://github.com/getsentry/sentry-dotnet/compare/3.17.0...3.19.0)
- Bump Java SDK to v6.1.4 ([#811](https://github.com/getsentry/sentry-unity/pull/811), [#820](https://github.com/getsentry/sentry-unity/pull/820), [#828](https://github.com/getsentry/sentry-unity/pull/828), [#847](https://github.com/getsentry/sentry-unity/pull/847), [#857](https://github.com/getsentry/sentry-unity/pull/857))
  - [changelog](https://github.com/getsentry/sentry-java/blob/main/CHANGELOG.md#614)
  - [diff](https://github.com/getsentry/sentry-java/compare/6.0.0-1-gc62a9f7a...6.1.4)
- Bump Native SDK to v0.4.18 ([#810](https://github.com/getsentry/sentry-unity/pull/810), [#824](https://github.com/getsentry/sentry-unity/pull/824))
  - [changelog](https://github.com/getsentry/sentry-native/blob/master/CHANGELOG.md#0418)
  - [diff](https://github.com/getsentry/sentry-native/compare/0.4.15-7-g9eecb1b...0.4.18)
- Bump CLI to v2.3.0 ([#826](https://github.com/getsentry/sentry-unity/pull/826), [#867](https://github.com/getsentry/sentry-unity/pull/867))
  - [changelog](https://github.com/getsentry/sentry-cli/blob/master/CHANGELOG.md#230)
  - [diff](https://github.com/getsentry/sentry-cli/compare/1.71.0...2.3.0)

### Fixes

- Only use application-not-responding detection in C#, disable in native plugins ([#852](https://github.com/getsentry/sentry-unity/pull/852))
- The SDK no longer accesses the disk on unknown platforms (e.g. Switch) ([#865](https://github.com/getsentry/sentry-unity/pull/865))

## 0.19.0

### Features

- Add rich context info to native crashes ([#747](https://github.com/getsentry/sentry-unity/pull/747))
- Include build ID in an event release info ([#795](https://github.com/getsentry/sentry-unity/pull/795))

### Fixes

- Don't report Application-Not-Responding while the app is in the background ([#796](https://github.com/getsentry/sentry-unity/pull/796))

## 0.18.0

### Features

- Capture Native Instruction Addrs for Exceptions ([#683](https://github.com/getsentry/sentry-unity/pull/683))
- Enable native crash support with Mono scripting backend on Android, Windows and Linux ([#751](https://github.com/getsentry/sentry-unity/pull/751))
- Application-Not-Responding detection ([#771](https://github.com/getsentry/sentry-unity/pull/771))
- Allow uploading sources for debug files ([#773](https://github.com/getsentry/sentry-unity/pull/773))
- Bump Sentry Java SDK to v6.0.0 ([#787](https://github.com/getsentry/sentry-unity/pull/787))
  - [changelog](https://github.com/getsentry/sentry-java/blob/6.0.0/CHANGELOG.md?plain=1#L3..L73)
  - [diff](https://github.com/getsentry/sentry-java/compare/6.0.0-rc.1...6.0.0)

## 0.17.0

### Features

- The SDK now uses the Unity Log Handler to capture errors, instead of manually parsing events received on Application.logMessageReceived ([#731](https://github.com/getsentry/sentry-unity/pull/731))
- Linux native crash support ([#734](https://github.com/getsentry/sentry-unity/pull/734))
- Collect context information synchronously during init to capture it for very early events ([#744](https://github.com/getsentry/sentry-unity/pull/744))
- Automatic user IDs on native crashes & .NET events ([#728](https://github.com/getsentry/sentry-unity/pull/728))
- Use single-threaded HTTP transport on unknown platforms ([#756](https://github.com/getsentry/sentry-unity/pull/756))
- Disable offline caching on unknown platforms ([#770](https://github.com/getsentry/sentry-unity/pull/770))
- Bump Sentry Cocoa SDK to v7.16.0 ([#725](https://github.com/getsentry/sentry-unity/pull/725))
  - [changelog](https://github.com/getsentry/sentry-cocoa/blob/7.16.0/CHANGELOG.md?plain=1#L3..L38)
  - [diff](https://github.com/getsentry/sentry-cocoa/compare/7.13.0...7.16.0)
- Bump Sentry Java SDK to v6.0.0-rc.1 ([#725](https://github.com/getsentry/sentry-unity/pull/725))
  - [changelog](https://github.com/getsentry/sentry-java/blob/6.0.0-rc.1/CHANGELOG.md?plain=1#L3..L79)
  - [diff](https://github.com/getsentry/sentry-java/compare/6.0.0-alpha.4...6.0.0-rc.1)

## 0.16.0

### Features

- macOS native crash support ([#710](https://github.com/getsentry/sentry-unity/pull/710))
- The SentryUnityOptions now provide a method to disable the UnityLoggingIntegration ([#724](https://github.com/getsentry/sentry-unity/pull/724))

### Fixes

- The automated debug symbol upload now works with Unity 2021.2 and newer ([#730](https://github.com/getsentry/sentry-unity/pull/730))
- Dropped support for Sentry options as Json ([#709](https://github.com/getsentry/sentry-unity/pull/709))
  - If you're migrating from version 0.3.0 or older, make sure to upgrade to 0.15.0 first, as it is the last version supporting the automated conversion of the options as Json file to a Scriptable Object.
- Bump Sentry .NET SDK 3.17.0 ([#726](https://github.com/getsentry/sentry-unity/pull/726))
  - [changelog 3.17.0](https://github.com/getsentry/sentry-dotnet/blob/3.17.0/CHANGELOG.md)
  - [diff](https://github.com/getsentry/sentry-dotnet/compare/3.16.0...3.17.0)

## 0.15.0

### Features

- WebGL - .NET support ([#657](https://github.com/getsentry/sentry-unity/pull/657))

### Fixes

- The SDK no longer creates a custom link.xml ([#707](https://github.com/getsentry/sentry-unity/pull/707))
- Fixed `DebugOnlyInEditor` only applying to the Unity logger ([#706](https://github.com/getsentry/sentry-unity/pull/706))
- Sentry no longer fails to send events in Unity 2019.4 IL2CPP builds for macOS ([#701](https://github.com/getsentry/sentry-unity/pull/701))
- Bump Sentry Cocoa SDK 7.13.0 ([#697](https://github.com/getsentry/sentry-unity/pull/697))
  - [changelog](https://github.com/getsentry/sentry-cocoa/blob/7.13.0/CHANGELOG.md)
  - [diff](https://github.com/getsentry/sentry-cocoa/compare/7.7.0...7.13.0)

## 0.14.0

### Features

- Capture `Debug.LogError()` and `Debug.LogException()` also on background threads ([#673](https://github.com/getsentry/sentry-unity/pull/673))
- Adding override for Sentry CLI URL ([#666](https://github.com/getsentry/sentry-unity/pull/666))
- Option to automatically attach screenshots to all events ([#670](https://github.com/getsentry/sentry-unity/pull/670))

### Fixes

- Refactor InApp logic from Stack Traces ([#661](https://github.com/getsentry/sentry-unity/pull/661))
- Whitespaces no longer cause issues when uploading symbols for Windows native ([#655](https://github.com/getsentry/sentry-unity/pull/655))
- AndroidManifest update removes previous `io.sentry` entries ([#652](https://github.com/getsentry/sentry-unity/pull/652))
- Bump Sentry .NET SDK 3.16.0 ([#678](https://github.com/getsentry/sentry-unity/pull/678))
  - [changelog 3.16.0](https://github.com/getsentry/sentry-dotnet/blob/3.16.0/CHANGELOG.md)
  - [diff](https://github.com/getsentry/sentry-dotnet/compare/3.14.1...3.16.0)
- Bump Sentry Java SDK to v6.0.0-alpha.4 ([#653](https://github.com/getsentry/sentry-unity/pull/653))
  - [changelog](https://github.com/getsentry/sentry-java/blob/6.0.0-alpha.4/CHANGELOG.md)
  - [diff](https://github.com/getsentry/sentry-java/compare/5.5.0...6.0.0-alpha.4)

## 0.13.0

### Features

- Windows - include sentry.dll & .pdb in debug symbol upload ([#641](https://github.com/getsentry/sentry-unity/pull/641))

### Fixes

- Resolved issue of the SDK accessing the same cache directory with multiple game instances running ([#643](https://github.com/getsentry/sentry-unity/pull/643))
- Bump Sentry .NET SDK 3.15.0 ([#639](https://github.com/getsentry/sentry-unity/pull/639))
  - [changelog](https://github.com/getsentry/sentry-dotnet/blob/3.15.0/CHANGELOG.md)
  - [diff](https://github.com/getsentry/sentry-dotnet/compare/3.14.1...3.15.0)

## 0.12.0

### Features

- The SDK no longer depends on Unity's iOS module for non iOS builds ([#601](https://github.com/getsentry/sentry-unity/pull/601))
- Update native SDK to v0.4.16 (pre-release) ([#615](https://github.com/getsentry/sentry-unity/pull/615))
- Windows native support (64-bit)
  - native crash handler ([#380](https://github.com/getsentry/sentry-unity/pull/380))
  - configuration & log forwarding ([#577](https://github.com/getsentry/sentry-unity/pull/577))
  - scope synchronization ([#546](https://github.com/getsentry/sentry-unity/pull/546))
  - symbol upload while building through Unity ([#607](https://github.com/getsentry/sentry-unity/pull/607))

### Fixes

- iOS builds no longer break when native support disabled or not available ([#592](https://github.com/getsentry/sentry-unity/pull/592))
- Close sentry instance when quitting the app ([#608](https://github.com/getsentry/sentry-unity/pull/608))
- iOS options.CrashedLastRun() reported an incorrect value ([#615](https://github.com/getsentry/sentry-unity/pull/615))

## 0.11.0

### Features

- Config window support for programmatic options configuration ([#569](https://github.com/getsentry/sentry-unity/pull/569))

## 0.10.1

### Features

- Samples include programmatic options configuration snippet ([#568](https://github.com/getsentry/sentry-unity/pull/568))
- Support for programmatic options configuration ([#564](https://github.com/getsentry/sentry-unity/pull/564))

### Fixes

- Bump Sentry .NET SDK 3.14.1 ([#573](https://github.com/getsentry/sentry-unity/pull/573))
  - [changelog](https://github.com/getsentry/sentry-dotnet/blob/3.14.1/CHANGELOG.md)
  - [diff](https://github.com/getsentry/sentry-dotnet/compare/3.14.0...3.14.1)

## 0.10.0

### Features

- Updated native samples to only display buttons for the currently active platform ([#551](https://github.com/getsentry/sentry-unity/pull/551))
- Unity logged exceptions are marked as unhandled by default ([#542](https://github.com/getsentry/sentry-unity/pull/542))

### Fixes

- Sentry.Unity.Editor.iOS.dll no longer breaks builds on Windows when the iOS module has not been installed ([#559](https://github.com/getsentry/sentry-unity/pull/559))
- Importing the link.xml when opening the config window no longer causes an infinite loop ([#539](https://github.com/getsentry/sentry-unity/pull/539))
- Bump Sentry .NET SDK 3.14.0 ([#561](https://github.com/getsentry/sentry-unity/pull/561))
  - [changelog](https://github.com/getsentry/sentry-dotnet/blob/3.14.0/CHANGELOG.md)
  - [diff](https://github.com/getsentry/sentry-dotnet/compare/3.13.0...3.14.0)

## 0.9.4

### Fixes

- Android Native Support no longer crashes when built with Mono while trying to capture a crash ([#524](https://github.com/getsentry/sentry-unity/pull/524))
- Bump Sentry CLI 1.72.0 ([#526](https://github.com/getsentry/sentry-unity/pull/526))
  - [changelog](https://github.com/getsentry/sentry-cli/releases/tag/1.72.0)
  - [diff](https://github.com/getsentry/sentry-cli/compare/1.71.0...1.72.0)
- Bump Sentry .NET SDK 3.13.0 ([#503](https://github.com/getsentry/sentry-unity/pull/503))
  - [changelog](https://github.com/getsentry/sentry-dotnet/blob/3.13.0/CHANGELOG.md)
  - [diff](https://github.com/getsentry/sentry-dotnet/compare/3.12.3...3.13.0)

## 0.9.3

### Fixes

- Automated Android symbols upload now uses valid paths on Windows ([#509](https://github.com/getsentry/sentry-unity/pull/509))

## 0.9.2

### Features

- Tag whether event was created in the UI thread ([#495](https://github.com/getsentry/sentry-unity/pull/495))

### Fixes

- Fix missing Sentry/Sentry.h ([#504](https://github.com/getsentry/sentry-unity/pull/504))
- Automated Android symbols upload now correctly escapes sentry-cli executable path ([#507](https://github.com/getsentry/sentry-unity/pull/507))

## 0.9.1

### Fixes

- Suppress errors when adding attachments ([#485](https://github.com/getsentry/sentry-unity/pull/485))

## 0.9.0

### Features

- Sample scene with custom context and screenshot ([#472](https://github.com/getsentry/sentry-unity/pull/472))

### Fixes

- Initializing the SDK with an options object won't bypass default option values ([#469](https://github.com/getsentry/sentry-unity/pull/469))
- Fixed overwriting Xcode build properties ([#466](https://github.com/getsentry/sentry-unity/pull/466))
- Xcode exports no longer break with sentry-cli already added ([#457](https://github.com/getsentry/sentry-unity/pull/457))
- Explicitly set <SignAssembly>false</SignAssembly> ([#470](https://github.com/getsentry/sentry-unity/pull/470)). So that Sentry.dll is not strong named when consumed inside Unity.
- Bump Sentry .NET SDK 3.12.3 ([#484](https://github.com/getsentry/sentry-unity/pull/484))
  - [changelog](https://github.com/getsentry/sentry-dotnet/blob/3.12.3/CHANGELOG.md)
  - [diff](https://github.com/getsentry/sentry-dotnet/compare/3.11.1...3.12.3)
- Bump Sentry Cocoa SDK 7.7.0 ([#481](https://github.com/getsentry/sentry-unity/pull/481))
  - [changelog](https://github.com/getsentry/sentry-cocoa/blob/7.7.0/CHANGELOG.md)
  - [diff](https://github.com/getsentry/sentry-cocoa/compare/7.5.2...7.7.0)
- Bump Sentry Android SDK 5.5.0 ([#474](https://github.com/getsentry/sentry-unity/pull/474))
  - [changelog](https://github.com/getsentry/sentry-java/blob/5.5.0/CHANGELOG.md)
  - [diff](https://github.com/getsentry/sentry-java/compare/5.4.0...5.5.0)

## 0.8.0

### Features

- ScriptableSentryUnityOptions is now public ([#419](https://github.com/getsentry/sentry-unity/pull/419))
- Automated symbols upload for iOS builds when bitcode is enabled ([#444](https://github.com/getsentry/sentry-unity/pull/444))
- Automated symbols upload for iOS builds when bitcode is disabled ([#443](https://github.com/getsentry/sentry-unity/pull/443))

### Fixes

- Android: Automated symbol upload no longer breaks non IL2CPP builds ([#450](https://github.com/getsentry/sentry-unity/pull/450))
- Config window now saves changes to sentry-cli options ([#454](https://github.com/getsentry/sentry-unity/pull/454))
- Sentry no longer requires Xcode projects to be exported on macOS ([#442](https://github.com/getsentry/sentry-unity/pull/442))

## 0.7.0

### Features

- Added automated symbols upload support for Android ([#408](https://github.com/getsentry/sentry-unity/pull/408))
- Avoid DLL conflict with other Unity packages ([#425](https://github.com/getsentry/sentry-unity/issues/425))

## 0.6.2

- fix release packaging ([#417](https://github.com/getsentry/sentry-unity/pull/417))

## 0.6.1

### Features

- Added sentry-cli to Unity package ([#414](https://github.com/getsentry/sentry-unity/pull/414))

### Fixes

- Added missing release string validation ([#389](https://github.com/getsentry/sentry-unity/pull/389))
- Sentry internal logs no longer show up as breadcrumbs ([#377](https://github.com/getsentry/sentry-unity/pull/377))
- Fixed missing context data when initializing SDK programmatically ([#376](https://github.com/getsentry/sentry-unity/pull/376))
- Fixed CaptureInEditor flag when initializing SDK programmatically ([#370](https://github.com/getsentry/sentry-unity/pull/370))
- Preventing numeric options to be set negative in the editor window ([#364](https://github.com/getsentry/sentry-unity/pull/364))
- Bump Sentry .NET SDK 3.11.1 ([#407](https://github.com/getsentry/sentry-unity/pull/407))
  - [changelog](https://github.com/getsentry/sentry-dotnet/blob/3.11.1/CHANGELOG.md)
  - [diff](https://github.com/getsentry/sentry-dotnet/compare/3.9.3...3.11.1)
- Bump Sentry Cocoa SDK 7.5.2 ([#407](https://github.com/getsentry/sentry-unity/pull/407))
  - [changelog](https://github.com/getsentry/sentry-cocoa/blob/7.5.2/CHANGELOG.md)
  - [diff](https://github.com/getsentry/sentry-cocoa/compare/7.3.0...7.5.2)
- Bump Sentry Android SDK 5.4.0 ([#411](https://github.com/getsentry/sentry-unity/pull/411))
  - [changelog](https://github.com/getsentry/sentry-java/blob/5.4.0/CHANGELOG.md)
  - [diff](https://github.com/getsentry/sentry-java/compare/5.2.1...5.4.0)

## 0.6.0

### Features

- Added iOS simulator support ([#358](https://github.com/getsentry/sentry-unity/pull/358))
- Android Native Support ([#307](https://github.com/getsentry/sentry-unity/pull/307))
- Android mark sessions as crashed ([#347](https://github.com/getsentry/sentry-unity/pull/347))
- Android native bridge for scope sync ([#308](https://github.com/getsentry/sentry-unity/pull/308))
- iOS native bridge for scope sync ([#296](https://github.com/getsentry/sentry-unity/pull/296))
- Sample: Throw exceptions in C++ and Objective-C. C++ segfault ([#342](https://github.com/getsentry/sentry-unity/pull/342))
- Update Unity from 2019.4.21f to 2019.4.30f ([#350](https://github.com/getsentry/sentry-unity/pull/350))

### Fixes

- Fixed Xcode generation with invalid or disabled SentryOptions ([#330](https://github.com/getsentry/sentry-unity/pull/330))
- Fixed iOS support related reference resolution issue for Windows ([#325](https://github.com/getsentry/sentry-unity/pull/325))
- Import link.xml caused an infinite loop ([#315](https://github.com/getsentry/sentry-unity/pull/315))
- Removed unused .asmdefs which clears a warning from console ([#316](https://github.com/getsentry/sentry-unity/pull/316))
- Don't send negative line number ([#317](https://github.com/getsentry/sentry-unity/pull/317))
- Android SDK: re-installation of native backend through C# ([#339](https://github.com/getsentry/sentry-unity/pull/339))
- Bump Sentry .NET SDK 3.9.3 ([#328](https://github.com/getsentry/sentry-unity/pull/328))
  - [changelog](https://github.com/getsentry/sentry-dotnet/blob/3.9.3/CHANGELOG.md)
  - [diff](https://github.com/getsentry/sentry-dotnet/compare/3.9.0...3.9.3)
- Bump Sentry Cocoa SDK 7.3.0 ([#328](https://github.com/getsentry/sentry-unity/pull/328))
  - [changelog](https://github.com/getsentry/sentry-cocoa/blob/7.3.0/CHANGELOG.md)
  - [diff](https://github.com/getsentry/sentry-cocoa/compare/7.2.2...7.3.0)
- Bump Sentry Android SDK 5.2.1 ([#359](https://github.com/getsentry/sentry-unity/pull/359))
  - [changelog](https://github.com/getsentry/sentry-java/blob/5.2.1/CHANGELOG.md)
  - [diff](https://github.com/getsentry/sentry-java/compare/5.2.0-beta.3...5.2.1)

## 0.5.2

### Features

- Operating System reported as raw_description and parsed by Sentry ([#305](https://github.com/getsentry/sentry-unity/pull/305))
- Release & Environment now sync with native options ([#298](https://github.com/getsentry/sentry-unity/pull/298))
- Bump Sentry .NET SDK 3.9.0 ([#299](https://github.com/getsentry/sentry-unity/pull/299))
  - [changelog](https://github.com/getsentry/sentry-dotnet/blob/3.9.0/CHANGELOG.md)
  - [diff](https://github.com/getsentry/sentry-dotnet/compare/3.8.3...3.9.0)

## 0.5.1

### Fixes

- Removed editor flag checks from options validation during build process ([#295](https://github.com/getsentry/sentry-unity/pull/295))
- By default, don't block Sentry.Init up to 2 seconds to flush events ([#291](https://github.com/getsentry/sentry-unity/pull/291))

## 0.5.0

### Features

- iOS native support ([#254](https://github.com/getsentry/sentry-unity/pull/254))
- Compile Initialization with the game ([#272](https://github.com/getsentry/sentry-unity/pull/272))
- Native crash in sample ([#270](https://github.com/getsentry/sentry-unity/pull/270))
- Cache, background threads and data for UnityEventProcessor ([#268](https://github.com/getsentry/sentry-unity/pull/268))

### Fixes

- Bump Sentry Cocoa SDK 7.2.2 ([#289](https://github.com/getsentry/sentry-unity/pull/289))
  - [changelog](https://github.com/getsentry/sentry-cocoa/blob/7.2.2/CHANGELOG.md)
  - [diff](https://github.com/getsentry/sentry-cocoa/compare/7.2.0-beta.7...7.2.2)
- Included NativeExample.c in sample project ([#288](https://github.com/getsentry/sentry-unity/pull/288))
- Added attribute to SentryInitialization to prevent codestripping of Init ([#285](https://github.com/getsentry/sentry-unity/pull/285))
- Fixed passing Sentry diagnostic level to iOS native layer ([#281](https://github.com/getsentry/sentry-unity/pull/281))
- Fixed stuck traces sample rate slider ([#276](https://github.com/getsentry/sentry-unity/pull/276))
- Fixed selected input field tab glitches ([#276](https://github.com/getsentry/sentry-unity/pull/276))
- Bump Sentry .NET SDK 3.8.3 ([#269](https://github.com/getsentry/sentry-unity/pull/269))
  - [changelog](https://github.com/getsentry/sentry-dotnet/blob/main/CHANGELOG.md#383)
  - [diff](https://github.com/getsentry/sentry-dotnet/compare/3.8.2...3.8.3)

## 0.4.3

### Features

- Log in single line ([#262](https://github.com/getsentry/sentry-unity/pull/262))

### Fixes

- Bump Sentry .NET SDK 3.8.2 ([#263](https://github.com/getsentry/sentry-unity/pull/263))
  - [changelog](https://github.com/getsentry/sentry-dotnet/blob/main/CHANGELOG.md#382)
  - [diff](https://github.com/getsentry/sentry-dotnet/compare/3.8.1...3.8.2)

## 0.4.2

### Features

- Added IsGlobalModeEnabled to SetDefaults ([#260](https://github.com/getsentry/sentry-unity/pull/260))

## 0.4.1

### Fixes

- Bump Sentry .NET SDK 3.8.1 ([#258](https://github.com/getsentry/sentry-unity/pull/258))
  - [changelog](https://github.com/getsentry/sentry-dotnet/blob/main/CHANGELOG.md#381)
  - [diff](https://github.com/getsentry/sentry-dotnet/compare/3.8.0...3.8.1)

## 0.4.0

### Features

- Main thread data ([#245](https://github.com/getsentry/sentry-unity/pull/245))
- New config window ([#243](https://github.com/getsentry/sentry-unity/pull/243))
- Bump Sentry .NET SDK 3.8.0 ([#255](https://github.com/getsentry/sentry-unity/pull/255))
  - [changelog](https://github.com/getsentry/sentry-dotnet/blob/main/CHANGELOG.md#380)
  - [diff](https://github.com/getsentry/sentry-dotnet/compare/3.5.0...3.8.0)

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
