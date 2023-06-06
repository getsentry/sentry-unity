# Changelog

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
