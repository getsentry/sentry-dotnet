#!/usr/bin/env bash
# Redirect stderr to stdout to avoid weird Powershell errors
exec 2>&1
set -eux

zeus upload -t "application/zip+nupkg" ./src/*/bin/Release/*.nupkg || [[ ! "$APPVEYOR_REPO_BRANCH" =~ ^release/ ]]

zeus job update --status=passed || [[ ! "$APPVEYOR_REPO_BRANCH" =~ ^release/ ]]
