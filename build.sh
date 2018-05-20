#!/bin/bash
set -e

msbuild /t:restore,build /p:Configuration=Release

targets=`grep -oP 'TargetFrameworks\>\K(.*)(?=\<)' \
    test/Sentry.PlatformAbstractions.Tests/Sentry.PlatformAbstractions.Tests.csproj \
    | awk -F ";" '{ for(i = 1; i <= NF; i++) { print $i; } }'`

nunitRunner=".nunit/nunit3-console.exe"
if [ ! -f $nunitRunner ]; then
    nunitPackage=NUnit.Console-3.8.0.zip
    curl -LO https://github.com/nunit/nunit-console/releases/download/3.8/$nunitPackage
    unzip $nunitPackage -d ".nunit"
    rm $nunitPackage
fi

echo -e "\033[92mTargets found:\n${targets[@]}\033[0m"

for target in $targets; do
    echo -e "\033[92mTesting $target\033[0m"

    if [[ "$target" == netcore* ]]; then
        dotnet test -c Release --no-build -f $target test/Sentry.PlatformAbstractions.Tests/Sentry.PlatformAbstractions.Tests.csproj
    else
        mono $nunitRunner test/Sentry.PlatformAbstractions.Tests/bin/Release/$target/Sentry.PlatformAbstractions.Tests.dll
    fi
done;
