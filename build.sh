#!/bin/bash
set -e

dotnet test -c Release \
    /p:CollectCoverage=true \
    /p:CoverletOutputFormat=opencover \
    /p:Exclude=\"[Sentry.Test*]*,[xunit.*]*\" \
    /p:UseSourceLink=true

# Docs
pushd docs
./build.sh
popd
