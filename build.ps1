$ErrorActionPreference = "Stop"

dotnet test SentryNoSamples.slnf `
    -c Release `
    /p:CopyLocalLockFileAssemblies=true `
    /p:Exclude='"""[Sentry.Protocol.Test*]*,[xunit.*]*,[System.*]*,[Microsoft.*]*,[Sentry.Test*]*\"""'
if ($LASTEXITCODE -ne 0) { exit 1 }

dotnet pack SentryNoSamples.slnf -c Release /p:ContinuousIntegrationBuild=true
if ($LASTEXITCODE -ne 0) { exit 1 }
