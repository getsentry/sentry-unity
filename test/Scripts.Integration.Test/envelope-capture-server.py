#!/usr/bin/env python3
"""Captures raw Sentry envelopes sent by the integration test app.

Stands in for Sentry's ingest endpoint: accepts every request, writes the body to disk
and answers 200 so the SDK considers the payload delivered. Point the DSN of the test
build at this server (host 127.0.0.1) and the run produces a corpus of real envelopes -
including native crash envelopes and crashpad minidump uploads - that can be replayed
against a local Sentry via scripts/replay-envelopes.py.

Usage:
    envelope-capture-server.py --output DIR [--host 0.0.0.0] [--port 8000] [--platform NAME]

Control endpoints:
    GET /HEALTH          200 once the server is serving
    GET /MARK?label=foo  tags subsequently captured files with `foo` (the test action)
    GET /STOP            shuts the server down
"""

import argparse
import gzip
import json
import re
import sys
import threading
import uuid
import zlib
from datetime import datetime, timezone
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
from urllib.parse import parse_qs, urlparse

state_lock = threading.Lock()
sequence = 0
label = "startup"
output_dir = Path(".")
platform_name = "unknown"


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


def decode_body(body, encoding):
    if not encoding:
        return body
    encoding = encoding.lower()
    try:
        if encoding == "gzip":
            return gzip.decompress(body)
        if encoding in ("deflate", "zlib"):
            return zlib.decompress(body)
    except Exception as error:
        print(f"failed to decompress {encoding} body: {error}", file=sys.stderr)
    return body


def safe(value):
    return re.sub(r"[^A-Za-z0-9_.-]", "_", value)[:60] or "unknown"


class Handler(BaseHTTPRequestHandler):
    protocol_version = "HTTP/1.1"

    def log_message(self, fmt, *args):
        print(f"{self.address_string()} - {fmt % args}", file=sys.stderr)

    def cors(self):
        self.send_header("Access-Control-Allow-Origin", "*")
        self.send_header("Access-Control-Allow-Methods", "POST, GET, OPTIONS")
        self.send_header("Access-Control-Allow-Headers", "*")
        self.send_header("Access-Control-Max-Age", "86400")

    def respond(self, code, payload=b"", content_type="application/json"):
        self.send_response(code)
        self.send_header("Content-Type", content_type)
        self.send_header("Content-Length", str(len(payload)))
        self.cors()
        self.end_headers()
        if payload:
            self.wfile.write(payload)

    def do_OPTIONS(self):
        self.respond(200)

    def do_GET(self):
        global label
        url = urlparse(self.path)
        if url.path == "/HEALTH":
            self.respond(200, b'{"ok":true}')
        elif url.path == "/MARK":
            new_label = parse_qs(url.query).get("label", ["unlabeled"])[0]
            with state_lock:
                label = safe(new_label)
            print(f"--- mark: {label} ---", file=sys.stderr)
            self.respond(200, b'{"ok":true}')
        elif url.path == "/STOP":
            self.respond(200, b'{"ok":true}')
            threading.Thread(target=self.server.shutdown).start()
        else:
            self.respond(200, b"{}")

    def read_body(self):
        if self.headers.get("Transfer-Encoding", "").lower() == "chunked":
            chunks = []
            while True:
                size = int(self.rfile.readline().split(b";")[0], 16)
                if size == 0:
                    self.rfile.readline()
                    break
                chunks.append(self.rfile.read(size))
                self.rfile.readline()
            return b"".join(chunks)
        return self.rfile.read(int(self.headers.get("Content-Length", 0)))

    def do_POST(self):
        global sequence
        url = urlparse(self.path)
        raw = self.read_body()
        body = decode_body(raw, self.headers.get("Content-Encoding"))

        with state_lock:
            sequence += 1
            seq, current_label = sequence, label

        meta = {
            "sequence": seq,
            "label": current_label,
            "platform": platform_name,
            "received": datetime.now(timezone.utc).isoformat(),
            "method": self.command,
            "path": url.path,
            "query": url.query,
            "headers": dict(self.headers),
            "raw_bytes": len(raw),
            "decoded_bytes": len(body),
        }

        content_type = self.headers.get("Content-Type", "")
        event_id = None
        if "multipart/form-data" in content_type:
            # crashpad uploads the minidump to /api/<project>/minidump/ as multipart
            extension = "multipart.bin"
            kind = "minidump"
        else:
            extension = "envelope"
            kind = "envelope"
            try:
                header, items = parse_envelope(body)
                meta["envelope_header"] = header
                meta["items"] = [
                    {
                        "type": item_header.get("type"),
                        "length": len(payload),
                        "filename": item_header.get("filename"),
                        "content_type": item_header.get("content_type"),
                    }
                    for item_header, payload in items
                ]
                event_id = header.get("event_id")
                types = [i.get("type") or "unknown" for i, _ in items]
                if types:
                    kind = "+".join(dict.fromkeys(types))
            except Exception as error:
                meta["parse_error"] = str(error)

        name = f"{seq:03d}-{safe(platform_name)}-{safe(current_label)}-{safe(kind)}"
        (output_dir / f"{name}.{extension}").write_bytes(body)
        (output_dir / f"{name}.meta.json").write_text(json.dumps(meta, indent=2))
        with state_lock:
            with (output_dir / "index.jsonl").open("a") as index:
                index.write(json.dumps({"file": f"{name}.{extension}", **meta}) + "\n")

        print(f"captured {name}.{extension} ({len(body)} bytes) {url.path}", file=sys.stderr)
        self.respond(200, json.dumps({"id": event_id or uuid.uuid4().hex}).encode())


def main():
    global output_dir, platform_name

    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--host", default="0.0.0.0")
    parser.add_argument("--port", type=int, default=8000)
    parser.add_argument("--output", required=True)
    parser.add_argument("--platform", default="unknown")
    args = parser.parse_args()

    output_dir = Path(args.output)
    output_dir.mkdir(parents=True, exist_ok=True)
    platform_name = args.platform

    server = ThreadingHTTPServer((args.host, args.port), Handler)
    print(f"envelope capture listening on {args.host}:{args.port} -> {output_dir}", file=sys.stderr)
    server.serve_forever()
    print(f"envelope capture stopped after {sequence} requests", file=sys.stderr)


if __name__ == "__main__":
    main()
