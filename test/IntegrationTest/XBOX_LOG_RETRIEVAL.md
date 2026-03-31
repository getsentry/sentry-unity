# Xbox Integration Test Log Retrieval — Status

## Problem
Xbox master (non-development) builds suppress all `Debug.Log` output. The integration tests parse app output for `EVENT_CAPTURED:` lines, so they get nothing and fail.

## What we built
- **`Logger.cs`** — Static helper that writes to both `Debug.Log` and an optional file via `StreamWriter` with `AutoFlush`. Throws on `Open()` failure (no silent swallowing).
- **`IntegrationTester.cs`** — All `Debug.Log` calls replaced with `Logger.Log`. On `UNITY_GAMECORE`, tries candidate paths in order: `D:\Logs\`, `persistentDataPath`, `temporaryCachePath`, `D:\` root. Writes a breadcrumb file (`D:\Logs\unity-integration-test-path.txt`) with the path that succeeded. On total failure, writes diagnostic info to `D:\unity-integration-test-diag.txt` before crashing.
- **`Integration.Tests.ps1`** — `Get-XboxLogOutput` function: checks for breadcrumb file first, then tries candidate xbcopy paths (D:\Logs first), then retrieves diagnostic/crash files and dumps directory listings.

## What we learned from CI runs

### Run 1 (single path, `persistentDataPath` only)
- App exited with code 1 — `Application.persistentDataPath` is not writable in packaged master builds.
- `xbdir.exe` doesn't exist in the GDK installation.

### Run 2 (fallback path logic added)
- App exited with code 0 — one of the 4 candidate paths succeeded.
- `D:\DevelopmentFiles\<PackageFamilyName>\LocalState` and `AC\LocalState` don't exist (0x80070002).
- `D:\Logs` exists (xbcopy didn't error) but the log file wasn't in it — the app probably didn't write there.
- `T:\` has a 1.5GB locked file (pagefile?) — error 0x80070020. Our log file wasn't visible there either.
- `xbcopy.exe` not found when called directly — needs full path via `$env:GameDK`.
- **We don't know which candidate path the app actually used.** The breadcrumb file mechanism was added after this run.

### Run 3 (breadcrumb + diagnostics + 4 candidates: persistentDataPath, temporaryCachePath, D:\Logs\, T:\)
- **message-capture exited with code 1** — all 4 candidate paths failed, app threw `IOException`.
- **exception-capture and crash-capture** ran without exit-code-1 warning but still produced no log file.
- `D:\FullException.dmp` (94KB) and `D:\FullExceptionLogFile.txt` (44 bytes) present on `D:\` — crash dump from our `IOException` throw.
- `D:\DevelopmentFiles\IntegrationTest6000.3.8f1_8wekyb3d8bbwe\` — **does not exist** (0x80070002). No subdirectories exist either (LocalState, AC, TempState all 0x80070002).
- `D:\DevelopmentFiles\` — exists but is **completely empty** (0 files).
- `D:\Logs\` — exists and has 12 files from other apps (`SentryPlayground*.log`, `SentryTower.log`) but **no `unity-integration-test.log`**. This means even though `D:\Logs\` is the 3rd candidate, the app crashes before reaching it (the first two candidates fail in a way that either hard-crashes or throws).
- `T:\` — 1 file (1.5GB pagefile), locked (0x80070020).
- `S:\` — exists but empty.
- `D:\` root — has `FullException.dmp`, `FullExceptionLogFile.txt`, `latest_stderr.txt` (0 bytes), `latest_stdout.txt` (0 bytes), and an old Unreal dump.
- No breadcrumb file was found.
- **Conclusion**: The app crashes attempting to write to the first candidates. `D:\Logs\` was never tried because of the crash. The fix is to try `D:\Logs\` first (known writable path) and also add `D:\` root as a fallback.

## Changes made after Run 3
1. **Reordered C# candidates**: `D:\Logs\` first, then `persistentDataPath`, `temporaryCachePath`, then `D:\` root (removed `T:\` — pagefile only).
2. **Safe access to Unity paths**: Wrapped `Application.persistentDataPath`/`temporaryCachePath` in try-catch in case they throw.
3. **Diagnostic file on failure**: App writes `D:\unity-integration-test-diag.txt` with error details before crashing, so the PS harness can read it.
4. **PS-side improvements**: Reordered candidate dirs to match C# order. Added retrieval of `D:\unity-integration-test-diag.txt` and `D:\FullExceptionLogFile.txt` on failure. Removed `T:\` from candidates.

## Next steps
1. **Push and run CI.** With `D:\Logs\` as the first candidate, the app should succeed in opening the log file there (other apps like SentryPlayground write there successfully).
2. **If `D:\Logs\` also fails for our app** (permission is per-app in master builds), the diagnostic file at `D:\unity-integration-test-diag.txt` will reveal the exact error for each candidate.
3. **If `D:\` root works but `D:\Logs\` doesn't**, we'll know from the diagnostic file and can switch the primary candidate.
4. **If nothing works**, we may need to switch to a development build subtarget or use the Windows Device Portal REST API.

## Key files
- `test/Scripts.Integration.Test/Scripts/Logger.cs` — File-based logger
- `test/Scripts.Integration.Test/Scripts/IntegrationTester.cs` — Test app entry point
- `test/IntegrationTest/Integration.Tests.ps1` — Test harness (Get-XboxLogOutput, Invoke-XboxDirListing)
- `modules/app-runner/app-runner/Private/DeviceProviders/XboxProvider.ps1` — Xbox device provider (CopyDeviceItem uses xbcopy)

## Key facts
- Package family name format: `IntegrationTest6000.3.8f1_8wekyb3d8bbwe`
- AUMID format: `IntegrationTest6000.3.8f1_8wekyb3d8bbwe!Game`
- GDK tools at: `C:\Program Files (x86)\Microsoft GDK\bin\` (resolved via `$env:GameDK`)
- `xbcopy` prefix `x` means Xbox device path (e.g., `xD:\Logs`)
- `xbcopy /mirror` copies directory contents, shows file count even on failure
- `xbdir.exe` does NOT exist in this GDK version
- Build is master subtarget (set in `Builder.cs` via `SetXboxSubtargetToMaster()`)
- `D:\DevelopmentFiles\<PackageFamilyName>\` does NOT exist for master builds (no sandbox storage)
- `D:\Logs\` is writable by other apps (SentryPlayground, SentryTower) — 12 log files present
- `D:\` root is writable (crash dumps land there: FullException.dmp, FullExceptionLogFile.txt)
- `T:\` only has a locked 1.5GB pagefile — not usable
- `S:\` exists but is empty
