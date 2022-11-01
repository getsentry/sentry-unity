#!/usr/bin/env python3

from http import HTTPStatus
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from urllib.parse import urlparse
from threading import Thread, Lock
import sys
import threading
import binascii
import json

apiOrg = 'sentry-sdks'
apiProject = 'sentry-unity'
uri = urlparse(sys.argv[1] if len(sys.argv) > 1 else 'http://127.0.0.1:8000')
uploads = {}
lock = Lock()


def registerUpload(name: str, chunks: int):
    lock.acquire()
    try:
        if name not in uploads:
            uploads[name] = \
                {'count': 1, 'chunks': chunks}
        else:
            uploads[name]['count'] += 1
            uploads[name]['chunks'] += chunks
    finally:
        lock.release()


class Handler(BaseHTTPRequestHandler):
    body = None

    def do_GET(self):
        self.start_response(HTTPStatus.OK)

        if self.path == "/STOP":
            print("HTTP server stopping!")
            threading.Thread(target=self.server.shutdown).start()

        if self.isApi('api/0/organizations/{}/chunk-upload/'.format(apiOrg)):
            self.writeJSON('{"url":"' + uri.geturl() + self.path + '",'
                           '"chunkSize":8388608,"chunksPerRequest":64,"maxFileSize":2147483648,'
                           '"maxRequestSize":33554432,"concurrency":1,"hashAlgorithm":"sha1","compression":["gzip"],'
                           '"accept":["debug_files","release_files","pdbs","sources","bcsymbolmaps"]}')
        else:
            self.end_headers()

        self.flushLogs()

    def do_POST(self):
        self.start_response(HTTPStatus.OK)

        if self.isApi('api/0/projects/{}/{}/files/difs/assemble/'.format(apiOrg, apiProject)):
            # Request body example:
            # {
            #   "9a01653a...":{"name":"UnityPlayer.dylib","debug_id":"eb4a7644-...","chunks":["f84d3907945cdf41b33da8245747f4d05e6ffcb4", ...]},
            #   "4185e454...":{"name":"UnityPlayer.dylib","debug_id":"86d95b40-...","chunks":[...]}
            # }
            # Response body to let the CLI know we have the symbols already (we don't need to test the actual upload):
            # {
            #   "9a01653a...":{"state":"ok","missingChunks":[]},
            #   "4185e454...":{"state":"ok","missingChunks":[]}
            # }
            jsonRequest = json.loads(self.body)
            jsonResponse = '{'
            for key, value in jsonRequest.items():
                jsonResponse += '"{}"'.format(key)
                jsonResponse += ':{"state":"ok","missingChunks":[]},'
                registerUpload(value['name'], len(value['chunks']))
            jsonResponse = jsonResponse.rstrip(',') + '}'
            self.writeJSON(jsonResponse)
        else:
            self.end_headers()

        self.flushLogs()

    def start_response(self, code):
        self.body = None
        self.log_request(code)
        self.send_response_only(code)

    def log_request(self, code=None, size=None):
        if isinstance(code, HTTPStatus):
            code = code.value
        body = self.body = self.requestBody()
        if body:
            body = self.body[0:min(1000, len(body))]
        self.log_message('"%s" %s %s%s',
                         self.requestline, str(code), "({} bytes)".format(size) if size else '', body)

    # Note: this may only be called once during a single request - can't `.read()` the same stream again.
    def requestBody(self):
        if self.command == "POST" and 'Content-Length' in self.headers:
            length = int(self.headers['Content-Length'])
            content = self.rfile.read(length)
            try:
                return content.decode("utf-8")
            except:
                return binascii.hexlify(bytearray(content))
        return None

    def isApi(self, api: str):
        if self.path.strip('/') == api.strip('/'):
            self.log_message("Matched API endpoint {}".format(api))
            return True
        return False

    def writeJSON(self, string: str):
        self.send_header("Content-type", "application/json")
        self.end_headers()
        self.wfile.write(str.encode(string))

    def flushLogs(self):
        sys.stdout.flush()
        sys.stderr.flush()


print("HTTP server listening on {}".format(uri.geturl()))
print("To stop the server, execute a GET request to {}/STOP".format(uri.geturl()))

try:
    httpd = ThreadingHTTPServer((uri.hostname, uri.port), Handler)
    target = httpd.serve_forever()
except KeyboardInterrupt:
    pass
finally:
    print('Upload stats:')
    for k in sorted(uploads):
        v = uploads[k]
        print('  {}: count={} chunks={}'.format(k, v['count'], v['chunks']))
