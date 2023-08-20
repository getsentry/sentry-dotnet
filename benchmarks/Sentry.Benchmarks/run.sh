#!/bin/bash
set -e errexit

framework=net6.0
dotnet build -c Release -f $framework
dotnet bin/Release/$framework/Sentry.Benchmarks.dll
