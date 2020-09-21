$ErrorActionPreference = "Stop"
$logger = If ($env:GITHUB_ACTIONS -eq "true") {"GitHubActions"} else {"console"}

dotnet test -c Release -l $logger `
    /p:CollectCoverage=true `
    /p:CoverletOutputFormat=opencover `
    /p:Exclude='"""[Sentry.Protocol.Test*]*,[xunit.*]*,[Sentry.Test*]*\"""'
if ($LASTEXITCODE -ne 0) { exit 1 }

dotnet pack -c Release --no-build
if ($LASTEXITCODE -ne 0) { exit 1 }
