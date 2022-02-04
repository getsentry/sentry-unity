#!/bin/bash
set -euo pipefail

git submodule init "$@"
git submodule update --recursive