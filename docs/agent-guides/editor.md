# Editor Integration

## Source Areas

- General Editor code: `src/Sentry.Unity.Editor/`.
- iOS export code: `src/Sentry.Unity.Editor.iOS/`.
- Generated package editor setup: `package-dev/Editor/SentryEditorPlatformSetUp.cs`.

## Features

- `Tools/Sentry` creates/loads Scriptable and CLI options. Tabs: Core, Logging, Enrichment, Transport, Advanced, Options Config, and Debug Symbols.
- Editor platform setup only establishes `SentryPlatformServices.UnityInfo` on domain reload/build. It does not configure player native SDKs.
- IL2CPP preprocessing adds `--emit-source-mapping`; line-number support also needs debug-symbol upload configuration.
- Performance auto-instrumentation is opt-in post-build IL rewriting. It only instruments types whose direct base type is `MonoBehaviour`, around `Awake` returns.
- Generic native post-build uploads symbols before copying artifacts; copy failures raise `BuildFailedException`.
- WebGL preprocessing fails with exception support `None` and warns unless `FullWithStacktrace`.
- Android export processing implements `IPostGenerateGradleAndroidProject`; callback order is configurable by `PostGenerateGradleProjectCallbackOrder`.

## Tests

- General: `test/Sentry.Unity.Editor.Tests/`
- Android export: `test/Sentry.Unity.Editor.Tests/Android/`
- Native post-build: `test/Sentry.Unity.Editor.Tests/Native/`
- iOS export: `test/Sentry.Unity.Editor.iOS.Tests/`
