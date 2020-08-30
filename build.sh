#!/bin/bash
set -e

dotnet test -c Release \
    /p:CollectCoverage=true \
    /p:CoverletOutputFormat=opencover \
    /p:Exclude=\"[Sentry.Protocol.Test*]*,[xunit.*]*,[Sentry.Testing]*,[Sentry.Test*]*\" \
    /p:UseSourceLink=true

# Docs
# Docs building is broken on travis: ImportError: No module named dateutil
#pushd docs
#./build.sh
#popd
