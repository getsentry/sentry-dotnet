$ErrorActionPreference = "Stop"

$testLogger = if ($env:GITHUB_ACTIONS -eq "true") {"GitHubActions;report-warnings=false"} else {"console"}

dotnet test SentryNoSamples.slnf -c Release -l $testLogger `
    /p:CopyLocalLockFileAssemblies=true
if ($LASTEXITCODE -ne 0) { exit 1 }

dotnet pack SentryNoSamples.slnf -c Release /p:ContinuousIntegrationBuild=true
if ($LASTEXITCODE -ne 0) { exit 1 }
