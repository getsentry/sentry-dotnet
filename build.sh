#!/bin/bash
set -e

dotnet test SentryNoSamples.slnf \
    -c Release \
    /p:CopyLocalLockFileAssemblies=true
