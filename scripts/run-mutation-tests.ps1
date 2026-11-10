<#
.SYNOPSIS
    Runs Stryker.NET on the lines under src/ that changed since -Base.

.DESCRIPTION
    Runs Stryker once per changed project, against its test project (src/<Name> -> test/<Name>.Tests), mutating only
    the changed lines. Lists the mutants no test caught in StrykerOutput/summary.md, with the full reports in
    StrykerOutput/<Name>/reports.

    It restores the test projects without UnoptimizedAssemblyDetector, and the next normal build restores it again.

.EXAMPLE
    dotnet tool install --global dotnet-stryker
    pwsh scripts/run-mutation-tests.ps1 -Base origin/main
#>
param([string] $Base = 'origin/main')

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true
Set-Location "$PSScriptRoot/.."

# Each hunk header gives the first changed line and the number of lines, which is 0 when lines were only removed
$hunks = foreach ($line in git diff --unified=0 (git merge-base $Base HEAD) -- 'src/*.cs')
{
    if ($line -match '^\+\+\+ b/(.+)')
    {
        $file = $Matches[1]
    }
    elseif ($line -match '^@@ -\S+ \+(\d+)(?:,(\d+))? @@' -and $Matches[2] -ne '0')
    {
        $first = [int]$Matches[1]
        [pscustomobject]@{ File = $file; First = $first; Last = $first + [int]($Matches[2] ?? 1) - 1 }
    }
}

function Test-DependsOn([string] $project, [string] $dependency)
{
    foreach ($reference in (Select-Xml -Path $project -XPath '//ProjectReference/@Include' | ForEach-Object { $_.Node.Value }))
    {
        $path = [System.IO.Path]::GetFullPath(($reference -replace '\\', '/'), (Split-Path $project))
        if ($path -eq $dependency -or ((Test-Path $path) -and (Test-DependsOn $path $dependency)))
        {
            return $true
        }
    }
    return $false
}

# Other test projects cover a project too, like Sentry.AspNetCore.Tests for src/Sentry, so every test project that
# depends on it runs, except the ones that can't run on net10.0 here: Sentry.AspNet.Tests targets only .NET Framework,
# the MVVM tests only net9.0, and the Android assembly reader tests build an Android app
$runnableTests = (Get-ChildItem test/*/*.Tests.csproj).FullName | Where-Object {
    (Split-Path $_ -LeafBase) -notin 'Sentry.AspNet.Tests', 'Sentry.Maui.CommunityToolkit.Mvvm.Tests', 'Sentry.Android.AssemblyReader.Tests'
}

Remove-Item StrykerOutput -Recurse -ErrorAction Ignore
New-Item -ItemType Directory StrykerOutput | Out-Null
$summary = "$PWD/StrykerOutput/summary.md"
foreach ($project in $hunks | Group-Object { ($_.File -split '/')[1] })
{
    $name = $project.Name
    $sourceProject = [System.IO.Path]::GetFullPath("$PWD/src/$name/$name.csproj")
    $testProjects = @($runnableTests | Where-Object { Test-DependsOn $_ $sourceProject })
    if (-not $testProjects)
    {
        Add-Content $summary "### ${name}: skipped, because no test project that runs here depends on it`n"
        continue
    }
    Write-Host "Mutating $name, tested by $($testProjects.Count) test projects"

    # Stryker's mutate spans are character offsets into the file, not line numbers
    $mutate = foreach ($changedFile in $project.Group | Group-Object File)
    {
        $text = Get-Content $changedFile.Name -Raw
        $lineStarts = @(0) + @([regex]::Matches($text, "`n") | ForEach-Object { $_.Index + 1 }) + $text.Length
        $spans = $changedFile.Group | ForEach-Object { "{$($lineStarts[$_.First - 1])..$($lineStarts[$_.Last])}" }
        "**/$($changedFile.Name)$($spans -join '')"
    }

    $output = "$PWD/StrykerOutput/$name"
    New-Item -ItemType Directory -Force $output | Out-Null
    @{
        'stryker-config' = @{
            'project' = "$name.csproj"
            'test-projects' = $testProjects
            'target-framework' = 'net10.0'
            'mutate' = @($mutate)
            # Snapshot tests can't pass on mutated code: the API approval tests would rewrite their snapshots, and these
            # stack trace and logging tests record frames that Stryker's rewrite changes
            'test-case-filter' = (@(
                    'ApiApprovalTests'
                    'StringStackTraceFactoryTests.MethodGeneric'
                    'SentryStackTraceFactoryTests.MethodGeneric'
                    'Sentry.Serilog.Tests.IntegrationTests.Simple'
                    'Sentry.NLog.Tests.IntegrationTests.Simple'
                ) | ForEach-Object { "FullyQualifiedName!~$_" }) -join '&'
            # Changing a log message or ConfigureAwait(false) changes nothing a test should check
            'ignore-methods' = @('Log*', '*.Log*', '*.ConfigureAwait')
            'reporters' = @('Progress', 'Html', 'Json')
        }
    } | ConvertTo-Json -Depth 3 | Set-Content "$output/stryker-config.json"

    # Stryker looks for a solution in the working directory, and the root has several
    Push-Location (Split-Path $testProjects[0])
    try
    {
        # Stryker analyzes the projects before building them, so restore them first without UnoptimizedAssemblyDetector,
        # which breaks that analysis (see Directory.Build.props)
        $env:SkipUnoptimizedAssemblyDetector = 'true'
        $testProjects | ForEach-Object { dotnet restore $_ }
        dotnet stryker --config-file "$output/stryker-config.json" --output $output --skip-version-check
    }
    catch
    {
        # One project failing shouldn't cost the others their report
        Add-Content $summary "### ${name}: Stryker couldn't run, see the log`n"
        continue
    }
    finally
    {
        Remove-Item Env:SkipUnoptimizedAssemblyDetector -ErrorAction Ignore
        Pop-Location
    }

    $report = Get-Content "$output/reports/mutation-report.json" -Raw | ConvertFrom-Json
    $mutants = @(foreach ($reportFile in $report.files.PSObject.Properties)
    {
        foreach ($mutant in $reportFile.Value.mutants | Where-Object status -In 'Killed', 'Timeout', 'Survived', 'NoCoverage', 'CompileError')
        {
            [pscustomobject]@{
                File = [System.IO.Path]::GetRelativePath($PWD, $reportFile.Name)
                Line = $mutant.location.start.line
                Mutation = $mutant.mutatorName
                Replacement = ($mutant.replacement -replace '\s+', ' ').Trim()
                Status = $mutant.status
            }
        }
    })
    $tested = @($mutants | Where-Object Status -NE 'CompileError')
    $missed = @($tested | Where-Object Status -In 'Survived', 'NoCoverage')
    # Stryker marks every mutant in a method it can't build as CompileError, across the whole project, so only the
    # ones on changed lines count
    $compileErrors = @($mutants | Where-Object Status -EQ 'CompileError')
    $untested = @(foreach ($hunk in $project.Group)
    {
        $compileErrors | Where-Object { $_.File -eq $hunk.File -and $_.Line -ge $hunk.First -and $_.Line -le $hunk.Last }
    })

    Add-Content $summary "### ${name}: tests caught $($tested.Count - $missed.Count) of $($tested.Count) mutants on the changed lines`n"
    if ($untested)
    {
        Add-Content $summary "Stryker couldn't build $($untested.Count) mutants on the changed lines, so no test ran against them.`n"
    }
    if ($missed)
    {
        Add-Content $summary '| Location | Mutation | Replaced with | Status |', '|---|---|---|---|'
        foreach ($mutant in $missed)
        {
            # Long replacements are cut, and pipes escaped so they don't end the table cell
            $replacement = $mutant.Replacement.Length -gt 80 ? $mutant.Replacement.Substring(0, 77) + '...' : $mutant.Replacement
            Add-Content $summary "| ``$($mutant.File):$($mutant.Line)`` | $($mutant.Mutation) | ``$($replacement -replace '\|', '\|')`` | $($mutant.Status) |"
            # GitHub Actions shows this as a warning on the line in the pull request's diff
            Write-Host "::warning file=$($mutant.File),line=$($mutant.Line),title=Mutant not caught::$($mutant.Mutation) ($($mutant.Status)): $replacement"
        }
        Add-Content $summary ''
    }
}

if (-not (Test-Path $summary))
{
    New-Item -Force $summary -Value "No lines under ``src/`` were added or changed, so there's nothing to mutate.`n" | Out-Null
}
