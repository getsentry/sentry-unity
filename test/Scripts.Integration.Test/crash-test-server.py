#!/usr/bin/env python3

from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
import sys
import threading


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


host = '127.0.0.1'
port = 8000
uri = 'http://{}:{}'.format(host, port)
print("HTTP server listening on {}".format(uri))
print("To stop the server, execute a GET request to {}/STOP".format(uri))
httpd = ThreadingHTTPServer((host, port), Handler)
target = httpd.serve_forever()
