#!/bin/sh
set -e errexit

framework=netcoreapp2.0
dotnet build -c Release -f $framework 
dotnet bin/Release/$framework/Sentry.Benchmarks.dll
