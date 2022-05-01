$ErrorActionPreference = "Stop"

$testLogger = if ($env:GITHUB_ACTIONS -eq "true") {"GitHubActions;report-warnings=false"} else {"console"}

dotnet test SentryWindows.slnf -c Release -l $testLogger `
    /p:CopyLocalLockFileAssemblies=true
if ($LASTEXITCODE -ne 0) { exit 1 }

dotnet pack SentryWindows.slnf -c Release --no-build
if ($LASTEXITCODE -ne 0) { exit 1 }
