# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Common Development Commands

### Downloading prebuilt native SDKs
```bash
# Download prebuilt native SDKs
dotnet msbuild /t:DownloadNativeSDKs src/Sentry.Unity
```

### Building the Project
```bash
# Build the Unity SDK
dotnet build
```

### Testing
```bash
# Run all Unity tests (Edit Mode, Play Mode, and Smoke Tests)
./test.sh

# Or run specific test targets:
dotnet msbuild /t:UnityEditModeTest /p:Configuration=Release
dotnet msbuild /t:UnityPlayModeTest /p:Configuration=Release
dotnet msbuild /t:UnitySmokeTestStandalonePlayerIL2CPP /p:Configuration=Release
```

### Integration Testing

The `integration-test.ps1` uses the same scripts as CI to allow for local debugging of the integration tests.
```powershell
./test/Scripts.Integration.Test/integration-test.ps1 -Platform "macOS" -UnityVersion "6000.1.8f1"
```

### Package Management
To create a new package version do the following in order
```bash
dotnet msbuild /t:DownloadNativeSDKs src/Sentry.Unity # Download prebuilt native SDKs from CI
dotnet build # Build the Unity SDK
pwsh scripts/repack.ps1 # Create the release package by running Assembly Alias and packaging the SDK. This also updates the snapshot for package content validation
```

## Project Architecture

### Core Components
- **Sentry.Unity**: Main Unity SDK implementation with Unity-specific functionality
- **Sentry.Unity.Editor**: Unity Editor integration, configuration windows, and build hooks
- **Sentry.Unity.Android**: Android-specific implementation using JNI bridge to Android SDK
- **Sentry.Unity.iOS**: iOS/macOS implementation using Objective-C bridge to Cocoa SDK
- **Sentry.Unity.Native**: Native platform support for Windows/Linux/macOS standalone builds
- **sentry-dotnet**: Underlying .NET SDK (included as Git submodule)

### Platform Integration Pattern
Each platform follows a similar pattern:
1. **Native Bridge**: Platform-specific bridge to native SDK (JNI for Android, Objective-C for iOS, P/Invoke for Native)
2. **Context Writer**: Synchronizes Unity context to native SDK
3. **Scope Observer**: Keeps scope synchronized between C# and native layers
4. **Configuration**: Platform-specific options and initialization

### Key Directories
- `src/`: Source code for all platform implementations
- `package-dev/`: Development version of the Unity package with all assemblies
- `package/`: Release package structure for Unity Package Manager
- `test/`: Comprehensive test suite including unit, integration, and platform tests
- `modules/`: Git submodules for native SDKs (sentry-java, sentry-native, sentry-cocoa)
- `samples/`: Sample Unity projects for testing and documentation
- `scripts/`: Build automation and testing scripts

### Assembly Structure
- Runtime assemblies are separate from Editor assemblies
- Platform-specific assemblies only compile for their target platforms
- Clear dependency hierarchy prevents circular references
- Assembly aliasing prevents symbol conflicts with user dependencies

### Integration Points
- **Unity Lifecycle**: SentryMonoBehaviour handles Unity-specific lifecycle events
- **Scene Management**: Automatic breadcrumb generation for scene changes and performance tracing
- **Exception Handling**: Unity-specific exception processors and filters
- **Build Pipeline**: Editor scripts handle build-time configuration and symbol upload
- **Native Integration**: Platform-specific bridges synchronize data with native SDKs

### Testing Strategy
- **Unit Tests**: Test individual components in isolation
- **Integration Tests**: Test cross-component interactions and Unity integration
- **Platform Tests**: Test platform-specific functionality
- **Smoke Tests**: End-to-end testing of core functionality across platforms
- **Shared Test Infrastructure**: Common utilities and mocks for consistent testing

## Development Notes

### Building for Different Platforms
The SDK automatically detects the target platform and includes appropriate native libraries. No special configuration is needed - the Unity Editor handles platform-specific compilation.

### Working with Native Dependencies
- Native libraries are included as plugins in `package-dev/Plugins/`
- Android uses JAR and AAR files with JNI bridge
- iOS uses XCFramework with Objective-C bridge  
- macOS uses DYLIB with Objective-C bridge  
- Windows/Linux/macOS standalone use native DLLs/SOs with P/Invoke

### Symbol Upload Integration
The SDK includes Sentry CLI integration for automatic debug symbol upload during builds. This is configured through the Sentry Editor window and build post-processors.

### Package Development Workflow
1. Make changes to source code in `src/`
2. Run `dotnet build` to build and update `package-dev/`
3. Test changes using the sample project or integration tests

### Error Handling Patterns
- Platform-specific implementations handle native SDK failures gracefully
- The SDK should not throw unhandled exception. The only exception to this rule is during initialization
- Unity-specific exception filters prevent SDK errors from affecting the game
- Extensive logging helps debug integration issues
- Fallback mechanisms ensure core functionality works even if platform specific features fail

## Git Commit Guidelines

### Commit Messages
- Use simple, direct commit messages without prefixes like "chore:" or "feat:"
- Messages start with a capital letter