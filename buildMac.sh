#!/bin/bash
set -e

if [ "$GITHUB_ACTIONS" == "true" ]
    then
        testLogger="GitHubActions;report-warnings=false"
    else
        testLogger="console"
fi

dotnet test $solutionFilterFile -c Release -l $testLogger \
    /p:CollectCoverage=true \
    /p:CoverletOutputFormat=opencover \
    /p:Exclude=\"[Sentry.Protocol.Test*]*,[xunit.*]*,[Sentry.Test*]*\" \
    /p:UseSourceLink=true
