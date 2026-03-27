# Cross-Layer Attachment Sync Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** When a user adds a file-path attachment in Unity C#, it should be present on events from C# (.NET SDK), Java (sentry-java), and native crashes (sentry-native).

**Architecture:** Extends the existing scope sync chain: C# Scope → Unity ScopeObserver → sentry-java Scope (via JNI) → NdkScopeObserver → sentry-native (via JNI). Each layer's scope observer is extended with attachment methods. File-path attachments propagate the path string down through all layers. Non-file attachments (byte/stream) remain C#-only for now.

**Tech Stack:** C# (.NET/Unity), Java (sentry-java), C (sentry-native JNI), P/Invoke

---

## Scope and Constraints

- **File-path attachments only** for cross-layer sync. The native crash handler reads files from disk at crash time — in-memory byte/stream attachments cannot survive a native crash. Byte/stream attachments continue to work for C# events as they do today.
- **Android is the primary target** (three-layer chain: C# → Java → Native). Windows/Linux (two-layer: C# → Native via P/Invoke) is a secondary target using the same ScopeObserver extension.
- **iOS/macOS** uses sentry-cocoa which has a separate architecture. Out of scope for this plan.

## Data Flow

```
User calls scope.AddAttachment("/path/to/file.json") in Unity C#

┌─ .NET SDK Scope ──────────────────────────────────────────────────┐
│ _attachments.Add(attachment)                                       │
│ ScopeObserver?.AddAttachment(attachment)  ←── NEW                  │
└────────────────────────┬───────────────────────────────────────────┘
                         │
        ┌────────────────┴─────────────────────┐
        │ (Android)                             │ (Windows/Linux)
        ▼                                       ▼
┌─ AndroidJavaScopeObserver ─┐      ┌─ NativeScopeObserver ────────┐
│ JNI → configureScope →     │      │ P/Invoke →                   │
│   scope.addAttachment(     │      │   sentry_attach_file(path)   │
│     new Attachment(path))  │      └──────────────────────────────┘
└────────────┬───────────────┘
             │
             ▼
┌─ sentry-java Scope ────────────────────────────────────────────────┐
│ attachments.add(attachment)                                         │
│ for (observer : getScopeObservers())                                │
│     observer.addAttachment(attachment)  ←── NEW                     │
└────────────┬────────────────────────────────────────────────────────┘
             │
             ▼
┌─ NdkScopeObserver ─────────────────────────────────────────────────┐
│ nativeScope.addAttachment(pathname)  ←── NEW                        │
└────────────┬────────────────────────────────────────────────────────┘
             │
             ▼
┌─ NativeScope (JNI) ────────────────────────────────────────────────┐
│ native void nativeAddAttachment(String path)  ←── NEW               │
└────────────┬────────────────────────────────────────────────────────┘
             │
             ▼
┌─ sentry.c (JNI) ──────────────────────────────────────────────────┐
│ sentry_attach_file(path)  ←── existing sentry-native API           │
└────────────────────────────────────────────────────────────────────┘
```

## File Map

### sentry-java (modules/sentry-java/)

| Action | File | What changes |
|--------|------|-------------|
| Modify | `sentry/src/main/java/io/sentry/IScopeObserver.java` | Add `default` methods: `addAttachment()`, `clearAttachments()` |
| Modify | `sentry/src/main/java/io/sentry/ScopeObserverAdapter.java` | Override with empty implementations |
| Modify | `sentry/src/main/java/io/sentry/Scope.java` | Notify observers in `addAttachment()` and `clearAttachments()` |
| Modify | `sentry-android-ndk/src/main/java/io/sentry/android/ndk/NdkScopeObserver.java` | Implement `addAttachment()` and `clearAttachments()` |
| Modify | `sentry-android-ndk/src/test/java/io/sentry/android/ndk/NdkScopeObserverTest.kt` | Add tests for attachment sync |

### sentry-native NDK JNI (modules/sentry-native/ndk/)

| Action | File | What changes |
|--------|------|-------------|
| Modify | `lib/src/main/java/io/sentry/ndk/INativeScope.java` | Add `addAttachment(path)`, `clearAttachments()` |
| Modify | `lib/src/main/java/io/sentry/ndk/NativeScope.java` | Add JNI native declarations and interface implementations |
| Modify | `lib/src/main/jni/sentry.c` | Add JNI functions calling `sentry_attach_file()` and `sentry_clear_attachments()` |

### sentry-dotnet (src/sentry-dotnet/)

| Action | File | What changes |
|--------|------|-------------|
| Modify | `src/Sentry/IScopeObserver.cs` | Add `AddAttachment(SentryAttachment)` |
| Modify | `src/Sentry/Scope.cs` | Call observer in `AddAttachment()`, guard against `Apply()` re-notification |
| Modify | `src/Sentry/FileAttachmentContent.cs` | Expose `FilePath` as public property |
| Modify | `src/Sentry/Internal/ScopeObserver.cs` | Add no-op `AddAttachment()` to internal base class |
| Modify | `src/Sentry/Platforms/Cocoa/CocoaScopeObserver.cs` | Add no-op `AddAttachment()` |
| Modify | `src/Sentry/Platforms/Android/AndroidScopeObserver.cs` | Add no-op `AddAttachment()` |
| No change | `src/Sentry/Platforms/Native/NativeScopeObserver.cs` | Inherits virtual no-op from internal `ScopeObserver` base |

### Unity SDK (src/)

| Action | File | What changes |
|--------|------|-------------|
| Modify | `Sentry.Unity/ScopeObserver.cs` | Add `AddAttachment()` template + abstract `AddFileAttachmentImpl()` |
| Modify | `Sentry.Unity.Android/AndroidJavaScopeObserver.cs` | Implement `AddFileAttachmentImpl()` |
| Modify | `Sentry.Unity.Android/SentryJava.cs` | Add `AddAttachment(path)` JNI method + `ISentryJava` interface |
| Modify | `Sentry.Unity.Native/NativeScopeObserver.cs` | Implement `AddFileAttachmentImpl()` via P/Invoke |
| Modify | `Sentry.Unity.Native/CFunctions.cs` | Add P/Invoke declarations for `sentry_attach_file` |
| Modify | `Sentry.Unity.iOS/NativeScopeObserver.cs` | Add no-op `AddFileAttachmentImpl()` (iOS out of scope) |

---

## Chunk 1: sentry-native NDK JNI Bridge

This is the bottom of the stack — the JNI functions that call sentry-native's C API. No dependencies on other changes.

### Task 1: Extend INativeScope interface

**Files:**
- Modify: `modules/sentry-native/ndk/lib/src/main/java/io/sentry/ndk/INativeScope.java`

- [ ] **Step 1: Add attachment methods to INativeScope**

```java
// Add to INativeScope interface:
void addAttachment(String path);

void clearAttachments();
```

- [ ] **Step 2: Verify it compiles (it won't yet — NativeScope needs updating)**

This is expected to break until Task 2 is done.

---

### Task 2: Implement JNI declarations in NativeScope

**Files:**
- Modify: `modules/sentry-native/ndk/lib/src/main/java/io/sentry/ndk/NativeScope.java`

- [ ] **Step 1: Add native method declarations and interface implementations**

```java
// Add native declarations:
public static native void nativeAddAttachment(String path);

public static native void nativeClearAttachments();

// Add interface implementations:
@Override
public void addAttachment(String path) {
  nativeAddAttachment(path);
}

@Override
public void clearAttachments() {
  nativeClearAttachments();
}
```

---

### Task 3: Implement JNI C functions

**Files:**
- Modify: `modules/sentry-native/ndk/lib/src/main/jni/sentry.c`

- [ ] **Step 1: Add JNI function for addAttachment**

Insert before the `send_envelope` function (around line 230). Follow the naming convention `Java_io_sentry_ndk_NativeScope_nativeAddAttachment`:

```c
JNIEXPORT void JNICALL
Java_io_sentry_ndk_NativeScope_nativeAddAttachment(
        JNIEnv *env,
        jclass cls,
        jstring path) {
    const char *charPath = (*env)->GetStringUTFChars(env, path, 0);

    // The returned sentry_attachment_t* is intentionally discarded.
    // Tracking it across the JNI boundary for individual removal is not
    // worth the complexity. Use sentry_clear_attachments() for bulk removal.
    sentry_attach_file(charPath);

    (*env)->ReleaseStringUTFChars(env, path, charPath);
}

JNIEXPORT void JNICALL
Java_io_sentry_ndk_NativeScope_nativeClearAttachments(JNIEnv *env, jclass cls) {
    sentry_clear_attachments();
}
```

- [ ] **Step 2: Build the NDK module to verify compilation**

Run from `modules/sentry-native/`:
```bash
# The NDK module builds as part of the Android Gradle build
# Verify the JNI function names match the Java declarations
```

- [ ] **Step 3: Commit**

```bash
git add modules/sentry-native/ndk/lib/src/main/java/io/sentry/ndk/INativeScope.java \
       modules/sentry-native/ndk/lib/src/main/java/io/sentry/ndk/NativeScope.java \
       modules/sentry-native/ndk/lib/src/main/jni/sentry.c
git commit -m "Add attachment JNI bridge to sentry-native NDK"
```

---

## Chunk 2: sentry-java Scope Observer Extension

Extend sentry-java's scope observer infrastructure to propagate attachments. This enables the Java → NDK attachment sync chain.

### Task 4: Add attachment methods to IScopeObserver and ScopeObserverAdapter

**Files:**
- Modify: `modules/sentry-java/sentry/src/main/java/io/sentry/IScopeObserver.java`
- Modify: `modules/sentry-java/sentry/src/main/java/io/sentry/ScopeObserverAdapter.java`

- [ ] **Step 1: Add default methods to IScopeObserver**

Use `default` methods to avoid breaking external implementors of the interface (sentry-java uses `default` methods on other interfaces like `ITransport`, `EventProcessor`). This ensures binary compatibility.

```java
// Add to IScopeObserver interface:
default void addAttachment(@NotNull Attachment attachment) {}

default void clearAttachments() {}
```

Requires adding import: `import io.sentry.Attachment;`

- [ ] **Step 2: Add overrides to ScopeObserverAdapter**

Even though the interface has defaults, the adapter should override for consistency with the rest of its methods:

```java
// Add to ScopeObserverAdapter class:
@Override
public void addAttachment(@NotNull Attachment attachment) {}

@Override
public void clearAttachments() {}
```

Requires adding import: `import io.sentry.Attachment;`

---

### Task 5: Notify observers in Scope.addAttachment() and clearAttachments()

**Files:**
- Modify: `modules/sentry-java/sentry/src/main/java/io/sentry/Scope.java`

- [ ] **Step 1: Add observer notification to addAttachment()**

Change from:
```java
@Override
public void addAttachment(final @NotNull Attachment attachment) {
  attachments.add(attachment);
}
```

To:
```java
@Override
public void addAttachment(final @NotNull Attachment attachment) {
  attachments.add(attachment);

  for (final IScopeObserver observer : options.getScopeObservers()) {
    observer.addAttachment(attachment);
  }
}
```

- [ ] **Step 2: Add observer notification to clearAttachments()**

Change from:
```java
@Override
public void clearAttachments() {
  attachments.clear();
}
```

To:
```java
@Override
public void clearAttachments() {
  attachments.clear();

  for (final IScopeObserver observer : options.getScopeObservers()) {
    observer.clearAttachments();
  }
}
```

This propagates through to `NdkScopeObserver.clearAttachments()` → `nativeScope.clearAttachments()` → JNI `sentry_clear_attachments()`, properly cleaning up native-layer state.

---

### Task 6: Implement attachment handling in NdkScopeObserver

**Files:**
- Modify: `modules/sentry-java/sentry-android-ndk/src/main/java/io/sentry/android/ndk/NdkScopeObserver.java`

- [ ] **Step 1: Add addAttachment() implementation**

Follow the existing pattern (async execution via executor service, try-catch with error logging). Only sync file-path attachments — byte-based attachments don't survive native crashes.

```java
@Override
public void addAttachment(final @NotNull Attachment attachment) {
  final String pathname = attachment.getPathname();
  if (pathname == null) {
    // Only file-path attachments can be synced to the native layer.
    // Byte-based attachments exist only in the Java/managed layer.
    return;
  }

  try {
    options.getExecutorService().submit(() -> nativeScope.addAttachment(pathname));
  } catch (Throwable e) {
    options.getLogger().log(SentryLevel.ERROR, e, "Scope sync addAttachment has an error.");
  }
}

@Override
public void clearAttachments() {
  try {
    options.getExecutorService().submit(() -> nativeScope.clearAttachments());
  } catch (Throwable e) {
    options.getLogger().log(SentryLevel.ERROR, e, "Scope sync clearAttachments has an error.");
  }
}
```

Requires adding import: `import io.sentry.Attachment;`

- [ ] **Step 2: Add test for addAttachment in NdkScopeObserverTest**

**File:** `modules/sentry-java/sentry-android-ndk/src/test/java/io/sentry/android/ndk/NdkScopeObserverTest.kt`

Add a test following the existing patterns in the file:

```kotlin
@Test
fun `add file-path attachment syncs to native scope`() {
    val attachment = Attachment("/data/data/com.example/files/log.txt")
    sut.addAttachment(attachment)
    verify(nativeScope).addAttachment("/data/data/com.example/files/log.txt")
}

@Test
fun `add byte attachment does not sync to native scope`() {
    val attachment = Attachment(byteArrayOf(1, 2, 3), "data.bin")
    sut.addAttachment(attachment)
    verify(nativeScope, never()).addAttachment(any())
}
```

- [ ] **Step 3: Run sentry-java tests**

```bash
cd modules/sentry-java
./gradlew :sentry-android-ndk:test
```

- [ ] **Step 4: Commit**

```bash
git add modules/sentry-java/sentry/src/main/java/io/sentry/IScopeObserver.java \
       modules/sentry-java/sentry/src/main/java/io/sentry/ScopeObserverAdapter.java \
       modules/sentry-java/sentry/src/main/java/io/sentry/Scope.java \
       modules/sentry-java/sentry-android-ndk/src/main/java/io/sentry/android/ndk/NdkScopeObserver.java \
       modules/sentry-java/sentry-android-ndk/src/test/java/io/sentry/android/ndk/NdkScopeObserverTest.kt
git commit -m "Add attachment sync to sentry-java scope observer chain"
```

---

## Chunk 3: sentry-dotnet IScopeObserver Extension

Extend the .NET SDK so that `Scope.AddAttachment()` notifies the scope observer, and expose the file path from `FileAttachmentContent`.

### Task 7: Expose FilePath on FileAttachmentContent

**Files:**
- Modify: `src/sentry-dotnet/src/Sentry/FileAttachmentContent.cs`

- [ ] **Step 1: Add public FilePath property**

Change the private field to back a public property:

```csharp
// Change:
private readonly string _filePath;

// To:
private readonly string _filePath;

/// <summary>
/// The path of the file on disk.
/// </summary>
public string FilePath => _filePath;
```

---

### Task 8: Add AddAttachment to .NET IScopeObserver

**Files:**
- Modify: `src/sentry-dotnet/src/Sentry/IScopeObserver.cs`

- [ ] **Step 1: Add AddAttachment method to the interface**

```csharp
/// <summary>
/// Adds an attachment.
/// </summary>
public void AddAttachment(SentryAttachment attachment);
```

---

### Task 9: Call observer from Scope.AddAttachment()

**Files:**
- Modify: `src/sentry-dotnet/src/Sentry/Scope.cs`

- [ ] **Step 1: Add observer notification to AddAttachment**

Change line 393 from:
```csharp
public void AddAttachment(SentryAttachment attachment) => _attachments.Add(attachment);
```

To:
```csharp
public void AddAttachment(SentryAttachment attachment, bool notifyObserver = true)
{
    _attachments.Add(attachment);
    if (notifyObserver && Options.EnableScopeSync)
    {
        Options.ScopeObserver?.AddAttachment(attachment);
    }
}
```

- [ ] **Step 2: Guard Scope.Apply() against re-notification**

`Scope.Apply(Scope other)` calls `other.AddAttachment(attachment)` for each existing attachment during scope cloning. This would trigger duplicate observer notifications. Change the `Apply` method's attachment loop (around line 536) to skip observer notification:

Change from:
```csharp
foreach (var attachment in Attachments)
{
    other.AddAttachment(attachment);
}
```

To:
```csharp
foreach (var attachment in Attachments)
{
    other.AddAttachment(attachment, notifyObserver: false);
}
```

Note: `sentry_attach_file` in sentry-native is also safe against duplicate paths, but skipping the notification is cleaner.

---

### Task 10: Add AddAttachment to sentry-dotnet internal IScopeObserver implementations

The .NET IScopeObserver interface change requires all implementations to add the new method. These are no-op stubs since these platforms either don't use file attachments this way or are handled separately.

**Files:**
- Modify: `src/sentry-dotnet/src/Sentry/Internal/ScopeObserver.cs`
- Modify: `src/sentry-dotnet/src/Sentry/Platforms/Cocoa/CocoaScopeObserver.cs`
- Modify: `src/sentry-dotnet/src/Sentry/Platforms/Android/AndroidScopeObserver.cs`

- [ ] **Step 1: Add to internal ScopeObserver base class**

Check if this is an abstract class. If so, add a virtual no-op:
```csharp
public virtual void AddAttachment(SentryAttachment attachment) { }
```

If it's concrete, add a regular implementation:
```csharp
public void AddAttachment(SentryAttachment attachment) { }
```

- [ ] **Step 2: Add to CocoaScopeObserver and AndroidScopeObserver**

If they inherit from the internal ScopeObserver base, the virtual no-op suffices. If they implement `IScopeObserver` directly, add:
```csharp
public void AddAttachment(SentryAttachment attachment) { }
```

- [ ] **Step 3: Accept API snapshot changes**

Both the IScopeObserver change and the FileAttachmentContent.FilePath property will cause API approval test failures:
```bash
cd src/sentry-dotnet
dotnet test test/Sentry.Tests/ --filter "ApiApprovalTests"
pwsh scripts/accept-verifier-changes.ps1
```

- [ ] **Step 4: Commit**

```bash
git add src/sentry-dotnet/src/Sentry/IScopeObserver.cs \
       src/sentry-dotnet/src/Sentry/Scope.cs \
       src/sentry-dotnet/src/Sentry/FileAttachmentContent.cs \
       src/sentry-dotnet/src/Sentry/Internal/ScopeObserver.cs \
       src/sentry-dotnet/src/Sentry/Platforms/Cocoa/CocoaScopeObserver.cs \
       src/sentry-dotnet/src/Sentry/Platforms/Android/AndroidScopeObserver.cs \
       src/sentry-dotnet/test/Sentry.Tests/
git commit -m "Add attachment notification to .NET IScopeObserver and Scope"
```

---

## Chunk 4: Unity SDK Scope Observer and Platform Implementations

Wire up the Unity SDK's scope observer to forward attachments to native layers.

### Task 11: Add AddAttachment to Unity ScopeObserver base class

**Files:**
- Modify: `src/Sentry.Unity/ScopeObserver.cs`

- [ ] **Step 1: Add AddAttachment template method**

Follow the exact pattern used by `AddBreadcrumb`, `SetTag`, etc. Add after the `SetTrace` method:

```csharp
public void AddAttachment(SentryAttachment attachment)
{
    if (attachment.Content is FileAttachmentContent fileContent)
    {
        _options.LogDebug("{0} Scope Sync - Adding file attachment \"{1}\"", _name, fileContent.FilePath);
        AddFileAttachmentImpl(fileContent.FilePath, attachment.FileName, attachment.ContentType);
    }
    else
    {
        _options.LogDebug("{0} Scope Sync - Skipping non-file attachment \"{1}\" (only file-path attachments sync to native)", _name, attachment.FileName);
    }
}

public abstract void AddFileAttachmentImpl(string filePath, string fileName, string? contentType);
```

Will need to add `using Sentry;` if not already present (for `FileAttachmentContent`).

---

### Task 12: Implement Android attachment sync

**Files:**
- Modify: `src/Sentry.Unity.Android/AndroidJavaScopeObserver.cs`
- Modify: `src/Sentry.Unity.Android/SentryJava.cs`

- [ ] **Step 1: Add AddFileAttachmentImpl to AndroidJavaScopeObserver**

```csharp
public override void AddFileAttachmentImpl(string filePath, string fileName, string? contentType) =>
    _sentryJava.AddAttachment(filePath, fileName, contentType);
```

- [ ] **Step 2: Add AddAttachment JNI method to SentryJava**

Follow the existing `SetTag` / `SetUser` patterns. Use `Sentry.configureScope()` with the `ScopeCallback` proxy to add the attachment to the Java scope:

```csharp
public void AddAttachment(string path, string fileName, string? contentType)
{
    RunJniSafe(() =>
    {
        using var attachment = contentType is not null
            ? new AndroidJavaObject("io.sentry.Attachment", path, fileName, contentType)
            : new AndroidJavaObject("io.sentry.Attachment", path, fileName);

        using var sentry = GetSentryJava();
        using var scopeCallback = new ScopeCallback(scope =>
        {
            scope.Call("addAttachment", attachment);
        });
        sentry.CallStatic("configureScope", scopeCallback);
    });
}
```

- [ ] **Step 3: Add to ISentryJava interface**

Add the method signature to the `ISentryJava` interface (defined at the top of `SentryJava.cs`):

```csharp
void AddAttachment(string path, string fileName, string? contentType);
```

**Note:** `Sentry.configureScope()` executes the callback synchronously, so the `attachment` AndroidJavaObject is guaranteed to be alive during callback execution. If sentry-java ever changes this to async, this would need revisiting.

---

### Task 13: Implement Native (Windows/Linux) attachment sync

**Files:**
- Modify: `src/Sentry.Unity.Native/NativeScopeObserver.cs`
- Modify: `src/Sentry.Unity.Native/CFunctions.cs`

- [ ] **Step 1: Add P/Invoke declaration to CFunctions.cs**

```csharp
[DllImport(SentryLib)]
internal static extern IntPtr sentry_attach_file(string path);
```

- [ ] **Step 2: Add AddFileAttachmentImpl to NativeScopeObserver**

```csharp
public override void AddFileAttachmentImpl(string filePath, string fileName, string? contentType) =>
    C.sentry_attach_file(filePath);
```

---

### Task 14: Add no-op to Unity iOS NativeScopeObserver

**Files:**
- Modify: `src/Sentry.Unity.iOS/NativeScopeObserver.cs`

- [ ] **Step 1: Add stub implementation**

iOS attachment sync to sentry-cocoa is out of scope for this plan. Add a no-op override:

```csharp
public override void AddFileAttachmentImpl(string filePath, string fileName, string? contentType)
{
    // iOS/macOS attachment sync to sentry-cocoa is not yet supported.
}
```

---

### Task 15: Build and test

- [ ] **Step 1: Build the SDK**

```bash
dotnet build
```

- [ ] **Step 2: Run unit tests**

```bash
pwsh scripts/run-tests.ps1
```

- [ ] **Step 3: Commit**

```bash
git add src/Sentry.Unity/ScopeObserver.cs \
       src/Sentry.Unity.Android/AndroidJavaScopeObserver.cs \
       src/Sentry.Unity.Android/SentryJava.cs \
       src/Sentry.Unity.Native/NativeScopeObserver.cs \
       src/Sentry.Unity.Native/CFunctions.cs \
       src/Sentry.Unity.iOS/NativeScopeObserver.cs
git commit -m "Add cross-layer attachment sync to Unity scope observers"
```

---

## Design Decisions

### Why file-path attachments only for native sync?

Native crash handlers read attachments from disk at crash time. In-memory byte arrays and streams in managed (C#/Java) memory cannot survive a native crash — the process memory is corrupted. File paths are the only reliable mechanism. If byte-based attachments need to reach native crashes in the future, the Unity SDK can write bytes to a temp file under `Application.persistentDataPath` and register that path.

### Why use `Sentry.configureScope()` for the Android JNI call?

sentry-java doesn't expose a static `Sentry.addAttachment()` convenience method. The `configureScope` + `ScopeCallback` pattern is already used by the Unity SDK for scope writes and is the standard way to modify the Java scope from C#. When `scope.addAttachment()` is called on the Java side, the observer chain fires automatically, propagating to the NDK layer.

### Why `clearAttachments()` instead of per-item `removeAttachment()`?

`sentry_remove_attachment()` in sentry-native takes an opaque `sentry_attachment_t*` pointer returned by `sentry_attach_file()`. Tracking these pointers across JNI boundaries adds significant complexity. Instead, we expose `sentry_clear_attachments()` for bulk removal — it's simpler and covers the primary use case (scope reset). Individual removal can be added later if needed.

### Why `default` methods on Java IScopeObserver?

Adding abstract methods to the `IScopeObserver` interface would break external code that implements it directly (rather than extending `ScopeObserverAdapter`). Using `default` methods with empty bodies maintains binary compatibility while still allowing concrete classes like `NdkScopeObserver` to override.

### Why extend IScopeObserver in sentry-java rather than bypass it?

The observer chain (Java Scope → NdkScopeObserver → sentry-native) is the established pattern for scope sync. Extending it means any sentry-java user (not just Unity) benefits from attachment sync to NDK. It also means Unity only pushes to one layer (Java), and the rest flows automatically.

---

## Known Limitations

- **`fileName` and `contentType` not propagated to sentry-native.** The `sentry_attachment_t*` return value from `sentry_attach_file()` is discarded, so `sentry_attachment_set_filename()` and `sentry_attachment_set_content_type()` cannot be called. sentry-native will use the basename of the file path as the filename.
- **Windows non-ASCII paths.** The P/Invoke `sentry_attach_file(string path)` marshals as ANSI. For Windows paths with non-ASCII characters, `sentry_attach_filew` should be used instead. This is a pre-existing limitation shared with all other P/Invoke string calls in the codebase.

---

## Future Work

- **iOS/macOS (sentry-cocoa):** Extend `SentryCocoaBridgeProxy` and the Objective-C bridge to support attachment sync. sentry-cocoa has its own attachment API that would need bridging.
- **Byte attachment sync:** Write byte/stream attachments to a temp file under a known path and register the path with native layers.
- **Individual attachment removal:** Track `sentry_attachment_t*` pointers through JNI to support `sentry_remove_attachment()`.
- **View hierarchy / screenshot as native crash attachments:** Use this infrastructure to pre-write view hierarchy snapshots to disk and register them, so they're included in native crash reports.
- **Windows wide-char path support:** Add `sentry_attach_filew` P/Invoke for proper non-ASCII path handling on Windows.
