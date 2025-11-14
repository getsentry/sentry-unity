#!/usr/bin/env bash
set -uo pipefail

container="unity"

# Check if container exists and is running
if [[ $(docker ps --filter "name=^/$container$" --format '{{.Names}}') == "$container" ]]; then
    echo "Returning Unity license..."
    # Try to return the license gracefully, but don't fail if it doesn't work
    docker exec $container unity-editor -quit -batchmode -returnlicense || echo "License return command failed, but continuing cleanup..."

    # Give Unity a moment to finish returning the license
    sleep 2

    echo "Stopping container '$container' gracefully..."
    # docker stop sends SIGTERM and waits for graceful shutdown (default 10s timeout)
    docker stop $container

    echo "Removing container '$container'..."
    docker rm $container

    echo "Cleanup completed successfully"
else
    echo "Container '$container' is not running, nothing to clean up"
fi
