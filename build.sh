#!/bin/bash
set -e

if [ "$GITHUB_ACTIONS" == "true" ]
    then
        testLogger="GitHubActions;report-warnings=false"
    else
        testLogger="console"
fi

case "$OSTYPE" in
  darwin*)  export Filter=SentryMac.slnf ;;
  linux*)   export Filter=SentryLinux.slnf ;;
  *)        exit -1 ;;
esac

dotnet test $Filter -c Release -l $testLogger \
    /p:CollectCoverage=true \
    /p:CoverletOutputFormat=opencover \
    /p:Exclude=\"[Sentry.Protocol.Test*]*,[xunit.*]*,[Sentry.Test*]*\" \
    /p:UseSourceLink=true
