# Capturing a raw envelope + debug file corpus from the integration tests

The integration tests exercise every managed error, native crash and app hang path the SDK has, on
every platform we ship. Capture mode records what those runs actually put on the wire - raw
envelopes, minidump uploads, and the debug files sentry-cli uploads at build time - so the whole lot
can be replayed against a local Sentry instead of going to sentry.io.

| | |
|---|---|
| [`test/Scripts.Integration.Test/envelope-capture-server.py`](../test/Scripts.Integration.Test/envelope-capture-server.py) | Stands in for both Sentry endpoints: envelope ingest, and the chunk-upload API sentry-cli uses for debug files. Writes everything to disk. |
| [`scripts/replay-envelopes.py`](../scripts/replay-envelopes.py) | Posts a captured envelope corpus to a DSN of your choice. |

**This is a temporary branch change, not a feature.** Two settings redirect the two halves:

- `ci.yml` hardcodes the DSN to `http://capture@127.0.0.1:8787/1` in place of `SENTRY_TEST_DSN`, so
  the SDK sends **envelopes** there at run time.
- the build jobs set `SENTRY_URL` to the same address, so sentry-cli uploads **debug files** there
  at build time.

`SENTRY_URL` is needed because sentry-cli 3.x **ignores `defaults.url` in `sentry.properties`**,
which is the only way the SDK knows how to redirect it
([`SentryCli.UrlOverride`](../src/Sentry.Unity.Editor/SentryCli.cs)). Without it the DSN alone
leaves symbol upload pointed at sentry.io, and the build still reports success - worth fixing
upstream, since it means self-hosted users silently upload their symbols to sentry.io.

There is nothing to toggle: push the branch, open the PR, wait for CI, fetch the artifacts. Revert
the commit once you have the corpus.

## What CI produces

| Artifact | From | Contents |
|---|---|---|
| `envelopes-all` | run jobs, merged | every envelope and minidump upload, one directory per platform |
| `symbols-<platform>-<unity-version>` | build jobs | the debug files and source bundles sentry-cli uploaded for that build |

```bash
gh run download <run-id> -n envelopes-all -D ./corpus
gh run download <run-id> -p 'symbols-*' -D ./corpus/symbols
```

Debug files are big (IL2CPP `GameAssembly.pdb` and friends), so they stay per-platform rather than
being merged into one download.

**The integration tests fail by design in capture mode.** There is no backend to verify against, so
`Integration.Tests.ps1` skips the Sentry API lookups and every event assertion fails. The artifacts
are the deliverable; a red run is expected. The symbol-upload assertions are the exception and still
mean something: they pass only if sentry-cli really did upload debug files to the capture server.

Coverage per matrix entry: `message-capture`, `exception-capture`, `crash-capture` (+ the
`crash-send` relaunch that flushes the crash envelope) and `app-hang-capture`, each of which also
emits logs, metrics, sessions and a transaction. Windows/macOS/Linux run twice, once per crash
backend (`crashpad`/`breakpad`/`native`/`cocoa`), so the corpus covers each native payload shape.

Details that took a few CI rounds to get right, in case any of them regress:

- `webgl-server.py` serves the WebGL build on port 8000, so capture listens on **8787**.
- Linux/Android/iOS builds run Unity inside a container, so [`ci-docker.sh`](../scripts/ci-docker.sh)
  uses `--network host` and forwards `SENTRY_URL` to let the in-container sentry-cli reach the host.
- The capture server is started **inside** the build/test step that needs it. A detached server does
  not survive the gap between steps - the runner leaves it suspended, holding the port without
  answering, which shows up as "Empty reply from server". The launcher clears such a leftover first.
- The iOS compile job sets a dummy `SENTRY_AUTH_TOKEN`, because sentry-cli refuses to combine a URL
  from the environment with the auth token baked into `sentry.properties`.
- Each build job asserts that debug files actually landed. sentry-cli reporting success is not proof
  it reached the capture server - it happily falls back to sentry.io.

### What CI no longer does on this branch

Stripped to keep the run short and the failures meaningful: the UPM package snapshot validation, all
build-size measurement (including every "build without Sentry" pass and the `build-size-summary`
job), and the dependency-conflict package steps.

## What lands on disk

One directory per matrix entry, so the merged corpus stays collision-free:

```
macos-cocoa-6000.5/001-macos-cocoa-6000.5-exception-capture-event_attachment.envelope   # raw bytes, gunzipped
macos-cocoa-6000.5/001-macos-cocoa-6000.5-exception-capture-event_attachment.meta.json  # path, headers, item types
windows-crashpad-6000.5/003-...-crash-capture-minidump.multipart.bin                    # crashpad minidump upload
windows-crashpad-6000.5/index.jsonl                                                     # one line per request
symbols/macos-6000.5/debug-files/<debug-id>-<checksum>-GameAssembly.dylib                # debug companion
symbols/macos-6000.5/debug-files/<debug-id>-<checksum>-GameAssembly.dylib.src            # source bundle
symbols/macos-6000.5/debug-files/index.jsonl                                             # debug id -> file
```

A dif and its source bundle share a debug id *and* a name, so the checksum in the file name is what
keeps them apart.

## Replaying into a local Sentry

```bash
# events, crashes, sessions, logs
python3 scripts/replay-envelopes.py ./corpus --dsn http://<key>@localhost:9000/1

# the debug files that symbolicate them
sentry-cli --url http://localhost:9000 --auth-token <token> debug-files upload \
  -o <org> -p <project> ./corpus/symbols/macos-6000.5/debug-files
```

sentry-cli reads the captured files straight out of the artifact and re-uploads them under their
original debug ids, which is what lets the replayed crashes symbolicate.

Each envelope is rewritten before it is posted: the DSN in the envelope header is swapped for the
target, `sent_at` is set to now, event ids are regenerated and all timestamps are shifted to now
while keeping their relative offsets (breadcrumbs, spans, session start). That keeps a corpus
replayable indefinitely without deduplicating against itself or falling outside the ingest window.
Pass `--keep-ids` / `--keep-timestamps` to replay the bytes as they were captured.

Minidump uploads are replayed verbatim to `/api/<project>/minidump/` with only the ingest key
swapped - the event ids inside the multipart body are left alone.

## Capturing locally

```bash
python3 test/Scripts.Integration.Test/envelope-capture-server.py --output ./out --platform macos
SENTRY_DSN="http://capture@127.0.0.1:8787/1" \
  ./test/Scripts.Integration.Test/dev-integration-test.ps1 -UnityVersion 6000.5 -Platform MacOS
```

Any DSN whose host is `127.0.0.1`, `localhost` or `10.0.2.2` puts `Integration.Tests.ps1` into
capture mode. Note that this also applies when you point the tests straight at a locally running
Sentry - the run works, but the API verification is skipped.
