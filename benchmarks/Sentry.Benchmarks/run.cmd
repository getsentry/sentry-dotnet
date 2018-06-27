dotnet build -c Release -o bin/Release
if not errorlevel 0 exit /b -1

dotnet bin/Release/Sentry.Benchmarks.dll
