param(
    [string]$ConfigFile = "generate-solution-filters-config.json"
)

$separator = [IO.Path]::DirectorySeparatorChar

# Get the script directory
$scriptDir = $PSScriptRoot
$repoRoot = Join-Path $scriptDir ('..' + $separator) -Resolve

# Configuration
$configPath = Join-Path $scriptDir $ConfigFile
$config = Get-Content $configPath | ConvertFrom-Json

# Get list of all projects in solution
$projectPaths = Get-ChildItem -Path $repoRoot -Recurse -Filter *.csproj | Select-Object -ExpandProperty FullName

# Strip repo root from projects
$projectPaths = $projectPaths | ForEach-Object {
    $_.Replace($repoRoot, '')
  }

# Loop through each filter config
foreach($filter in $config.filterConfigs){

    # Add includes
    $includedProjects = @()
    foreach($include in $filter.includes){
        $includedProjects += ($projectPaths | Where-Object { $_ -like $include })
    }

    # Remove excludes
    foreach($exclude in $filter.excludes){
        $includedProjects = ($includedProjects | Where-Object { $_ -notlike $exclude })
    }

    # Remove duplicates
    $includedProjects = $includedProjects | Select-Object -Unique

    # Start filter file
    $content = "{
  `"solution`": {
    `"path`": `"$($config.solution)`",
    `"projects`": ["

    # Add all the projects we want to include
    foreach($project in $includedProjects) {
        # Escape path separators for Windows-style
        $escapedProject = $project.ToString().Replace($separator.ToString(), '\\')
        $content += "`n      ""$escapedProject"","
      }

    # Finalize filter file
    $content += "`n"
    $content += @'
    ]
  }
}
'@

  # Output filter file
  $outputPath = Join-Path $repoRoot $filter.outputPath
  $content | Set-Content $outputPath
}
