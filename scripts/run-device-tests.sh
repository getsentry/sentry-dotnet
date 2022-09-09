#!/bin/bash

pushd "$(dirname "$0")" > /dev/null

dotnet build -f net6.0-android ../test/Sentry.Maui.Device.TestApp

if ! command -v xharness &> /dev/null
then
    dotnet tool install Microsoft.DotNet.XHarness.CLI --global --version "1.*-*"
fi

rm -rf ../test_output

xharness android test \
    --app=../test/Sentry.Maui.Device.TestApp/bin/Debug/net6.0-android/io.sentry.dotnet.maui.device.testapp-Signed.apk \
    --package-name=io.sentry.dotnet.maui.device.testapp \
    --output-directory=../test_output

popd > /dev/null
