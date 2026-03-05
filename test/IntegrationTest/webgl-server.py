#!/usr/bin/env python3
"""
HTTP server for WebGL integration tests.

Serves a Unity WebGL build with proper Brotli Content-Encoding headers,
launches headless Chrome to run the test, captures browser console output,
and waits for the INTEGRATION_TEST_COMPLETE signal.

Usage:
    python3 webgl-server.py <build-dir> <test-action> [timeout-seconds]

Prints captured browser console lines to stdout (one per line).
Exit code 0 if completion signal seen, 1 on timeout or error.
"""

import json
import os
import sys
import time
from http import HTTPStatus
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer
from threading import Thread

from selenium import webdriver
from selenium.webdriver.chrome.options import Options

HOST = "127.0.0.1"
PORT = 8000


def create_handler(app_dir):
    class Handler(SimpleHTTPRequestHandler):
        def __init__(self, *args, **kwargs):
            super().__init__(*args, directory=app_dir, **kwargs)

        def do_POST(self):
            # Accept POST requests (Sentry envelope endpoint) and respond OK
            content_length = int(self.headers.get("Content-Length", 0))
            self.rfile.read(content_length)
            self.send_response(HTTPStatus.OK)
            self.end_headers()

        def send_head(self):
            path = self.translate_path(self.path)
            if path.endswith(".br"):
                try:
                    f = open(path, "rb")
                except OSError:
                    self.send_error(HTTPStatus.NOT_FOUND, "File not found")
                    return None
                ctype = self.guess_type(path[:-3])
                try:
                    fs = os.fstat(f.fileno())
                    self.send_response(HTTPStatus.OK)
                    self.send_header("Content-Encoding", "br")
                    self.send_header("Content-type", ctype)
                    self.send_header("Content-Length", str(fs[6]))
                    self.send_header(
                        "Last-Modified", self.date_time_string(fs.st_mtime)
                    )
                    self.end_headers()
                    return f
                except Exception:
                    f.close()
                    raise
            return super().send_head()

        def log_message(self, format, *args):
            # Suppress request logging to keep output clean
            pass

    return Handler


def parse_console_message(raw_msg):
    """Extract the actual message from a Chrome console log entry.

    Chrome formats console messages as: 'URL LINE:COL "actual message"'
    """
    quote_start = raw_msg.find('"')
    if quote_start >= 0:
        raw_msg = raw_msg[quote_start:].strip('" ')
    return raw_msg.replace("\\n", "\n")


def run_test(app_dir, test_action, timeout_seconds):
    # Start HTTP server
    handler_class = create_handler(app_dir)
    server = ThreadingHTTPServer((HOST, PORT), handler_class)
    server_thread = Thread(target=server.serve_forever, daemon=True)
    server_thread.start()

    # Small delay for server startup
    time.sleep(0.5)

    # Launch headless Chrome
    options = Options()
    options.add_experimental_option("excludeSwitches", ["enable-logging"])
    options.add_argument("--headless")
    options.add_argument("--no-sandbox")
    options.add_argument("--disable-dev-shm-usage")
    options.set_capability("goog:loggingPrefs", {"browser": "ALL"})

    driver = webdriver.Chrome(options=options)
    url = f"http://{HOST}:{PORT}?test={test_action}"
    driver.get(url)

    collected_lines = []
    complete = False
    start_time = time.time()

    try:
        while time.time() - start_time < timeout_seconds:
            for entry in driver.get_log("browser"):
                msg = parse_console_message(entry["message"])
                collected_lines.append(msg)

                if "INTEGRATION_TEST_COMPLETE" in msg:
                    complete = True

            if complete:
                # Give a brief moment for any final console messages
                time.sleep(1)
                for entry in driver.get_log("browser"):
                    collected_lines.append(parse_console_message(entry["message"]))
                break

            time.sleep(0.5)
    finally:
        driver.quit()
        server.shutdown()

    # Output collected lines as JSON array for easy parsing by PowerShell
    print(json.dumps(collected_lines))

    return 0 if complete else 1


if __name__ == "__main__":
    if len(sys.argv) < 3:
        print(f"Usage: {sys.argv[0]} <build-dir> <test-action> [timeout-seconds]", file=sys.stderr)
        sys.exit(2)

    app_dir = sys.argv[1]
    test_action = sys.argv[2]
    timeout = int(sys.argv[3]) if len(sys.argv) > 3 else 60

    sys.exit(run_test(app_dir, test_action, timeout))
