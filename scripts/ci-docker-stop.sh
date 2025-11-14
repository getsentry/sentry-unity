#!/usr/bin/env bash
set -uo pipefail

container="unity"

if [[ $(docker ps --filter "name=^/$container$" --format '{{.Names}}') == "$container" ]]; then
    echo "Stopping container '$container' and waiting for shutdown..."
    
    # docker stop sends SIGTERM to all processes (including Unity if running)
    # and waits for graceful shutdown. If Unity is running, it will return the license.
    # 300 second timeout should be plenty for Unity to shut down and return license.
    docker stop -t 300 $container

    echo "Removing container '$container'..."
    docker rm $container

    echo "Cleanup completed successfully"
else
    echo "Container '$container' is not running, nothing to clean up"
fi
