#!/usr/bin/env python3

from http import HTTPStatus
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
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
            content = self.rfile.read(min(1000, content_length))
            try:
                body = content.decode("utf-8")
            except:
                body = binascii.hexlify(bytearray(content))
        self.log_message('"%s" %s %s%s',
                         self.requestline, str(code), str(size), body)


host = '127.0.0.1'
port = 8000
uri = 'http://{}:{}'.format(host, port)
print("HTTP server listening on {}".format(uri))
print("To stop the server, execute a GET request to {}/STOP".format(uri))
httpd = ThreadingHTTPServer((host, port), Handler)
target = httpd.serve_forever()
