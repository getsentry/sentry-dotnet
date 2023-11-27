param(
    [string]$ConfigFile = "generate-solution-filters-config.yaml"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$yamlModule = "powershell-yaml"
$retries = 5
while (-not (Get-Module -ListAvailable -Name $yamlModule) -and $retries -gt 0)
{
    if ($retries -lt 5)
    {
        Start-Sleep -Seconds 10
    }
    Write-Debug "The module '$yamlModule' is not installed. Installing..."
    Install-Module -Name $yamlModule -Scope CurrentUser -Force
    $retries--
}

Import-Module $yamlModule

$separator = [IO.Path]::DirectorySeparatorChar.ToString()
$lf = if ([Environment]::OSVersion.Platform -eq [PlatformID]::Win32NT) { "`r`n" } else { "`n" }

$scriptDir = $PSScriptRoot
$repoRoot = Join-Path $scriptDir ('..' + $separator) -Resolve

# Load configuration
$configPath = Join-Path $scriptDir $ConfigFile
Write-Debug "Loading configuration file $configPath"
if (-not (Test-Path $configPath))
{
    Write-Error "Config file '$configPath' does not exist."
    exit 1
}

try
{
    $config = Get-Content $configPath | ConvertFrom-Yaml
}
catch
{
    Write-Error "Error parsing config file '$configPath': $_"
    exit 1
}

# Get list of all projects in solution
Write-Debug "Searching the repository for projects..."
$projectPaths = Get-ChildItem -Path $repoRoot -Recurse -Filter *.csproj | `
    Select-Object -ExpandProperty FullName | `
    ForEach-Object { $_.Replace($repoRoot, '').Replace('\', '/') } # Force linux style separators for glob matching
Write-Debug "Found $($projectPaths.Count) projects"

# Generate a solution filter for each filter config
foreach ($filter in $config.filterConfigs)
{
    Write-Debug "Processing filter $($filter.outputPath)"

    $includedProjects = @()

    # Process includes, if present
    if ($filter.ContainsKey("include"))
    {
        # Add include groups
        if ($filter.include.ContainsKey("groups"))
        {
            foreach ($group in $filter.include.groups)
            {
                Write-Debug "Include $group"
                foreach ($include in $config.groupConfigs.$group)
                {
                    $includedProjects += ($projectPaths | Where-Object { $_ -like $include })
                }
            }
        }

        # Add ad-hoc includes
        if ($filter.include.ContainsKey("patterns"))
        {
            foreach ($include in $filter.include.patterns)
            {
                Write-Debug "Include $include"
                $includedProjects += ($projectPaths | Where-Object { $_ -like $include })
            }
        }
    }

    # Process excludes, if present
    if ($filter.ContainsKey("exclude"))
    {
        # Remove exclude groups
        if ($filter.exclude.ContainsKey("groups"))
        {
            foreach ($group in $filter.exclude.groups)
            {
                Write-Debug "Exclude $group"
                foreach ($exclude in $config.groupConfigs.$group)
                {
                    $includedProjects = ($includedProjects | Where-Object { $_ -notlike $exclude })
                }
            }
        }

        # Remove ad-hoc excludes
        if ($filter.exclude.ContainsKey("patterns"))
        {
            foreach ($exclude in $filter.exclude.patterns)
            {
                Write-Debug "Exclude $exclude"
                $includedProjects = ($includedProjects | Where-Object { $_ -notlike $exclude })
            }
        }
    }

    # Remove duplicates and sort
    $includedProjects = $includedProjects | Select-Object -Unique | Sort-Object
    Write-Debug "Writing filter matching $($includedProjects.Count) projects"

    # Start filter file
    $solution = if ($filter.ContainsKey('solution'))
    {
        $filter.solution
    }
    else
    {
        $config.coreSolution
    }
    $content = "{
  `"solution`": {
    `"path`": `"$($solution)`",
    `"projects`": ["

    # Add all the projects we want to include
    $firstProject = $true;
    foreach ($project in $includedProjects)
    {
        # Solution Filter files use escaped Windows style path separators
        $escapedProject = $project.Replace('/', '\\')
        $line = "$lf      ""$escapedProject"""
        if (!$firstProject)
        {
            $line = "," + $line
        }
        $firstProject = $false;
        $content += $line
    }

    # Finalize filter file
    $content += "$lf"
    $content += @'
    ]
  }
}
'@

    # Output filter file
    $outputPath = Join-Path $repoRoot $filter.outputPath
    $content | Set-Content $outputPath
    Write-Debug "Created $outputPath"
}

# Copy the Core solution to each of the required build solutions
$source = Join-Path $repoRoot $config.coreSolution
foreach ($buildSolution in $config.buildSolutions) {
  $destination = Join-Path $repoRoot $buildSolution
  Copy-Item -Path $source -Destination $destination -Force
}
