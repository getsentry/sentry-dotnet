set UseSentryCLI=false
dotnet build Sentry.sln -c Release
dotnet test Sentry.sln -c Release --no-build
