$ErrorActionPreference = "Stop"

$testLogger = if ($env:GITHUB_ACTIONS -eq "true") {"GitHubActions;report-warnings=false"} else {"console"}

dotnet test test/Sentry.Tests -c Debug -l $testLogger `
    --filter ShouldNotLogOperationCanceledExceptionWhenIsCancellationRequested
if ($LASTEXITCODE -ne 0) { exit 1 }
