@echo off
setlocal

pushd %~dp0

dotnet build -f net6.0-android ..\test\Sentry.Maui.Device.TestApp

where xharness >nul 2>nul
if %ERRORLEVEL% NEQ 0 dotnet tool install Microsoft.DotNet.XHarness.CLI --global --version "1.*-*"

if exist ..\test_output rmdir /q /s ..\test_output

xharness android test ^
    --app=..\test\Sentry.Maui.Device.TestApp\bin\Debug\net6.0-android\io.sentry.dotnet.maui.device.testapp-Signed.apk ^
    --package-name=io.sentry.dotnet.maui.device.testapp ^
    --output-directory=..\test_output

popd
