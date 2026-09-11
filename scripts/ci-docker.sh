#!/usr/bin/env bash
set -euo pipefail

unityPrefix="$1"
unityVersion=$(jq -r --arg p "$unityPrefix" '.[$p].version' ./scripts/unity-versions.json)
imageVariant=$(echo "$2" | tr '[:upper:]' '[:lower:]')
licenseConfig=$3

container="unity"
image="unityci/editor:ubuntu-$unityVersion-$imageVariant-3"
cwd="${GITHUB_WORKSPACE:-$(pwd)}"
user="gh"
uid=$(id -u)
gid=$(id -g)

# Local fallbacks for testing
ANDROID_HOME=${ANDROID_HOME:-/android-home-missing}
JAVA_HOME_11_X64=${JAVA_HOME_11_X64:-/java-home-missing}

if [[ $(docker ps --filter "name=^/$container$" --format '{{.Names}}') == "$container" ]]; then
    echo "Removing existing container '$container'"
    docker stop $container
    docker rm $container
fi

echo "Starting up '$image' as '$container'"
suexec="docker exec --user root"

# Format: <job-name>-<image-variant>-<run-id>
uniqueHostname="${GITHUB_JOB:-local}-${imageVariant}-${GITHUB_RUN_ID:-0}"
# Sanitize hostname: replace underscores and spaces with hyphens, ensure lowercase
uniqueHostname=$(echo "$uniqueHostname" | tr '[:upper:]_ ' '[:lower:]--' | tr -s '-')

# Capture mode (see docs/envelope-capture.md): sentry-cli runs inside this container but the capture
# server runs on the host, so the container shares the host network to reach it. `--hostname` and
# `--network host` are mutually exclusive, hence the either/or. Port matches capture-corpus.ps1.
if [ -n "${SENTRY_CAPTURE_PATH:-}" ]; then
    networkArgs=(--network host -e SENTRY_URL="http://127.0.0.1:8787" -e SENTRY_CAPTURE_PATH="${SENTRY_CAPTURE_PATH}")
else
    networkArgs=(--hostname "$uniqueHostname")
fi

# We use the host dotnet installation - it's much faster than installing inside the docker container.
set -x
docker run -td --name $container \
    "${networkArgs[@]}" \
    --user $uid:$gid \
    -v "$cwd":/sentry-unity \
    -v $ANDROID_HOME:$ANDROID_HOME \
    -v $JAVA_HOME_11_X64:$JAVA_HOME_11_X64 \
    -v /usr/share/dotnet:/usr/share/dotnet \
    -v /opt/microsoft/powershell/7:/opt/microsoft/powershell/7 \
    -e UNITY_VERSION=$unityVersion \
    -e GITHUB_ACTIONS="${GITHUB_ACTIONS}" \
    -e SENTRY_AUTH_TOKEN="${SENTRY_AUTH_TOKEN:-}" \
    --workdir /sentry-unity $image

# Generate unique machine-id to avoid any hardcoded values and license-fetch congestion
$suexec $container rm -f /etc/machine-id
$suexec $container dbus-uuidgen --ensure=/etc/machine-id

$suexec $container groupadd -g $gid $user
$suexec $container useradd -u $uid -g $gid --create-home $user

$suexec $container ln -s /usr/share/dotnet/dotnet /usr/bin/dotnet
$suexec $container ln -s /opt/microsoft/powershell/7/pwsh /usr/bin/pwsh

$suexec $container mkdir -p /usr/share/unity3d/config/
echo $licenseConfig | $suexec -i $container sh -c "cat > /usr/share/unity3d/config/services-config.json"
$suexec $container chown -R $uid /usr/share/unity3d/config/

# Unity 2021+ tries to write to this directory during asset import...
$suexec $container chmod -R 755 /opt/unity/Editor/Data/UnityReferenceAssemblies/

echo "Container started successfully: "
docker ps --filter "name=^/$container$"
