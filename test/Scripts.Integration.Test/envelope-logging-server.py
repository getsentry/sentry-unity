#!/usr/bin/env python3

from http import HTTPStatus
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from urllib.parse import urlparse
import sys
import threading
import gzip
import os
import uuid

DEBUG = False

if len(sys.argv) < 2:
    print("Usage: envelope-logging-server.py <url> [envelope_dir]")
    sys.exit(1)

ENVELOPE_DIR = sys.argv[2] if len(sys.argv) > 2 else os.path.join(os.path.dirname(os.path.realpath(__file__)), "envelopes")
os.makedirs(ENVELOPE_DIR, exist_ok=True)
print(f"Storing envelopes in: {ENVELOPE_DIR}")

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

    def save_envelope(self, content):
        try:
            if content.startswith(b'\x1f\x8b'):
                try:
                    content = gzip.decompress(content)
                except Exception as e:
                    print(f"Failed to decompress gzip content: {e}")
                    return None
            
            # We have to split the envelope into items because there's potentially a binary in there that we want to skip.
            envelope_items = content.split(b'\n')
            text_parts = []

            try:
                header = envelope_items[0].decode('utf-8')
                text_parts.append(header)
            except UnicodeDecodeError:
                print(f"Warning: Envelope header could not be decoded as UTF-8")
                text_parts.append('{"error": "Could not decode header"}')
            
            i = 1
            while i < len(envelope_items):
                if not envelope_items[i].strip():
                    i += 1
                    continue
                
                try:
                    item_header = envelope_items[i].decode('utf-8')
                    text_parts.append(item_header)
                    i += 1
                    
                    if i < len(envelope_items):
                        try:
                            payload = envelope_items[i].decode('utf-8')
                            text_parts.append(payload)
                        except UnicodeDecodeError:
                            # Add a placeholder for binary data
                            text_parts.append('{"binary_data": true, "size": ' + str(len(envelope_items[i])) + '}')
                        i += 1
                except UnicodeDecodeError:
                    print(f"Warning: Item header at position {i} could not be decoded as UTF-8")
                    i += 1
            
            text_content = '\n'.join(text_parts)
            
            envelope_path = os.path.join(ENVELOPE_DIR, f"envelope_{str(uuid.uuid4())}.json")
            with open(envelope_path, 'w', encoding='utf-8') as f:
                f.write(text_content)
                    
            print(f"Envelope saved to {envelope_path}")
            return True
                
        except Exception as e:
            print(f"Error saving envelope: {e}")
            return None

    def log_request(self, code='-', size='-'):
        if isinstance(code, HTTPStatus):
            code = code.value
        
        if self.command == "POST" and 'Content-Length' in self.headers:
            content_length = int(self.headers['Content-Length'])
            content = self.rfile.read(content_length)
            
            self.log_message('"%s" %s %s', self.requestline, str(code), str(size))
            
            if '/envelope/' in self.path:
                self.save_envelope(content)
            else:
                self.log_message('Received request: %s', self.requestline)
        else:
            self.log_message('"%s" %s %s', self.requestline, str(code), str(size))


uri = urlparse(sys.argv[1] if len(sys.argv) > 1 else 'http://127.0.0.1:8000')
print("HTTP server listening on {}".format(uri.geturl()))
print("To stop the server, execute a GET request to {}/STOP".format(uri.geturl()))
httpd = ThreadingHTTPServer((uri.hostname, uri.port), Handler)
target = httpd.serve_forever()
