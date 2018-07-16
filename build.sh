#!/bin/bash
set -e

dotnet test -c Release /p:CollectCoverage=true /p:CoverletOutputFormat=opencover /p:Exclude="[Sentry.Test*]*"

# Docs
pushd docs
./build.sh
popd
