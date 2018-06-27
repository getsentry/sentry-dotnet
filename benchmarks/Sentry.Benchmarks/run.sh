#!/bin/sh
set -e errexit

dotnet build -c Release -o bin/Release
dotnet bin/Release/Sentry.Benchmarks.dll
