#!/bin/bash

cocoaRoot=$1
frameworkDestination=$2

cd $cocoaRoot
carthage build --use-xcframeworks --no-skip-current --platform iOS

mkdir -p "$frameworkDestination"
cp -r Carthage/Build/Sentry.xcframework/ios-arm64_armv7/Sentry.framework "$frameworkDestination"
