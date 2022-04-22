$ErrorActionPreference = "Stop"

dotnet test SentryNoSamples.slnf -c Release -l "GitHubActions;report-warnings=false" `
    /p:CopyLocalLockFileAssemblies=true
if ($LASTEXITCODE -ne 0) { exit 1 }

dotnet pack SentryNoSamples.slnf -c Release /p:ContinuousIntegrationBuild=true
if ($LASTEXITCODE -ne 0) { exit 1 }
