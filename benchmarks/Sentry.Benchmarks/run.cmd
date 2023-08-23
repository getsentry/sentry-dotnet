set framework=net6.0

dotnet build -c Release -f %framework%
if not errorlevel 0 exit /b -1

dotnet bin\Release\%framework%\Sentry.Benchmarks.dll
