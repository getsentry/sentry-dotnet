#!/bin/bash
set -e

dotnet test -c Release \
    /p:Godot=1 \
    /p:CollectCoverage=true \
    /p:CoverletOutputFormat=opencover \
    /p:Exclude=\"[Sentry.Protocol.Test*]*,[xunit.*]*,[Sentry.Test*]*\" \
    /p:UseSourceLink=true

# Docs
pushd docs
./build.sh
popd
