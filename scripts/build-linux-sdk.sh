#!/bin/bash
set -e

if [ "$#" -ne 2 ]; then
    echo "Usage: $0 <path/to/sentry-native> <path/to/artifacts>"
    exit 1
fi

sentry_native_root=$(realpath "$1")
sentry_native_build="$sentry_native_root/build"
sentry_linux_artifacts_destination=$(realpath "$2")

cmake -B "$sentry_native_build" -D SENTRY_BACKEND=breakpad -D SENTRY_SDK_NAME=sentry.native.unity -D CMAKE_BUILD_TYPE=RelWithDebInfo -S "$sentry_native_root"
cmake --build "$sentry_native_build" --target sentry --parallel

mkdir -p "$sentry_linux_artifacts_destination"

# strip all, including exported symbols except those starting with 'sentry_', except for 'sentry__'
strip -s "$sentry_native_build/libsentry.so" -w -K sentry_[^_]* -o "$sentry_linux_artifacts_destination/libsentry.so"
cp "$sentry_native_build/libsentry.so" "$sentry_linux_artifacts_destination/libsentry.dbg.so"
objcopy --add-gnu-debuglink="$sentry_linux_artifacts_destination/libsentry.dbg.so" "$sentry_linux_artifacts_destination/libsentry.so"
