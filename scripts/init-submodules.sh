#!/bin/bash
set -euo pipefail

git submodule init "$@"
git submodule update --recursive
git submodule foreach git submodule update --init --recursive