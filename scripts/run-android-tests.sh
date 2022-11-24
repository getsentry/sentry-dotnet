#!/bin/bash

pushd "$(dirname "$0")" > /dev/null

if ! command -v xharness &> /dev/null
then
    dotnet tool install Microsoft.DotNet.XHarness.CLI --global --version "1.*-*"
fi

dotnet build -f net7.0-android ../test/Sentry.Maui.Device.TestApp

if [ $? -eq 0 ]
then
    rm -rf ../test_output
    xharness android test \
        --app=../test/Sentry.Maui.Device.TestApp/bin/Debug/net7.0-android/io.sentry.dotnet.maui.device.testapp-Signed.apk \
        --package-name=io.sentry.dotnet.maui.device.testapp \
        --output-directory=../test_output
fi

popd > /dev/null
