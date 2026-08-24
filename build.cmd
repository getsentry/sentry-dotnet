dotnet build Sentry.slnx -c Release
dotnet test Sentry.slnx -c Release --no-build /p:CollectCoverage=true
