#!/bin/bash
set -e

if [ "$GITHUB_ACTIONS" == "true" ]; then
    testLogger="GitHubActions;report-warnings=false"
else
    testLogger="console"
fi

dotnet test -c Release -l $testLogger \
    /p:CopyLocalLockFileAssemblies=true

// test 2
