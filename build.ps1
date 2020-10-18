$ErrorActionPreference = "Stop"

$testLogger = if ($env:GITHUB_ACTIONS -eq "true") {"GitHubActions;report-warnings=false"} else {"console"}

dotnet test -c Release -l $testLogger `
    /p:CollectCoverage=true `
    /p:CoverletOutputFormat=opencover `
    /p:Exclude='"""[Sentry.Protocol.Test*]*,[xunit.*]*,[Sentry.Test*]*\"""'
if ($LASTEXITCODE -ne 0) { exit 1 }

dotnet pack -c Release --no-build /p:ContinuousIntegrationBuild=true
if ($LASTEXITCODE -ne 0) { exit 1 }
