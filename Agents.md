# Agents.md

This file provides comprehensive guidance for AI agents and developers working with the Sentry Unity SDK repository.

---

## 1. Overview & Quick Reference

### Repository Purpose
The Sentry Unity SDK provides error monitoring, performance tracing, and crash reporting for Unity applications across all platforms (Android, iOS, macOS, Windows, Linux, WebGL, PlayStation, Xbox).

### Quick Commands

**IMPORTANT**: Always run `dotnet build` from the repository root. Never build specific `.csproj` files directly.

```bash
# Download prebuilt native SDKs
dotnet msbuild /t:DownloadNativeSDKs src/Sentry.Unity

# Build the Unity SDK (always from root, never target specific .csproj files)
dotnet build

# Run all tests (builds SDK first)
pwsh scripts/run-tests.ps1

# Run specific test types
pwsh scripts/run-tests.ps1 -PlayMode
pwsh scripts/run-tests.ps1 -EditMode

# Run filtered tests
pwsh scripts/run-tests.ps1 -Filter "TestClassName"
pwsh scripts/run-tests.ps1 -PlayMode -Filter "Throttler"

# Skip build for faster iteration
pwsh scripts/run-tests.ps1 -SkipBuild -Filter "MyTest"

# Integration testing (local)
./test/Scripts.Integration.Test/integration-test.ps1 -Platform "macOS" -UnityVersion "2021.3.45f2"

# Create release package
dotnet msbuild /t:DownloadNativeSDKs src/Sentry.Unity
dotnet build
pwsh scripts/repack.ps1
```

### Key Directories

| Directory | Purpose |
|-----------|---------|
| `src/` | Source code for all platform implementations |
| `package-dev/` | Development Unity package with all assemblies |
| `package/` | Release package template for UPM |
| `test/` | Test suite (unit, integration, platform tests) |
| `modules/` | Git submodules for native SDKs |
| `samples/` | Sample Unity projects |
| `scripts/` | Build automation and testing scripts |
| `.github/workflows/` | CI/CD workflow definitions |

### Git Commit Guidelines
- Use simple, direct commit messages without prefixes like "chore:" or "feat:"
- Messages start with a capital letter

---

## 2. Project Architecture

### Core Components

```
src/
├── Sentry.Unity/           # Main SDK - Unity-specific functionality
├── Sentry.Unity.Editor/    # Editor integration, config windows, build hooks
├── Sentry.Unity.Android/   # Android JNI bridge to sentry-java
├── Sentry.Unity.iOS/       # iOS/macOS Objective-C bridge to sentry-cocoa
└── Sentry.Unity.Native/    # Windows/Linux P/Invoke to sentry-native
```

### Platform Integration Pattern

Each platform follows a consistent architecture:

1. **Native Bridge** - Platform-specific interface to native SDK
   - Android: JNI via `AndroidJavaClass`/`AndroidJavaObject`
   - iOS/macOS: Objective-C via `DllImport("__Internal")`
   - Windows/Linux: P/Invoke via `DllImport("sentry")`

2. **Context Writer** - Synchronizes Unity context (device, GPU, app info) to native SDK

3. **Scope Observer** - Keeps scope (breadcrumbs, tags, user) synchronized between C# and native layers

4. **Configuration** - Platform-specific options and initialization logic

### Assembly Structure
- Runtime assemblies separate from Editor assemblies
- Platform-specific assemblies compile only for target platforms
- Clear dependency hierarchy prevents circular references
- Assembly aliasing prevents symbol conflicts with user dependencies

---

## 3. CI/CD System

### Workflow Architecture

The CI system uses modular, reusable workflows in `.github/workflows/`:

| Workflow | Purpose |
|----------|---------|
| `ci.yml` | Main pipeline - triggers on push/PR |
| `build.yml` | Reusable build workflow |
| `sdk.yml` | Native SDK builds (Android, Linux, Windows, Cocoa) |
| `smoke-test-create.yml` | Creates integration test projects |
| `smoke-test-build-android.yml` | Builds Android test apps |
| `smoke-test-run-android.yml` | Runs Android tests on emulator |
| `smoke-test-build-ios.yml` | Builds iOS test apps |
| `smoke-test-compile-ios.yml` | Compiles iOS Xcode projects |
| `smoke-test-run-ios.yml` | Runs iOS tests on simulator |
| `release.yml` | Manual release preparation |
| `update-deps.yml` | Scheduled dependency updates (daily) |
| `create-unity-matrix.yml` | Generates test matrix |

### Unity Version Matrix

| Version | PR Testing | Main Branch |
|---------|------------|-------------|
| 2021.3.x | No | Yes |
| 2022.3.x | Yes | Yes |
| 6000.0.x | Yes | Yes |
| 6000.1.x | No | Yes |

Version mapping is defined in `scripts/ci-env.ps1`:
- `2021.3` → `2021.3.45f2`
- `2022.3` → `2022.3.70f1`
- `6000.0` → `6000.0.48f1`
- `6000.1` → `6000.1.17f1`

### Docker-Based Builds

Builds run in Docker containers using `ghcr.io/unityci/editor` images:
- Ensures consistent environment across CI runs
- Container setup in `scripts/ci-docker.sh`
- Includes Unity editor, Android SDK, Java, and .NET

### MSBuild Targets

Key targets defined in `Directory.Build.targets`:

| Target | Purpose |
|--------|---------|
| `DownloadNativeSDKs` | Downloads prebuilt native SDKs from CI |
| `BuildAndroidSDK` | Builds Android SDK via Gradle |
| `BuildLinuxSDK` | Builds Linux SDK via CMake |
| `BuildWindowsSDK` | Builds Windows SDK via CMake (Crashpad) |
| `BuildCocoaSDK` | Downloads iOS/macOS SDKs from releases |
| `UnityEditModeTest` | Runs edit-mode unit tests |
| `UnityPlayModeTest` | Runs play-mode tests |
| `UnitySmokeTestStandalonePlayerIL2CPP` | Runs smoke tests |

### Artifact Caching

- **Native SDK Cache**: Keyed by submodule SHA and package version
- **Unity Library Cache**: Keyed by platform, Unity version, and test scripts
- **Build Artifacts**: 14-day retention for failed CI runs

### CI Flow

**On Pull Request:**
1. Create Unity version matrix (2022.3, 6000.0 only)
2. Build SDK in Docker
3. Validate UPM package contents
4. Create integration test projects
5. Build for WebGL, Linux, Android, iOS, Windows
6. Run smoke tests and crash tests
7. Measure build sizes

**On Main Branch:**
- Same as PR but with all Unity versions
- Build native SDKs in parallel
- Extended test coverage

---

## 4. Building & Packaging

### Build System

Central configuration in `Directory.Build.targets` (900+ lines) and `Directory.Build.props`:

```xml
<!-- Key properties -->
<Version>4.0.0</Version>
<DotNetSdkVersion>10.0.100</DotNetSdkVersion>
<LangVersion>12</LangVersion>
<TargetFramework>netstandard2.0</TargetFramework> <!-- or netstandard2.1 for Unity 2021+ -->
```

### Native SDK Download

```bash
dotnet msbuild /t:DownloadNativeSDKs src/Sentry.Unity
```

Downloads prebuilt native SDKs from CI artifacts or releases:
- Android: JAR/AAR files to `package-dev/Plugins/Android/Sentry~/`
- iOS: XCFramework to `package-dev/Plugins/iOS/`
- macOS: DYLIB to `package-dev/Plugins/macOS/`
- Windows: DLL + Crashpad to `package-dev/Plugins/Windows/Sentry/`
- Linux: SO to `package-dev/Plugins/Linux/Sentry/`

### Assembly Aliasing

Prevents symbol conflicts with user dependencies using `assemblyalias` tool:

```bash
pwsh scripts/build-and-alias.ps1
```

- Runtime assemblies: `Microsoft*`, `System*` → prefixed with `Sentry.`
- Editor assemblies: `Microsoft*`, `Mono.Cecil*` → prefixed with `Sentry.`

### Package Structure

| Directory | Purpose |
|-----------|---------|
| `package-dev/` | Development package with source, used for testing |
| `package/` | Release template with metadata (package.json, LICENSE) |
| `package-release/` | Final release package (created dynamically) |
| `package-release.zip` | Distributed UPM package |

### Release Workflow

```bash
# Full release preparation
dotnet msbuild /t:DownloadNativeSDKs src/Sentry.Unity
dotnet build
pwsh scripts/repack.ps1  # Assembly aliasing + packaging + snapshot update
```

Scripts involved:
- `scripts/pack.ps1` - Creates the release package
- `scripts/repack.ps1` - Full preparation pipeline
- `scripts/build-and-alias.ps1` - Build with assembly aliasing

### Package Validation

`test/Scripts.Tests/test-pack-contents.ps1` validates package contents against a snapshot to detect unintended changes.

---

## 5. Native Platform Support

### Platform Implementation Matrix

| Platform | Bridge Type | DllImport | Key Source Files |
|----------|-------------|-----------|------------------|
| Android | JNI | N/A | `SentryJava.cs`, `SentryNativeAndroid.cs` |
| iOS | Objective-C | `__Internal` | `SentryCocoaBridgeProxy.cs`, `SentryNativeCocoa.cs` |
| macOS | Objective-C | `__Internal` | `SentryCocoaBridgeProxy.cs`, `SentryNativeCocoa.cs` |
| Windows | P/Invoke | `sentry` | `SentryNativeBridge.cs`, `CFunctions.cs` |
| Linux | P/Invoke | `sentry` | `SentryNativeBridge.cs`, `CFunctions.cs` |
| PlayStation | P/Invoke | `sentry` | `SentryNativeBridge.cs`, `CFunctions.cs` |

### Native SDK Submodules

```
modules/
├── sentry-java/    # Android SDK (Gradle build)
├── sentry-native/  # Windows/Linux/macOS (CMake build)
└── sentry-cocoa/   # iOS/macOS (prebuilt XCFramework)
```

### Key Source Files

**Android (`src/Sentry.Unity.Android/`):**
- `SentryJava.cs` - JNI wrapper using `AndroidJavaClass`/`AndroidJavaObject`
- `SentryNativeAndroid.cs` - Configuration and initialization
- `AndroidJavaScopeObserver.cs` - Scope synchronization
- `NativeContextWriter.cs` - Context synchronization

**iOS/macOS (`src/Sentry.Unity.iOS/`):**
- `SentryCocoaBridgeProxy.cs` - P/Invoke to Objective-C functions
- `SentryNativeCocoa.cs` - Configuration logic
- `NativeScopeObserver.cs` - Scope synchronization
- `SentryNativeBridge.m` - Objective-C bridge implementation

**Windows/Linux (`src/Sentry.Unity.Native/`):**
- `SentryNativeBridge.cs` - P/Invoke bindings to `sentry` C library
- `CFunctions.cs` - Low-level C API definitions
- `SentryNative.cs` - Configuration and crash detection
- `NativeScopeObserver.cs` - Scope synchronization
- `NativeDebugImageProvider.cs` - Debug image enumeration

### Compile-Time Platform Detection

Defined in `package-dev/Runtime/SentryInitialization.cs`:

```csharp
#if SENTRY_NATIVE_COCOA    // iOS, macOS (IL2CPP)
#if SENTRY_NATIVE_ANDROID  // Android (IL2CPP)
#if SENTRY_NATIVE          // Windows/Linux 64-bit, GameCore, PS5
#if SENTRY_WEBGL           // WebGL
```

### Initialization Flow

1. `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]` triggers `SentryInitialization.Init()`
2. `SetUpPlatformServices()` registers platform-specific configuration callback
3. When .NET SDK initializes, it invokes the platform configuration
4. Native SDK initializes and scope sync begins

### Plugin Directory Structure

```
package-dev/Plugins/
├── Android/
│   ├── proguard-sentry-unity.pro
│   └── Sentry~/
│       ├── sentry.jar
│       ├── sentry-android-core-release.aar
│       ├── sentry-android-ndk-release.aar
│       └── sentry-native-ndk-release.aar
├── iOS/
│   ├── Sentry.xcframework~/
│   ├── SentryNativeBridge.m
│   └── SentryCxaThrowHook.cpp
├── macOS/
│   ├── Sentry.dylib
│   └── SentryNativeBridge.m
├── Windows/Sentry/
│   ├── sentry.dll
│   ├── sentry.pdb
│   ├── crashpad_handler.exe
│   └── crashpad_wer.dll
├── Linux/Sentry/
│   ├── libsentry.so
│   └── libsentry.dbg.so
└── PS5/
    └── sentry_utils.c
```

### Scope Synchronization Pattern

Base class: `src/Sentry.Unity/ScopeObserver.cs`

All platforms implement:
- `AddBreadcrumbImpl()` - Add breadcrumbs to native layer
- `SetTagImpl()` / `UnsetTagImpl()` - Manage tags
- `SetUserImpl()` / `UnsetUserImpl()` - Manage user info
- `SetExtraImpl()` - Add extra context
- `SetTraceImpl()` - Set trace context

### Context Writing Pattern

Base class: `src/Sentry.Unity/ContextWriter.cs`

Synchronizes during SDK initialization:
- App start time, build type
- OS information
- Device info (CPU, memory, simulator status)
- GPU capabilities
- Unity-specific context

---

## 6. Unity Integrations

### Runtime Lifecycle

**`src/Sentry.Unity/SentryMonoBehaviour.cs`**

Central lifecycle manager:
- Singleton pattern with `DontDestroyOnLoad`
- Handles `OnApplicationPause()`, `OnApplicationFocus()`, `OnApplicationQuit()`
- Coroutine queue for background thread operations
- Provides `StartAwakeSpan()` / `FinishAwakeSpan()` for performance instrumentation

### SDK Initialization

**`src/Sentry.Unity/SentryUnitySdk.cs`**

Orchestrates initialization:
- Configures options from `ScriptableSentryUnityOptions`
- Registers integrations
- Sets up platform-specific callbacks
- Creates `SentryMonoBehaviour` instance

### Integrations

Located in `src/Sentry.Unity/Integrations/`:

| Integration | File | Purpose |
|-------------|------|---------|
| Scene Manager | `SceneManagerIntegration.cs` | Breadcrumbs for scene load/unload/change |
| Scene Tracing | `SceneManagerTracingIntegration.cs` | Spans for scene loading |
| Startup Tracing | `StartupTracingIntegration.cs` | Transaction for app startup |
| Lifecycle | `LifeCycleIntegration.cs` | Session tracking across pause/resume |
| Log Handler | `UnityLogHandlerIntegration.cs` | Captures `Debug.LogException()` |
| App Logging | `UnityApplicationLoggingIntegration.cs` | Hooks `Application.LogMessageReceived` |
| ANR | `AnrIntegration.cs` | Application Not Responding detection |
| Low Memory | `LowMemoryIntegration.cs` | Memory warning events |
| Scope | `UnityScopeIntegration.cs` | Populates scope with Unity context |

### Startup Tracing Detail

`StartupTracingIntegration.cs` creates spans:
- `app.start` - Main transaction
- `runtime.init` - Runtime initialization
- `runtime.init.subsystem` - Subsystem registration
- `runtime.init.afterassemblies` - After assemblies load
- `runtime.init.splashscreen` - Splash screen display
- `runtime.init.firstscene` - First scene load

### Event Processors

| Processor | File | Purpose |
|-----------|------|---------|
| Unity | `UnityEventProcessor.cs` | App memory, battery, device context |
| Screenshot | `ScreenshotEventProcessor.cs` | Captures screen as JPEG attachment |
| View Hierarchy | `ViewHierarchyEventProcessor.cs` | GameObject hierarchy JSON |
| IL2CPP | `Il2CppEventProcessor.cs` | Line number support for IL2CPP |

### Screenshot Capture

**`src/Sentry.Unity/ScreenshotEventProcessor.cs`**

- Configurable: `AttachScreenshot`, `ScreenshotQuality`, `ScreenshotCompression`
- Hooks: `BeforeCaptureScreenshot()`, `BeforeSendScreenshot()`
- Rate-limited: 1 screenshot per frame
- Must run on main thread

### View Hierarchy Capture

**`src/Sentry.Unity/ViewHierarchyEventProcessor.cs`**

- Configuration: `MaxViewHierarchyRootObjects`, `MaxViewHierarchyObjectChildCount`, `MaxViewHierarchyDepth`
- Hooks: `BeforeCaptureViewHierarchy()`, `BeforeSendViewHierarchy()`
- Output: `view-hierarchy.json` attachment

### Editor Integration

**`src/Sentry.Unity.Editor/ConfigurationWindow/`**

Accessible via **Tools → Sentry** menu:
- `SentryWindow.cs` - Main editor window
- `CoreTab.cs` - DSN and basic setup
- `LoggingTab.cs` - Log capture configuration
- `EnrichmentTab.cs` - Screenshot/hierarchy settings
- `TransportTab.cs` - HTTP transport configuration
- `DebugSymbolsTab.cs` - Symbol upload settings
- `AdvancedTab.cs` - Advanced options
- `Wizard.cs` - Initial setup wizard

### Build Post-Processing

**`src/Sentry.Unity.Editor/Native/BuildPostProcess.cs`**

Runs after build (Priority 1):
- Debug symbol upload via `sentry-cli`
- Crash handler installation (Windows: Crashpad)
- Platform-specific configuration
- IL2CPP method mapping upload

### Configuration Options

**`src/Sentry.Unity/SentryUnityOptions.cs`** (560+ lines)

Key options:
- `Enabled` - Enable/disable SDK
- `CaptureInEditor` - Capture events in editor
- `AutoStartupTraces` - Automatic startup tracing
- `AutoSceneLoadTraces` - Scene load performance tracing
- `AttachScreenshot` - Screenshot on error
- `AttachViewHierarchy` - View hierarchy on error
- `AnrTimeout` - ANR detection timeout
- Platform-specific: `*NativeSupportEnabled`, `*NativeInitializationType`

---

## 7. Testing Infrastructure

### Test Types

| Type | Command | Location |
|------|---------|----------|
| Edit Mode | `dotnet msbuild /t:UnityEditModeTest` | `test/Sentry.Unity.Tests/` |
| Play Mode | `dotnet msbuild /t:UnityPlayModeTest` | `test/Sentry.Unity.Tests/` |
| Editor Tests | `dotnet msbuild /t:UnityEditModeTest` | `test/Sentry.Unity.Editor.Tests/` |
| Smoke Tests | `dotnet msbuild /t:UnitySmokeTestStandalonePlayerIL2CPP` | Integration tests |
| Integration | `integration-test.ps1` | `test/Scripts.Integration.Test/` |

### Running All Tests

```bash
# Run all tests (builds SDK first)
pwsh scripts/run-tests.ps1

# Run with filtering
pwsh scripts/run-tests.ps1 -Filter "TestClassName"

# Skip build for faster iteration
pwsh scripts/run-tests.ps1 -SkipBuild
```

### Integration Test Scripts

Located in `test/Scripts.Integration.Test/`:

| Script | Purpose |
|--------|---------|
| `create-project.ps1` | Creates new Unity test project |
| `add-sentry.ps1` | Adds Sentry package to project |
| `configure-sentry.ps1` | Configures Sentry in test project |
| `build-project.ps1` | Builds for target platform |
| `run-smoke-test.ps1` | Executes smoke and crash tests |
| `measure-build-size.ps1` | Compares build size with/without SDK |
| `integration-test.ps1` | Full local integration test |

### Local Integration Testing

```powershell
./test/Scripts.Integration.Test/integration-test.ps1 -Platform "macOS" -UnityVersion "6000.1.8f1"
```

Supported platforms: `macOS`, `Windows`, `Linux`, `Android`, `iOS`, `WebGL`

### Sample Projects

| Project | Path | Purpose |
|---------|------|---------|
| Unity of Bugs | `samples/unity-of-bugs/` | Sample app for testing |
| Integration Test | `samples/IntegrationTest/` | CI integration testing |

---

## 8. Development Guidelines

### Development Workflow

**Prerequisites (first-time setup or after clean):**
```bash
# Download native SDKs - REQUIRED before building
dotnet msbuild /t:DownloadNativeSDKs src/Sentry.Unity
```

**Development cycle:**
1. Make changes to source code in `src/`
2. Run `dotnet build` to build and update `package-dev/`
3. Run `pwsh scripts/run-tests.ps1` to build and run all tests
4. Test changes using the sample project or integration tests
5. Run `pwsh scripts/repack.ps1` before creating releases

> **Note:** The native SDKs in `package-dev/Plugins/` are not committed to the repository. You must run `DownloadNativeSDKs` before the first build or after cleaning the repository.

### Error Handling Patterns

- Platform-specific implementations handle native SDK failures gracefully
- The SDK should not throw unhandled exceptions (except during initialization)
- Unity-specific exception filters prevent SDK errors from affecting the game
- Extensive logging helps debug integration issues
- Fallback mechanisms ensure core functionality works even if platform-specific features fail

### Exception Filters

Located in `src/Sentry.Unity/Integrations/`:
- `UnityBadGatewayExceptionFilter.cs` - Filters HTTP 502 errors
- `UnityWebExceptionFilter.cs` - Filters web-related exceptions
- `UnitySocketExceptionFilter.cs` - Filters socket exceptions

### Platform-Specific Considerations

- **Android**: JNI calls must be on correct thread; use `AndroidJavaObject` lifecycle properly
- **iOS**: `DllImport("__Internal")` for statically linked functions
- **Windows**: Crashpad handler must be deployed alongside the build
- **WebGL**: Limited functionality; no startup tracing, no caching

### Debug Symbol Upload

Configured through Editor window (Debug Symbols tab):
- Uses Sentry CLI (`scripts/download-sentry-cli.ps1`)
- IL2CPP method mapping for accurate stack traces
- Optional source inclusion

### Main Thread Safety

- `MainThreadData` class caches main thread info
- Screenshot/hierarchy capture verified to be on main thread
- Background operations use `QueueCoroutine()` for main thread execution
- Use `Task.Run()` for background thread operations

### Native Dependencies

- Native libraries included as plugins in `package-dev/Plugins/`
- Android: JAR and AAR files with JNI bridge
- iOS: XCFramework with Objective-C bridge
- macOS: DYLIB with Objective-C bridge
- Windows/Linux: DLLs/SOs with P/Invoke

---

## 9. Notes & Discoveries

<!--
This section captures learnings discovered during development sessions.
Format: - [YYYY-MM-DD] Category: Note
-->
