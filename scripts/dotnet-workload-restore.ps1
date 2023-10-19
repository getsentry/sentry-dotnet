# `dotnet workload restore` sometimes doesn't fetch all workloads on the first run
# This script runs the same command until it stops fetching new workloads
param([string] $ProjectOrSolution = 'Sentry.sln')

Set-StrictMode -Version latest
$ErrorActionPreference = "Stop"


# Restore workloads and return the list of installed ones.
function DotnetWorkloadRestore([string] $target)
{
    $matchText = "Successfully installed workload\(s\) "
    Write-Host "::group::Restoring workloads for $target"
    try
    {
        $tempArg = (Test-Path env:RUNNER_TEMP) ? @("--temp-dir", $env:RUNNER_TEMP) : ''
        dotnet workload restore $target --from-rollback-file rollback.json $tempArg --nologo | ForEach-Object {
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
    finally
    {
        Write-Host "::endgroup::"
    }
}

# Solution filters are not accepted by `dotnet workload restore`. See https://github.com/dotnet/sdk/issues/36277
$projects = @($ProjectOrSolution)
if ($ProjectOrSolution -match "\.slnf$")
{
    $slnf = Get-Content -Raw .\Sentry-CI-Build-Windows.slnf | ConvertFrom-Json
    $projects = $slnf.solution.projects 
}

$prevInstalled = @();
$projects | ForEach-Object {
    $project = $_
    $maxRuns = 5
    while ($true)
    {
        $installed = DotnetWorkloadRestore $project
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
}