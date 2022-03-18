#!/bin/bash
set -e

if [ "$GITHUB_ACTIONS" == "true" ]
    then
        testLogger="GitHubActions;report-warnings=false"
    else
        testLogger="console"
fi

dotnet test SentryNoSamples.slnf -c Release -l $testLogger \
    --filter ShouldNotLogOperationCanceledExceptionWhenIsCancellationRequested \
    /p:CollectCoverage=true \
    /p:CoverletOutputFormat=opencover \
    /p:CopyLocalLockFileAssemblies=true \
    /p:Exclude=\"[Sentry.Protocol.Test*]*,[xunit.*]*,[System.*]*,[Microsoft.*]*,[Sentry.Test*]*\" \
    /p:UseSourceLink=true
