#!/usr/bin/env bash
set -euo pipefail

if [[ $# -ne 2 ]]; then
    echo "Usage: $0 <UnityVersion> <UnityChangeset>"
    exit 1
fi

version=$1
changeSet=$2

# see https://github.com/game-ci/docker/tags
unityCiRepoVersion=0.17.0
os=ubuntu

wget -O Dockerfile https://raw.githubusercontent.com/game-ci/docker/v$unityCiRepoVersion/images/ubuntu/editor/Dockerfile
cat Dockerfile.append >> Dockerfile

docker build \
    --build-arg hubImage=unityci/hub:$os-$unityCiRepoVersion \
    --build-arg baseImage=unityci/base:$os-$unityCiRepoVersion \
    --build-arg version=$version \
    --build-arg changeSet=$changeSet \
    --build-arg module="ios android" \
    .
