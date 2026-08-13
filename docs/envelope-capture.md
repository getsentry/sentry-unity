# Capturing a raw envelope corpus from the integration tests

The integration tests exercise every managed error, native crash and app hang path the SDK has, on
every platform we ship. Capture mode records what those runs actually put on the wire - raw
envelopes, minidump uploads and all - so the payloads can be replayed against a local Sentry instead
of being sent to sentry.io.

Two pieces:

| | |
|---|---|
| [`test/Scripts.Integration.Test/envelope-capture-server.py`](../test/Scripts.Integration.Test/envelope-capture-server.py) | Stands in for the ingest endpoint. Writes every request to disk and answers `200`. |
| [`scripts/replay-envelopes.py`](../scripts/replay-envelopes.py) | Posts a captured corpus to a DSN of your choice. |

## Capturing from CI

**This is a temporary branch change, not a feature.** `ci.yml` hardcodes the DSN to
`http://capture@127.0.0.1:8787/1` in place of `SENTRY_TEST_DSN`, because the DSN is baked into
`SentryOptions.asset` at build time and so has to be switched for the whole pipeline. There is
nothing to toggle: push the branch, open the PR, wait for CI, fetch the artifact. Revert the commit
once you have the corpus.

Every run job:

1. starts the capture server on `127.0.0.1:8787` (`adb reverse` tunnels the port on Android; the iOS
   simulator and WebGL's headless Chrome share the runner's loopback),
2. runs the usual test actions, marking each captured file with the action it belongs to,
3. uploads its own `envelopes-<platform>-<unity-version>` artifact.

The `collect-envelopes` job merges all of them into a single **`envelopes-all`** artifact - one
download for the whole matrix - and writes a per-platform payload count to the run summary:

```bash
gh run download <run-id> -n envelopes-all -D ./corpus
```

Two things the capture DSN would otherwise break, both handled:

- `webgl-server.py` serves the WebGL build on port 8000, so the capture server listens on **8787**.
- sentry-cli takes its upload URL from the DSN whenever that DSN is not sentry.io
  ([`SentryCli.UrlOverride`](../src/Sentry.Unity.Editor/SentryCli.cs)), which would point symbol
  upload at the capture server. `CliConfiguration` pins `UrlOverride` to `https://sentry.io`.

**The integration tests fail by design in capture mode.** There is no backend to verify against, so
`Integration.Tests.ps1` skips the Sentry API lookups and every event assertion fails. The artifacts
are the deliverable; a red run is expected.

Coverage per matrix entry: `message-capture`, `exception-capture`, `crash-capture` (+ the
`crash-send` relaunch that flushes the crash envelope) and `app-hang-capture`, each of which also
emits logs, metrics, sessions and a transaction. Windows/macOS/Linux run twice, once per crash
backend (`crashpad`/`breakpad`/`native`/`cocoa`), so the corpus covers each native payload shape.

## Capturing locally

```bash
python3 test/Scripts.Integration.Test/envelope-capture-server.py --output ./envelopes --platform macos
SENTRY_DSN="http://capture@127.0.0.1:8787/1" \
  ./test/Scripts.Integration.Test/dev-integration-test.ps1 -UnityVersion 6000.2 -Platform MacOS
```

Any DSN whose host is `127.0.0.1`, `localhost` or `10.0.2.2` puts `Integration.Tests.ps1` into
capture mode. Note that this also applies when you point the tests straight at a locally running
Sentry - the run works, but the API verification is skipped.

## What lands on disk

One directory per matrix entry, so the merged corpus stays collision-free:

```
macos-cocoa-6000.2/001-macos-cocoa-6000.2-exception-capture-event_attachment.envelope   # raw bytes, gunzipped
macos-cocoa-6000.2/001-macos-cocoa-6000.2-exception-capture-event_attachment.meta.json  # path, headers, item types
windows-crashpad-6000.2/003-...-crash-capture-minidump.multipart.bin                    # crashpad minidump upload
windows-crashpad-6000.2/index.jsonl                                                     # one line per request
windows-crashpad-6000.2/capture-server.log
```

## Replaying into a local Sentry

```bash
python3 scripts/replay-envelopes.py ./envelopes --dsn http://<key>@localhost:9000/1
python3 scripts/replay-envelopes.py ./envelopes --dsn ... --include '*crash*' --dry-run
```

Each envelope is rewritten before it is posted: the DSN in the envelope header is swapped for the
target, `sent_at` is set to now, event ids are regenerated and all timestamps are shifted to now
while keeping their relative offsets (breadcrumbs, spans, session start). That keeps a corpus
replayable indefinitely without deduplicating against itself or falling outside the ingest window.
Pass `--keep-ids` / `--keep-timestamps` to replay the bytes as they were captured.

Minidump uploads are replayed verbatim to `/api/<project>/minidump/` with only the ingest key
swapped - the event ids inside the multipart body are left alone.
