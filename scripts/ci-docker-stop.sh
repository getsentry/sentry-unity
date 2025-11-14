#!/usr/bin/env bash
set -uo pipefail

container="unity"

if [[ $(docker ps --filter "name=^/$container$" --format '{{.Names}}') == "$container" ]]; then
    echo "Checking for running processes in container..."
    docker exec $container ps aux || true

    echo "Stopping container '$container' and waiting for shutdown..."
    start_time=$(date +%s)

    # docker stop sends SIGTERM to all processes (including Unity if running)
    # and waits for graceful shutdown. If Unity is running, it will return the license.
    # 180 second (3 minute) timeout for Unity to finish any in-progress operations,
    # write files, and return license. If nothing is running, bash exits immediately.
    docker stop -t 180 $container

    end_time=$(date +%s)
    elapsed=$((end_time - start_time))
    echo "Container stopped after $elapsed seconds"

    echo "Removing container '$container'..."
    docker rm $container

    echo "Cleanup completed successfully"
else
    echo "Container '$container' is not running, nothing to clean up"
fi
