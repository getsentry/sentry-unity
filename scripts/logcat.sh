#!/bin/bash
set -e

# 'Sentry' is the tag from the Android SDK. 'Unity' includes the C# layer in the Unity SDK:
adb logcat Unity:V Sentry:V '*:S'
