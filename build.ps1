$ErrorActionPreference = "Stop"

$testLogger = if ($env:GITHUB_ACTIONS -eq "true") {"GitHubActions;report-warnings=false"} else {"console"}

# Fixes an issue with GitHub Actions build on Windows
dotnet nuget locals all --clear

dotnet restore --locked-mode
 if ($LASTEXITCODE -ne 0) { exit 1 }

dotnet test -c Release -l $testLogger `
    /p:CollectCoverage=true `
    /p:CoverletOutputFormat=opencover `
    /p:Exclude='"""[Sentry.Protocol.Test*]*,[xunit.*]*,[Sentry.Test*]*\"""'
if ($LASTEXITCODE -ne 0) { exit 1 }

dotnet pack -c Release /p:ContinuousIntegrationBuild=true
if ($LASTEXITCODE -ne 0) { exit 1 }
