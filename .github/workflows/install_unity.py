import os
from sys import stdout
import subprocess
import urllib.request


def download_hub():
	print("Downloading the hub installer.")
	# urllib.request.urlretrieve("https://public-cdn.cloud.unity3d.com/hub/prod/UnityHubSetup.exe", "hubinstaller.exe")
	print("Download finished.")

def install_hub():	
	print("Installing the hub.")
	# instalprocess = subprocess.Popen(["hubinstaller.exe", "/S"], shell=True, stdout=subprocess.PIPE)
	# instalprocess.wait()
	print("Hub installer finished.")

def install_unity():
	hubpath = r'C:\\Program Files\\Unity Hub\\Unity Hub.exe'
	version = os.environ['UNITY_VERSION']
	changeset = os.environ['CHANGESET']
	
	print("Installing Unity")
	print("\tversion:\t" + version)
	print("\tchangeset:\t" + changeset)

	# process = subprocess.Popen([hubpath, "--", "--headless",  "install", "--version", version, "--changeset", changeset, "-m", "android", "-m", "android-sdk-ndk-tools"], stdout=subprocess.PIPE)
	# while True:
	# 	output = process.stdout.readline().decode()
	# 	if output == '' and process.poll() is not None:
	# 		break
	# 	if output:
	# 		print(output, end =" ") # , end =" " so there are no double newlines 

	# rc = process.poll()
	# print(rc)
	return 0


def main():
	download_hub()
	install_hub()
	install_unity()

if __name__ == "__main__":
	main()