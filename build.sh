#!/bin/bash
set -e

dotnet restore --locked-mode
dotnet test -c Release \
    --no-restore \
    /p:CollectCoverage=true \
    /p:CoverletOutputFormat=opencover \
    /p:Exclude=\"[Sentry.Test*]*,[xunit.*]*\" \
    /p:UseSourceLink=true

# Docs
pushd docs
./build.sh
popd
