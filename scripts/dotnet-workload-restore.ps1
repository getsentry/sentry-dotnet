# `dotnet workload restore` sometimes doesn't fetch all workloads on the first run
# This script runs the same command until it stops fetching new workloads
param([string] $ProjectOrSolution = 'Sentry.sln')

Set-StrictMode -Version latest
$ErrorActionPreference = "Stop"


# Restore workloads and return the list of installed ones.
$dotnetWorkloadRestore = {
    $matchText = "Successfully installed workload\(s\) "
    Write-Host "Restoring $ProjectOrSolution"
    $tempArg = (Test-Path env:RUNNER_TEMP) ? @("--temp-dir", $env:RUNNER_TEMP) : ''
    dotnet workload restore $ProjectOrSolution --from-rollback-file rollback.json $tempArg --nologo | ForEach-Object {
        Write-Host "  $_"
        if ($_ -match $matchText)
        {
            $installed = $_
            $installed = $installed -replace $matchText, ''
            $installed = $installed -replace '\.', '' # trailing dot
            $installed = $installed.Trim() -split ' '
            $installed | Sort-Object -Unique
        }
    }
}

$maxRuns = 5
$prevInstalled = @();
while ($true)
{
    $installed = $dotnetWorkloadRestore.Invoke()
    if ("$installed" -eq "")
    {
        throw "Installation error"
    }
    elseif ("$installed" -eq "$prevInstalled")
    {
        Write-Host "No new packages installed since the previous run, stopping."
        break
    }
    $prevInstalled = $installed
    
    if (--$maxRuns -le 0)
    {
        throw "Too many retries to install all workloads"
    }
}