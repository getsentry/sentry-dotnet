<#
.SYNOPSIS
    Reports vulnerable packages, failing only on advisories that are not allowlisted.

.DESCRIPTION
    Wraps `dotnet package list --vulnerable` and filters its machine-readable output
    through scripts/vulnerability-allowlist.json.

    We deliberately floor our PackageReferences at the oldest version that works, so most
    findings are floors we have decided not to raise rather than something to action. The
    allowlist records those decisions and the reasoning; anything not listed fails.

    Fails closed: any error running or parsing the scan is a failure, never a pass.

.PARAMETER Project
    Solution or project to scan. Defaults to Sentry.slnx.

.PARAMETER InputJson
    Use an existing `--format json` report instead of invoking dotnet. For testing.

.PARAMETER AllowlistPath
    Defaults to scripts/vulnerability-allowlist.json next to this script.
#>
[CmdletBinding()]
param(
    [string] $Project = 'Sentry.slnx',
    [string] $InputJson,
    [string] $AllowlistPath = (Join-Path $PSScriptRoot 'vulnerability-allowlist.json')
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Read-Allowlist([string] $path) {
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Allowlist not found: $path"
    }
    $parsed = Get-Content -LiteralPath $path -Raw | ConvertFrom-Json
    $entries = @($parsed.allowed)
    foreach ($entry in $entries) {
        foreach ($field in 'advisory', 'package', 'reason') {
            if ([string]::IsNullOrWhiteSpace($entry.$field)) {
                throw "Allowlist entry is missing '$field': $($entry | ConvertTo-Json -Compress)"
            }
        }
    }
    # Comma operator: stop PowerShell unrolling the array, so an empty allowlist stays an
    # empty array instead of collapsing to $null.
    return , $entries
}

function Get-ScanReport([string] $project, [string] $inputJson) {
    if ($inputJson) {
        if (-not (Test-Path -LiteralPath $inputJson)) {
            throw "Input JSON not found: $inputJson"
        }
        return Get-Content -LiteralPath $inputJson -Raw
    }

    # --source pins the query to nuget.org - the only feed that publishes a
    # VulnerabilityInfo resource. The other feeds can error out with 401s.
    $output = dotnet package list --project $project --vulnerable --include-transitive `
        --no-restore --format json --source https://api.nuget.org/v3/index.json 2>&1 | Out-String

    # A non-zero exit still yields a valid report when the cause is per-project problems, which
    # Assert-NoProblems explains far better than the raw JSON would. Only treat output we can't
    # parse at all as a hard failure here.
    try {
        $null = $output | ConvertFrom-Json
    }
    catch {
        throw "dotnet package list exited with $LASTEXITCODE and produced no readable report:`n$output"
    }
    return $output
}

# A project that failed to report wasn't scanned, so we can't say it's clean. Usually it just
# wasn't restored - `dotnet restore Sentry.slnx` covers projects the .slnf filters leave out.
function Assert-NoProblems($report) {
    if ($report.PSObject.Properties.Name -notcontains 'problems') { return }
    $problems = @($report.problems)
    if ($problems.Count -eq 0) { return }

    Write-Host ''
    Write-Host "$($problems.Count) project(s) could not be scanned:"
    foreach ($problem in $problems) {
        $name = if ($problem.PSObject.Properties.Name -contains 'project') { Split-Path -Leaf $problem.project } else { '?' }
        Write-Host "  [$($problem.level)] $name - $($problem.text)"
    }
    throw 'Cannot report on vulnerabilities while projects are missing from the scan. Run `dotnet restore Sentry.slnx` first.'
}

# `dotnet package list` reports a vulnerability once per project/framework/package, so the
# same advisory shows up many times. Flatten to one row each so we can group and count.
function Get-Findings($report) {
    $rows = [System.Collections.Generic.List[object]]::new()
    foreach ($project in @($report.projects)) {
        $name = [System.IO.Path]::GetFileNameWithoutExtension($project.path)
        # A project with nothing to report has no 'frameworks' member at all.
        $frameworks = if ($project.PSObject.Properties.Name -contains 'frameworks') { @($project.frameworks) } else { @() }
        foreach ($framework in $frameworks) {
            foreach ($kind in 'topLevelPackages', 'transitivePackages') {
                if ($framework.PSObject.Properties.Name -notcontains $kind) { continue }
                foreach ($package in @($framework.$kind)) {
                    if ($package.PSObject.Properties.Name -notcontains 'vulnerabilities') { continue }
                    foreach ($vulnerability in @($package.vulnerabilities)) {
                        $rows.Add([pscustomobject]@{
                            Project   = $name
                            Framework = $framework.framework
                            Package   = $package.id
                            Version   = $package.resolvedVersion
                            Severity  = $vulnerability.severity
                            Advisory  = $vulnerability.advisoryurl
                        })
                    }
                }
            }
        }
    }
    # As above: a clean scan must return an empty collection, not $null.
    return , $rows
}

function Write-Findings([string] $heading, $findings) {
    Write-Host ''
    Write-Host $heading
    $findings |
        Sort-Object Package, Version, Project, Framework |
        Select-Object Package, Version, Severity, Project, Framework, Advisory |
        Format-Table -AutoSize |
        Out-String -Width 200 |
        Write-Host
}

$allowlist = Read-Allowlist $AllowlistPath
$report = Get-ScanReport $Project $InputJson | ConvertFrom-Json
Assert-NoProblems $report
$findings = Get-Findings $report

# Match on advisory + package, so a new advisory against an already-listed package still fails.
$matched = @{}
$allowed = [System.Collections.Generic.List[object]]::new()
$blocking = [System.Collections.Generic.List[object]]::new()
foreach ($finding in $findings) {
    $entry = $allowlist | Where-Object { $_.advisory -eq $finding.Advisory -and $_.package -eq $finding.Package } | Select-Object -First 1
    if ($entry) {
        $matched["$($entry.advisory)|$($entry.package)"] = $true
        $allowed.Add($finding)
    }
    else {
        $blocking.Add($finding)
    }
}

Write-Host "Scanned $(@($report.projects).Count) projects; $($findings.Count) vulnerability findings."

if ($allowed.Count -gt 0) {
    Write-Findings "Allowlisted ($($allowed.Count) findings) - see $(Split-Path -Leaf $AllowlistPath) for the reasoning:" $allowed
}

# An entry that matches nothing is stale - the finding it covered is gone. Report it so the
# allowlist does not rot, but do not fail on it: fixing a vulnerability must never break CI.
$stale = $allowlist | Where-Object { -not $matched.ContainsKey("$($_.advisory)|$($_.package)") }
if ($stale) {
    Write-Host ''
    Write-Host '::warning::Stale allowlist entries - these no longer match any finding and should be removed:'
    foreach ($entry in $stale) {
        Write-Host "  $($entry.package) $($entry.advisory)"
    }
}

if ($blocking.Count -eq 0) {
    Write-Host ''
    Write-Host 'No vulnerable packages outside the allowlist.'
    exit 0
}

Write-Findings "Not allowlisted ($($blocking.Count) findings):" $blocking
$packages = ($blocking | Select-Object -ExpandProperty Package -Unique | Sort-Object) -join ', '
Write-Host "::error::Vulnerable packages detected outside the allowlist: $packages. Either update the dependency, or add an entry to $(Split-Path -Leaf $AllowlistPath) explaining why the floor stays."
exit 1
