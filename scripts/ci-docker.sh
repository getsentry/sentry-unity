#!/usr/bin/env bash
set -euo pipefail

# unityVersion=$(pwsh ./scripts/ci-env.ps1 "unity$1")
unityVersion="2019.4.40f1"
imageVariant=$(echo "$2" | tr '[:upper:]' '[:lower:]')
licenseConfig=$3

# 1. We use the host dotnet installation - it's much faster than installing inside the docker container.
# 2. We must use the iOS version of the image instead of 'base' - Sentry.Unity.Editor.iOS.csproj requires some libraries.
#    Maybe we could just cache the needed file instead of pulling the 1 GB larger image on every build...

container="unity"
image="unityci/editor:ubuntu-$unityVersion-$imageVariant-1.0.1"
user="gh"
uid=$(id -u)
gid=$(id -g)

if [[ $(docker ps --filter "name=^/$container$" --format '{{.Names}}') == "$container" ]]; then
    echo "Removing existing container '$container'"
    docker stop $container
    docker rm $container
fi

echo "Starting up '$image' as '$container'"

docker run -td --name $container \
    --user $uid:$gid \
    -v $GITHUB_WORKSPACE:/sentry-unity \
    -v /usr/share/dotnet:/usr/share/dotnet \
    -v /opt/microsoft/powershell/7:/opt/microsoft/powershell/7 \
    --workdir /sentry-unity $image

# -v $ANDROID_HOME:$ANDROID_HOME \
# -v $JAVA_HOME_11_X64:$JAVA_HOME_11_X64 \

suexec="docker exec --user root"

$suexec $container groupadd -g $gid $user
$suexec $container useradd -u $uid -g $gid --create-home $user

$suexec $container ln -s /usr/share/dotnet/dotnet /usr/bin/dotnet
$suexec $container ln -s /opt/microsoft/powershell/7/pwsh /usr/bin/pwsh

$suexec $container mkdir -p /usr/share/unity3d/config/
echo $licenseConfig | $suexec -i $container sh -c "cat > /usr/share/unity3d/config/services-config.json"
$suexec $container chown -R $(id -u) /usr/share/unity3d/config/

echo "Container started successfully: "
docker ps --filter "name=^/$container$"
