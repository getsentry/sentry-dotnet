#!/bin/bash
set -e

if [ "$GITHUB_ACTIONS" == "true" ]
    then
        testLogger="GitHubActions;report-warnings=false"
    else
        testLogger="console"
fi

dotnet test test/Sentry.Tests -c Debug -l $testLogger \
    --filter ShouldNotLogOperationCanceledExceptionWhenIsCancellationRequested \
    /p:CopyLocalLockFileAssemblies=true
