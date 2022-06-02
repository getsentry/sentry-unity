#!/usr/bin/env python3

from http import HTTPStatus
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from urllib.parse import urlparse
import sys
import threading
import binascii


class Handler(BaseHTTPRequestHandler):
    def commonServe(self):
        self.send_response(200, "")
        self.end_headers()
        sys.stdout.flush()
        sys.stderr.flush()

        if (self.path == "/STOP"):
            print("HTTP server stopping!")
            threading.Thread(target=self.server.shutdown).start()

    def do_GET(self):
        self.commonServe()

    def do_POST(self):
        self.commonServe()

    # override
    def log_request(self, code='-', size='-'):
        if isinstance(code, HTTPStatus):
            code = code.value
        body = ""
        if self.command == "POST" and 'Content-Length' in self.headers:
            content_length = int(self.headers['Content-Length'])
            content = self.rfile.read(content_length)
            try:
                body = content.decode("utf-8")
            except:
                body = binascii.hexlify(bytearray(content))
            body = body[0:min(1000, len(body))]
        self.log_message('"%s" %s %s%s',
                         self.requestline, str(code), str(size), body)


uri = urlparse(sys.argv[1] if len(sys.argv) > 1 else 'http://127.0.0.1:8000')
print("HTTP server listening on {}".format(uri.geturl()))
print("To stop the server, execute a GET request to {}/STOP".format(uri.geturl()))
httpd = ThreadingHTTPServer((uri.hostname, uri.port), Handler)
target = httpd.serve_forever()
