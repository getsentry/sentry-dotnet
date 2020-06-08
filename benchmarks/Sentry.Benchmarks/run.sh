#!/bin/bash
set -e errexit

framework=netcoreapp2.1
dotnet build -c Release -f $framework
dotnet bin/Release/$framework/Sentry.Benchmarks.dll
