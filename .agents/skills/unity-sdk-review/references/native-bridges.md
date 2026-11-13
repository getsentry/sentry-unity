# Native Bridges

Read this when a hunk adds, removes, or changes a P/Invoke, a JNI call, or a native bridge source.

## Where Each Entry Point Must Exist

| C# declaration | Library name | Native sides that must match |
|----------------|--------------|------------------------------|
| `src/Sentry.Unity.iOS/SentryCocoaBridgeProxy.cs` (`EntryPoint = "SentryNativeBridge*"`) | `__Internal` | `package-dev/Plugins/iOS/SentryNativeBridge.m`, `SentryNativeBridgeNoOp.m`, `SentryNativeBridgeExternal.m`, and `package-dev/Plugins/macOS/SentryNativeBridge.m` |
| `src/Sentry.Unity.iOS/NativeContextWriter.cs` (`SentryNativeBridgeWriteScope`) | `__Internal` | Same four bridge files |
| `src/Sentry.Unity.Native/CFunctions.cs`, `SentryNativeBridge.cs` (`sentry_*`) | `SentryNativeLibrary.Name`: `sentry-native` on desktop, `sentry` on Android, PS5, and Xbox, `__Internal` on Switch | sentry-native exports (not in the checkout), and `package-dev/Plugins/Switch/SentryStub~/sentry_native_stubs.c` for every symbol the Switch build binds |
| `src/Sentry.Unity.Native/SentryNativeSwitch.cs` | `__Internal` | `sentry_native_stubs.c` |
| `src/Sentry.Unity.Android/SentryNative.cs`, `SentryJava.cs` | JNI / `sentry` | sentry-java and the NDK (not in the checkout) |

Existing guards are incomplete:

- `test/Sentry.Unity.Editor.iOS.Tests/SentryNativeBridgeTests.cs` checks only that each `SentryCocoaBridgeProxy` entry point **name** appears in the three iOS bridge files. It does not check the macOS bridge, `NativeContextWriter`, argument count, or argument types.
- `SwitchNativeStubTests.Stub_ContainsEverySwitchNativeBinding` checks that the Switch stub covers the Switch `__Internal` bindings.

So report:

- An entry point that is added or renamed in C# but missing from any bridge file in the table. On iOS this is a link error. On macOS it is `EntryPointNotFoundException` at init.
- A parameter that is added, removed, or reordered on one side only. The NoOp and External bridges are easy to miss.
- A P/Invoke that becomes reachable on Switch without a matching stub function in `sentry_native_stubs.c`.
- A bridge function that calls an Objective-C class that another bridge file has already migrated away from (for example `SentrySDK` vs `SentryObjCSDK`), when the repo shows the migration.
- In `SentryNativeBridgeExternal.m`: a selector called without a `respondsToSelector:` check. Unity builds without Objective-C exceptions, so an unknown selector crashes the host app.

## Marshalling Under IL2CPP

- `string[]` or other arrays passed to `const char **` need an explicit count parameter, and the native side must not read past it. Without `[MarshalAs(UnmanagedType.LPArray, SizeParamIndex = n)]` the layout depends on the default marshaller. Report a mismatch only when the native signature in the repo disagrees with the C# side.
- `bool` crossing to C defaults to a 4-byte `BOOL`. Objective-C `BOOL` is 1 byte (`signed char`). The repo uses `sbyte` or `int` with explicit conversion. A raw `bool` paired with a 1-byte native type is a bug.
- Delegates passed to native code must be `static` methods with `[MonoPInvokeCallback(typeof(...))]` and kept alive. Instance-method or lambda callbacks fail under IL2CPP.
- `va_list` handling in `SentryNativeBridge.cs`: on x86_64 System V and Linux AArch64, `va_list` is a struct passed by reference, and `UseLibC` selects `vsnprintf` from libc. A platform added to the native logger path needs the same treatment as its ABI siblings.
- `string` returned from native code is freed by the marshaller. Returning a pointer that the native side still owns (or that was not allocated with `malloc` or `strdup`) corrupts the heap.

## Threading

- `SentryJava` methods that touch JNI must run on the Unity main thread, or attach the thread with `AndroidJNI.AttachCurrentThread` and detach it afterwards. Existing methods check `MainThreadData.IsMainThread()` first. A new JNI call without either is a bug.
- Scope observers (`NativeScopeObserver`, `AndroidJavaScopeObserver`) can be called from any thread.

## Do Not Report

- Missing stubs for PS5 or Xbox. Their libraries are supplied by the user.
