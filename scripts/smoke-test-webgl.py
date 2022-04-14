#!/usr/bin/env python3

# Testing approach:
#  1. Start a web=server for pre-built WebGL app directory (index.html & co) and to collect the API requests
#  3. Run the smoke test using chromedriver
#  4. Check the messages received by the API server

import binascii
import datetime
import logging
import re
import time
import os
from http import HTTPStatus
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer
from threading import Thread
from selenium import webdriver
from selenium.webdriver.chrome.options import Options
from selenium.webdriver.common.desired_capabilities import DesiredCapabilities

host = '127.0.0.1'
port = 8000
scriptDir = os.path.dirname(os.path.abspath(__file__))
appDir = os.path.join(scriptDir, '..', 'samples',
                      'artifacts', 'builds', 'WebGL')


class RequestVerifier:
    __requests = []
    __testNumber = 0

    def Capture(self, info, body):
        if re.match('"exception":{"values":[{"type":"The resource [^ ]+ could not be loaded from the resource file!"', body):
            print(
                "TEST: Skipping the received HTTP Request because it's an unrelated unity bug:\n{}".format(body))

        print("TEST: Received HTTP Request #{} = {}\n{}".format(
            len(self.__requests), info, body), flush=True)
        self.__requests.append({"request": info, "body": body})

    def Expect(self, message, result):
        self.__testNumber += 1
        info = "TEST | #{}. {}: {}".format(self.__testNumber,
                                           message, "PASS" if result else "FAIL")
        if result:
            print(info, flush=True)
        else:
            raise Exception(info)

    def ExpectMessage(self, index, substring):
        message = self.__requests[index]["body"]
        self.Expect("HTTP Request #{} contains \"{}\".".format(index, substring),
                    substring in message or substring.replace("'", "\"") in message)


t = RequestVerifier()


class Handler(SimpleHTTPRequestHandler):
    def __init__(self, *args, **kwargs):
        super().__init__(*args, directory=appDir, **kwargs)

    def do_POST(self):
        body = ""
        content = self.rfile.read(int(self.headers['Content-Length']))
        try:
            body = content.decode("utf-8")
        except:
            logging.exception("Exception while parsing an API request")
            body = binascii.hexlify(bytearray(content))
        t.Capture(self.requestline, body)
        self.send_response(HTTPStatus.OK, '{'+'}')
        self.end_headers()


appServer = ThreadingHTTPServer((host, port), Handler)
appServerThread = Thread(target=appServer.serve_forever)
appServerThread.start()


class TestDriver:
    def __init__(self):
        options = Options()
        options.add_experimental_option('excludeSwitches', ['enable-logging'])
        options.add_argument('--headless')
        d = DesiredCapabilities.CHROME
        d['goog:loggingPrefs'] = {'browser': 'ALL'}
        self.driver = webdriver.Chrome(
            options=options, desired_capabilities=d)
        self.driver.get('http://{}:{}?test=smoke'.format(host, port))
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
                entry['timestamp']/1000).strftime('%H:%M:%S.%f'), entry['message']), flush=True)


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


# Verify received API requests - see SmokeTester.cs - this is a copy-paste with minimal syntax changes
currentMessage = 0
t.ExpectMessage(currentMessage, "'type':'session'")
currentMessage += 1
t.ExpectMessage(currentMessage, "'type':'event'")
t.ExpectMessage(currentMessage, "LogError(GUID)")
currentMessage += 1
t.ExpectMessage(currentMessage, "'type':'event'")
t.ExpectMessage(currentMessage, "CaptureMessage(GUID)")
currentMessage += 1
t.ExpectMessage(currentMessage, "'type':'event'")
t.ExpectMessage(
    currentMessage, "'message':'crumb','type':'error','data':{'foo':'bar'},'category':'bread','level':'critical'}")
t.ExpectMessage(currentMessage, "'message':'scope-crumb'}")
t.ExpectMessage(currentMessage, "'extra':{'extra-key':42}")
t.ExpectMessage(currentMessage, "'tags':{'tag-key':'tag-value'")
t.ExpectMessage(
    currentMessage, "'user':{'email':'email@example.com','id':'user-id','ip_address':'::1','username':'username','other':{'role':'admin'}}")
print('TEST: PASS', flush=True)
