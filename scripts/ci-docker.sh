#!/usr/bin/env bash
set -euo pipefail

unityVersion=$(pwsh ./scripts/ci-env.ps1 "unity$1")
imageVariant=$(echo "$2" | tr '[:upper:]' '[:lower:]')
licenseConfig=$3

container="unity"
image="unityci/editor:ubuntu-$unityVersion-$imageVariant-1.0.1"
cwd="${GITHUB_WORKSPACE:-$(pwd)}"
user="gh"
uid=$(id -u)
gid=0 # same as root so we have access to the whole of the unity installation

if [[ $(docker ps --filter "name=^/$container$" --format '{{.Names}}') == "$container" ]]; then
    echo "Removing existing container '$container'"
    docker stop $container
    docker rm $container
fi

echo "Starting up '$image' as '$container'"

# We use the host dotnet installation - it's much faster than installing inside the docker container.
set -x
docker run -td --name $container \
    --user $uid:$gid \
    -v "$cwd":/sentry-unity \
    -v /usr/share/dotnet:/usr/share/dotnet \
    -v /opt/microsoft/powershell/7:/opt/microsoft/powershell/7 \
    --workdir /sentry-unity $image
set +x

# -v $ANDROID_HOME:$ANDROID_HOME \
# -v $JAVA_HOME_11_X64:$JAVA_HOME_11_X64 \

suexec="docker exec --user root"

$suexec $container useradd -u $uid -g $gid --create-home $user

$suexec $container ln -s /usr/share/dotnet/dotnet /usr/bin/dotnet
$suexec $container ln -s /opt/microsoft/powershell/7/pwsh /usr/bin/pwsh

$suexec $container mkdir -p /usr/share/unity3d/config/
echo $licenseConfig | $suexec -i $container sh -c "cat > /usr/share/unity3d/config/services-config.json"
$suexec $container chown -R $(id -u) /usr/share/unity3d/config/

echo "Container started successfully: "
docker ps --filter "name=^/$container$"
