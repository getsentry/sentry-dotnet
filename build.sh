#!/bin/bash
set -e

if [ "$GITHUB_ACTIONS" == "true" ]
    then
        testLogger="GitHubActions"
    else
        testLogger="console"
fi

dotnet test -c Release -l $testLogger \
    /p:CollectCoverage=true \
    /p:CoverletOutputFormat=opencover \
    /p:Exclude=\"[Sentry.Protocol.Test*]*,[xunit.*]*,[Sentry.Test*]*\" \
    /p:UseSourceLink=true

# Docs
# Docs building is broken on travis: ImportError: No module named dateutil
#pushd docs
#./build.sh
#popd
