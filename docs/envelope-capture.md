# Capturing an envelope + debug file corpus

The integration tests exercise every managed error, native crash and app hang path the SDK has, on
every platform we ship. Capture mode records what those runs actually put on the wire - raw
envelopes, minidump uploads, and the debug files, source bundles and IL2CPP line mappings
sentry-cli uploads - so the whole lot can be replayed against a local Sentry to work on event
processing or symbolication.

It is **off by default** and changes nothing about a normal CI run.

## Running it

Actions → **CI** → *Run workflow* → tick **capture-corpus**.

Every build and run job then writes its corpus to an artifact:

```bash
gh run download <run-id> -p 'corpus-*' -D ./corpus
```

| Artifact | Contains |
|---|---|
| `corpus-<platform>-<unity-version>` | debug files, source bundles and IL2CPP line mappings sentry-cli uploaded for that build |
| `corpus-run-<platform>-<backend>-<unity-version>` | the envelopes and minidump uploads that run produced |

Debug files are large (IL2CPP `GameAssembly.pdb` and friends run to hundreds of MB per platform), so
they stay per-job rather than being merged into one download.

Everything sentry-cli uploaded lands in `debug-files/`, named
`<debug-id>-<checksum>-<name>` and suffixed by kind:

| Suffix | Kind | |
|---|---|---|
| *(none)* | `debug-file` | the dSYM / PDB / ELF itself |
| `.src` | `source-bundle` | the sources, from `--include-sources` |
| `.il2cpp.json` | `il2cpp-line-mapping` | C++ → C# line mapping, from `--il2cpp-mapping` |
| `.proguard` | `proguard-mapping` | Android `mapping.txt`, from `upload-proguard` |

`debug-files/index.jsonl` records the `kind` alongside the assemble request, and the server prints
a per-kind tally when it shuts down.

**The event assertions fail by design in a capture run.** There is no backend to verify against, so
`Integration.Tests.ps1` skips the Sentry API lookups and every event assertion fails. The artifacts
are the deliverable; a red run is expected.

Coverage per matrix entry: `message-capture`, `exception-capture`, `crash-capture` (+ the
`crash-send` relaunch that flushes the crash envelope) and `app-hang-capture`, each of which also
emits logs, metrics, sessions and a transaction. Windows/macOS/Linux run twice, once per crash
backend (`crashpad`/`breakpad`/`native`/`cocoa`), so the corpus covers each native payload shape.

## Replaying into a local Sentry

```bash
# events, crashes, sessions, logs
python3 scripts/replay-envelopes.py ./corpus --dsn http://<key>@localhost:9000/1

# the debug files that symbolicate them
sentry-cli --url http://localhost:9000 --auth-token <token> debug-files upload \
  -o <org> -p <project> ./corpus/corpus-macos-6000.5/macos-6000.5/debug-files
```

Each envelope is rewritten before it is posted: the DSN in the envelope header is swapped for the
target, `sent_at` is set to now, event ids are regenerated and all timestamps are shifted to now
while keeping their relative offsets (breadcrumbs, spans, session start). That keeps a corpus
replayable indefinitely without deduplicating against itself or falling outside the ingest window.
Pass `--keep-ids` / `--keep-timestamps` to replay the bytes as they were captured.

Minidump uploads are replayed verbatim to `/api/<project>/minidump/` with only the ingest key
swapped - the event ids inside the multipart body are left alone.

`debug-files upload` re-uploads the difs and source bundles, but **not** the `.il2cpp.json`
mappings: sentry-cli only picks up files it recognises as difs, and it recomputes mappings from the
generated C++ next to the object rather than from a mapping file. The C++ is not in the corpus, so
the captured `.il2cpp.json` is the only copy - read it directly, or POST it to the chunk-upload and
`files/difs/assemble/` endpoints the way sentry-cli does.

## How it works

Everything keys off one environment variable, **`SENTRY_CAPTURE_PATH`**. The workflows set it from
the `capture` input; unset, every hook below is a no-op.

| Piece | Role |
|---|---|
| [`capture-corpus.ps1`](../test/Scripts.Integration.Test/capture-corpus.ps1) | `Test-CaptureEnabled` / `Start-CaptureServer` / `Set-CaptureLabel`, and the capture DSN and port |
| [`envelope-capture-server.py`](../test/Scripts.Integration.Test/envelope-capture-server.py) | Stands in for both Sentry endpoints: envelope ingest, and the chunk-upload API sentry-cli uses for debug files |
| [`replay-envelopes.py`](../scripts/replay-envelopes.py) | Posts a captured corpus to a DSN of your choice |
| `configure-sentry.ps1` | Bakes the capture DSN into the test app |
| `build-project.ps1`, `compile-xcode-project.ps1` | Start the server and point sentry-cli at it for the build |
| `Integration.Tests.ps1` | Starts the server for the test run, labels each action, skips API verification |
| `ci-docker.sh` | In capture mode only, shares the host network so the in-container sentry-cli can reach the server |

Details worth knowing if any of this regresses:

- The server is started **inside** the build or test step that needs it, never in a step of its own:
  a server started earlier does not survive the gap - the runner leaves it suspended, holding the
  port without answering, which surfaces as "Empty reply from server". `Start-CaptureServer` clears
  such a leftover before binding, and is safe to call repeatedly within a job.
- `SENTRY_URL` is what redirects sentry-cli. **sentry-cli 3.x ignores `defaults.url` in
  `sentry.properties`**, which is the only knob the SDK offers
  ([`SentryCli.UrlOverride`](../src/Sentry.Unity.Editor/SentryCli.cs)) - so without it, symbol upload
  silently goes to sentry.io and the build still reports success. That also means self-hosted users
  currently upload their symbols to sentry.io; worth fixing upstream.
- The iOS Xcode phase additionally needs `SENTRY_AUTH_TOKEN` from the environment, because sentry-cli
  refuses to combine a URL from the environment with a token from `sentry.properties`.
- IL2CPP line mappings need no wiring of their own: `--il2cpp-mapping` is part of the same
  `debug-files upload` the difs go through ([`BuildPostProcess`](../src/Sentry.Unity.Editor/Native/BuildPostProcess.cs),
  [`DebugSymbolUpload`](../src/Sentry.Unity.Editor/Android/DebugSymbolUpload.cs),
  [`SentryXcodeProject`](../src/Sentry.Unity.Editor.iOS/SentryXcodeProject.cs)), and they are chunked
  and assembled like everything else. They are told apart **by content**: assemble carries only a
  name, a debug id and the chunks, and a mapping inherits name and debug id from the object it was
  computed from, so only the payload distinguishes them (`SYSB` magic, or a leading `{` for the
  mapping JSON).
- Android proguard mappings ride the same path despite being a separate `upload-proguard`
  invocation: sentry-cli chunk-uploads them and assembles them through `files/difs/assemble/` like
  everything else. They are the one kind identifiable by metadata - sentry-cli names them
  `/proguard/<uuid>.txt` and sends no debug id, hence the `unknown-` prefix in the corpus.
- A proguard mapping only exists when minification is on: `sentryUploadProguardMapping` is
  registered from [`AndroidUtils.ShouldUploadMapping`](../src/Sentry.Unity.Editor/Android/AndroidUtils.cs),
  which reads `PlayerSettings.Android.minifyRelease` (release, because the test builds set
  `EditorUserBuildSettings.development = false`). The integration test turns both minify flags on
  in [`Builder.cs`](../test/Scripts.Integration.Test/Editor/Builder.cs), so an Android capture run
  is expected to show a `proguard-mapping` in the tally. If it does not, check that flag first.
- A capture run where the tally shows difs but no `il2cpp-line-mapping` means IL2CPP line numbers
  regressed upstream of the upload - either `--emit-source-mapping` never reached il2cpp
  ([`Il2CppBuildPreProcess`](../src/Sentry.Unity.Editor/Il2CppBuildPreProcess.cs), gated on
  `Il2CppLineNumberSupportEnabled`), or the generated C++ was gone by the time sentry-cli ran, since
  it reads the `source_info` comments back out of those files.
- Capture listens on **8787**; `webgl-server.py` already serves the WebGL build on 8000.
- macOS ATS blocks plain HTTP to an IP literal, so the test app's `Info.plist` gets
  `NSAllowsArbitraryLoads` ([`AllowInsecureHttp.cs`](../test/Scripts.Integration.Test/Editor/AllowInsecureHttp.cs)).

## Capturing locally

```bash
SENTRY_CAPTURE_PATH=$PWD/corpus/macos \
  ./test/Scripts.Integration.Test/dev-integration-test.ps1 -UnityVersion 6000.5 -Platform MacOS
```

The same hooks apply, so a local run produces the same corpus layout as CI.
