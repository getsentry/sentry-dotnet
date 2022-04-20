#!/bin/bash
set -e

dotnet test SentryNoSamples.slnf \
    -c Release \
    /p:CopyLocalLockFileAssemblies=true \
    /p:Exclude=\"[Sentry.Protocol.Test*]*,[xunit.*]*,[System.*]*,[Microsoft.*]*,[Sentry.Test*]*\" \
    /p:UseSourceLink=true
