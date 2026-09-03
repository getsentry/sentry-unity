#!/usr/bin/env python3
"""Replays a captured envelope corpus into a Sentry instance.

Takes the output of test/Scripts.Integration.Test/envelope-capture-server.py (envelopes and
crashpad minidump uploads produced by the Unity integration tests on every platform) and posts
it to the DSN of your choice - typically a local Sentry.

By default every replay gets fresh event ids and timestamps shifted to now, so the same corpus
can be replayed repeatedly without events deduplicating or falling outside the ingest window.

Usage:
    replay-envelopes.py <corpus-dir> --dsn http://<key>@localhost:9000/1
    replay-envelopes.py <corpus-dir> --dsn ... --include '*crash*' --dry-run
"""

import argparse
import fnmatch
import json
import sys
import urllib.error
import urllib.request
import uuid
from datetime import datetime, timezone
from pathlib import Path
from urllib.parse import parse_qsl, urlencode, urlparse

TIMESTAMP_KEYS = {"timestamp", "start_timestamp", "started", "received", "time"}
JSON_ITEM_TYPES = {"event", "transaction", "session", "sessions", "check_in", "log", "feedback",
                   "user_report", "replay_event", "profile", "client_report"}


def parse_envelope(data):
    """Splits envelope bytes into (header, [(item_header, payload)])."""
    newline = data.find(b"\n")
    if newline == -1:
        raise ValueError("no envelope header")
    header = json.loads(data[:newline])
    items = []
    pos = newline + 1
    while pos < len(data):
        if data[pos:pos + 1] == b"\n":
            pos += 1
            continue
        newline = data.find(b"\n", pos)
        if newline == -1:
            break
        item_header = json.loads(data[pos:newline])
        pos = newline + 1
        if "length" in item_header:
            end = pos + int(item_header["length"])
        else:
            end = data.find(b"\n", pos)
            if end == -1:
                end = len(data)
        items.append((item_header, data[pos:end]))
        pos = end
    return header, items


def serialize_envelope(header, items):
    out = [json.dumps(header, separators=(",", ":")).encode(), b"\n"]
    for item_header, payload in items:
        item_header = dict(item_header, length=len(payload))
        out += [json.dumps(item_header, separators=(",", ":")).encode(), b"\n", payload, b"\n"]
    return b"".join(out)


def to_epoch(value):
    if isinstance(value, (int, float)):
        return float(value)
    if isinstance(value, str):
        try:
            return datetime.fromisoformat(value.replace("Z", "+00:00")).timestamp()
        except ValueError:
            return None
    return None


def from_epoch(epoch, template):
    if isinstance(template, (int, float)):
        return epoch
    return datetime.fromtimestamp(epoch, timezone.utc).isoformat().replace("+00:00", "Z")


def collect_timestamps(node, found):
    if isinstance(node, dict):
        for key, value in node.items():
            if key in TIMESTAMP_KEYS:
                epoch = to_epoch(value)
                if epoch:
                    found.append(epoch)
            collect_timestamps(value, found)
    elif isinstance(node, list):
        for value in node:
            collect_timestamps(value, found)


def shift_timestamps(node, delta):
    if isinstance(node, dict):
        for key, value in node.items():
            if key in TIMESTAMP_KEYS:
                epoch = to_epoch(value)
                if epoch:
                    node[key] = from_epoch(epoch + delta, value)
                    continue
            shift_timestamps(value, delta)
    elif isinstance(node, list):
        for value in node:
            shift_timestamps(value, delta)


def rewrite(data, dsn, new_ids, fresh_timestamps):
    header, items = parse_envelope(data)
    header["dsn"] = dsn.url
    header["sent_at"] = datetime.now(timezone.utc).isoformat().replace("+00:00", "Z")

    parsed = []
    for item_header, payload in items:
        if item_header.get("type") in JSON_ITEM_TYPES:
            try:
                parsed.append((item_header, json.loads(payload), True))
                continue
            except (ValueError, UnicodeDecodeError):
                pass
        parsed.append((item_header, payload, False))

    if fresh_timestamps:
        found = []
        for _, payload, is_json in parsed:
            if is_json:
                collect_timestamps(payload, found)
        if found:
            delta = datetime.now(timezone.utc).timestamp() - max(found)
            for _, payload, is_json in parsed:
                if is_json:
                    shift_timestamps(payload, delta)

    if new_ids:
        event_id = uuid.uuid4().hex
        if "event_id" in header:
            header["event_id"] = event_id
        for _, payload, is_json in parsed:
            if is_json and isinstance(payload, dict) and "event_id" in payload:
                payload["event_id"] = event_id

    rebuilt = [
        (item_header, json.dumps(payload, separators=(",", ":")).encode() if is_json else payload)
        for item_header, payload, is_json in parsed
    ]
    return serialize_envelope(header, rebuilt)


class Dsn:
    def __init__(self, url):
        parsed = urlparse(url)
        if not parsed.username or not parsed.hostname or len(parsed.path) < 2:
            raise ValueError(f"not a valid DSN: {url}")
        self.url = url
        self.key = parsed.username
        self.project = parsed.path.strip("/")
        port = f":{parsed.port}" if parsed.port else ""
        self.base = f"{parsed.scheme}://{parsed.hostname}{port}/api/{self.project}"

    def endpoint(self, name):
        return f"{self.base}/{name}/"


def post(url, body, content_type, dsn, timeout):
    auth = f"Sentry sentry_version=7, sentry_client=replay-envelopes/1.0, sentry_key={dsn.key}"
    request = urllib.request.Request(
        url, data=body, method="POST",
        headers={"Content-Type": content_type, "X-Sentry-Auth": auth})
    try:
        with urllib.request.urlopen(request, timeout=timeout) as response:
            return response.status, response.read(200).decode("utf-8", "replace")
    except urllib.error.HTTPError as error:
        return error.code, error.read(400).decode("utf-8", "replace")
    except urllib.error.URLError as error:
        return None, str(error.reason)


def main():
    parser = argparse.ArgumentParser(description=__doc__,
                                     formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("corpus", help="directory with captured .envelope / .multipart.bin files")
    parser.add_argument("--dsn", required=True, help="target DSN, e.g. http://key@localhost:9000/1")
    parser.add_argument("--include", default="*", help="glob filter on the file name")
    parser.add_argument("--keep-ids", action="store_true", help="replay original event ids")
    parser.add_argument("--keep-timestamps", action="store_true", help="do not shift timestamps to now")
    parser.add_argument("--dry-run", action="store_true")
    parser.add_argument("--timeout", type=float, default=30)
    args = parser.parse_args()

    dsn = Dsn(args.dsn)
    corpus = Path(args.corpus)
    files = sorted(path for path in corpus.rglob("*")
                   if path.suffix in (".envelope", ".bin") and fnmatch.fnmatch(path.name, args.include))
    if not files:
        print(f"no envelopes matching '{args.include}' under {corpus}", file=sys.stderr)
        return 1

    failures = 0
    for path in files:
        data = path.read_bytes()

        if path.name.endswith(".multipart.bin"):
            # crashpad minidump upload - replayed verbatim, only the ingest key is swapped
            meta = json.loads(path.with_name(path.name[:-len(".multipart.bin")] + ".meta.json").read_text())
            content_type = meta["headers"].get("Content-Type", "multipart/form-data")
            query = dict(parse_qsl(meta.get("query", "")))
            query["sentry_key"] = dsn.key
            url = f"{dsn.endpoint('minidump')}?{urlencode(query)}"
        else:
            try:
                data = rewrite(data, dsn, not args.keep_ids, not args.keep_timestamps)
            except Exception as error:
                print(f"SKIP  {path.name}: cannot rewrite ({error})", file=sys.stderr)
                failures += 1
                continue
            content_type = "application/x-sentry-envelope"
            url = dsn.endpoint("envelope")

        if args.dry_run:
            print(f"DRY   {path.name} -> {url} ({len(data)} bytes)")
            continue

        status, body = post(url, data, content_type, dsn, args.timeout)
        ok = status is not None and 200 <= status < 300
        failures += 0 if ok else 1
        print(f"{'OK ' if ok else 'FAIL'}  {status if status else 'ERR'}  {path.name}  {body.strip()[:120]}")

    print(f"\n{len(files) - failures}/{len(files)} replayed to {dsn.base}")
    return 1 if failures else 0


if __name__ == "__main__":
    sys.exit(main())
