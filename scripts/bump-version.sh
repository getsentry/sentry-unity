#!/bin/bash

set -eux
# craft executes this file by convension.

# Requires powershell: `brew install powershell`
pwsh ./scripts/bump-version.ps1 $1