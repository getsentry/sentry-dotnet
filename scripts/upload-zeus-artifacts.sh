#!/usr/bin/env bash
set -eux

zeus upload -t "application/zip+nupkg" ./src/*/bin/Release/*.nupkg || [[ ! "$APPVEYOR_REPO_BRANCH" =~ ^release/ ]]

zeus job update --status=passed || [[ ! "$APPVEYOR_REPO_BRANCH" =~ ^release/ ]]
