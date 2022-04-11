#!/usr/bin/env python3

import time
import datetime
import os
from http.server import BaseHTTPRequestHandler, SimpleHTTPRequestHandler, ThreadingHTTPServer
from threading import Thread
from typing import final
from selenium import webdriver
from selenium.webdriver.chrome.options import Options
from selenium.webdriver.common.desired_capabilities import DesiredCapabilities

# Testing approach:
#  1. Start an API server
#  2. Start a server that serves the pre-built WebGL app directory (index.html & co)
#  3. Wait for the smoke test to complete
#  4. Check the messages received on the API server

host = '127.0.0.1'
htmlPort = 8080
apiPort = 8000
scriptDir = os.path.dirname(os.path.abspath(__file__))
appDir = os.path.join(scriptDir, '..', 'samples',
                      'artifacts', 'builds', 'WebGL')
apiRequests = []


class AppDirHandler(SimpleHTTPRequestHandler):
    def __init__(self, *args, **kwargs):
        super().__init__(*args, directory=appDir, **kwargs)


appServer = ThreadingHTTPServer((host, htmlPort), AppDirHandler)
appServerThread = Thread(target=appServer.serve_forever)
appServerThread.start()


class ApiHandler(BaseHTTPRequestHandler):
    def do_GET(self):
        apiRequests.add(self.requestline)

    def do_POST(self):
        apiRequests.add(self.requestline)


apiServer = ThreadingHTTPServer((host, apiPort), ApiHandler)
apiServerThread = Thread(target=apiServer.serve_forever)
apiServerThread.start()


class TestDriver:
    def __init__(self):
        options = Options()
        options.add_experimental_option('excludeSwitches', ['enable-logging'])
        d = DesiredCapabilities.CHROME
        d['goog:loggingPrefs'] = {'browser': 'ALL'}
        self.driver = webdriver.Chrome(options=options, desired_capabilities=d)
        self.driver.get('http://{}:{}?test=smoke'.format(host, htmlPort))
        self.messages = []

    def fetchMessages(self):
        for entry in self.driver.get_log('browser'):
            m = entry['message']
            entry['message'] = m[m.find('"'):].replace('\\n', '').strip('" ')
            self.messages.append(entry)

    def hasMessage(self, message):
        self.fetchMessages()
        return any(message in entry['message'] for entry in self.messages)

    def dumpMessages(self):
        self.fetchMessages()
        for entry in self.messages:
            print("CHROME: {} {}".format(datetime.datetime.fromtimestamp(
                entry['timestamp']/1000).strftime('%H:%M:%S.%f'), entry['message']))


def waitUntil(condition, interval=0.1, timeout=1):
    start = time.time()
    while not condition():
        if time.time() - start >= timeout:
            raise Exception('Waiting timed out'.format(condition))
        time.sleep(interval)


driver = TestDriver()
try:
    waitUntil(lambda: driver.hasMessage('SMOKE TEST: PASS'), timeout=10)
finally:
    driver.dumpMessages()
    driver.driver.quit()
    appServer.shutdown()
    apiServer.shutdown()
    print('API requests: {}'.format(apiRequests))
