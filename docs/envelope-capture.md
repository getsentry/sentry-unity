# Capturing an envelope + debug file corpus

The integration tests exercise every managed error, native crash and app hang path the SDK has, on
every platform we ship. Capture mode records what those runs actually put on the wire - raw
envelopes, minidump uploads, and the debug files sentry-cli uploads - so the whole lot can be
replayed against a local Sentry to work on event processing or symbolication.

It is **off by default** and changes nothing about a normal CI run.

## Running it

Actions → **CI** → *Run workflow* → tick **capture-corpus**.

Every build and run job then writes its corpus to an artifact:

```bash
gh run download <run-id> -p 'corpus-*' -D ./corpus
```

| Artifact | Contains |
|---|---|
| `corpus-<platform>-<unity-version>` | debug files and source bundles sentry-cli uploaded for that build |
| `corpus-run-<platform>-<backend>-<unity-version>` | the envelopes and minidump uploads that run produced |

Debug files are large (IL2CPP `GameAssembly.pdb` and friends run to hundreds of MB per platform), so
they stay per-job rather than being merged into one download.

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
- Capture listens on **8787**; `webgl-server.py` already serves the WebGL build on 8000.
- macOS ATS blocks plain HTTP to an IP literal, so the test app's `Info.plist` gets
  `NSAllowsArbitraryLoads` ([`AllowInsecureHttp.cs`](../test/Scripts.Integration.Test/Editor/AllowInsecureHttp.cs)).

## Capturing locally

```bash
SENTRY_CAPTURE_PATH=$PWD/corpus/macos \
  ./test/Scripts.Integration.Test/dev-integration-test.ps1 -UnityVersion 6000.5 -Platform MacOS
```

The same hooks apply, so a local run produces the same corpus layout as CI.
