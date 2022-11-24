#!/bin/bash

pushd "$(dirname "$0")" > /dev/null

if ! command -v xharness &> /dev/null
then
    dotnet tool install Microsoft.DotNet.XHarness.CLI --global --version "1.*-*"
fi

dotnet build -f net7.0-ios ../test/Sentry.Maui.Device.TestApp

if [ $? -eq 0 ]
then
    [ "$(arch)" == "arm64" ] && ARCH="arm64" || ARCH="x64"
    rm -rf ../test_output
    xharness apple test \
        --app=../test/Sentry.Maui.Device.TestApp/bin/Debug/net7.0-ios/iossimulator-$ARCH/Sentry.Maui.Device.TestApp.app \
        --target=ios-simulator-64 \
        --output-directory=../test_output
fi

popd > /dev/null
