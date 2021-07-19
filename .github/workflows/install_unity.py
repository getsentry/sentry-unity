import os
from sys import stdout
import subprocess
import urllib.request

urllib.request.urlretrieve("https://public-cdn.cloud.unity3d.com/hub/prod/UnityHubSetup.exe", "hubinstaller.exe")
subprocess.Popen(["hubinstaller.exe", "/S"], shell=True, stdout=subprocess.PIPE)

hubpath = r'C:\\Program Files\\Unity Hub\\Unity Hub.exe'
process = subprocess.Popen([hubpath, "--", "--headless",  "install", "--version", "2019.4.28f1", "-m", "android", "-m", "android-sdk-ndk-tools"], stdout=subprocess.PIPE)

while True:
	output = process.stdout.readline().decode()
	if output == '' and process.poll() is not None:
		break
	if output:
		print(output, end =" ") # , end =" " so there are no double newlines 

rc = process.poll()
print(rc)