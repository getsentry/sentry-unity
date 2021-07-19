import os
from sys import stdout
import subprocess
import urllib.request

print("Downloading the hub installer.")
urllib.request.urlretrieve("https://public-cdn.cloud.unity3d.com/hub/prod/UnityHubSetup.exe", "hubinstaller.exe")
print("Installer downloaded.")

print("Installing the installer")
instalprocess = subprocess.Popen(["hubinstaller.exe", "/S"], shell=True, stdout=subprocess.PIPE)
instalprocess.wait()
print("Installer installed.")

hubpath = r'C:\\Program Files\\Unity Hub\\Unity Hub.exe'

# print("Checking out the installer")
# process = subprocess.Popen([hubpath, "--", "--headless",  "help"], stdout=subprocess.PIPE)
# while True:
# 	output = process.stdout.readline().decode()
# 	if output == '' and process.poll() is not None:
# 		break
# 	if output:
# 		print(output, end =" ") # , end =" " so there are no double newlines


print("Installing Unity")
process = subprocess.Popen([hubpath, "--", "--headless",  "install", "--changeset", "b76dac84db26", "-m", "android", "-m", "android-sdk-ndk-tools"], stdout=subprocess.PIPE)

while True:
	output = process.stdout.readline().decode()
	if output == '' and process.poll() is not None:
		break
	if output:
		print(output, end =" ") # , end =" " so there are no double newlines 

rc = process.poll()
print(rc)