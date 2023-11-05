# So that this works in VS Code testing integration. Otherwise the script is run within its directory.
Push-Location $PSScriptRoot/../

# In CI, the module is loaded automatically
if (!(Test-Path env:CI ))
{
    Import-Module $PSScriptRoot/../../github-workflows/sentry-cli/integration-test/action.psm1 -Force
}

function ShouldAnyElementMatch ($ActualValue, [string]$ExpectedValue, [switch] $Negate, [string] $Because)
{
    <#
    .SYNOPSIS
        Asserts whether any item in the collection matches the expected value
    .EXAMPLE
        'foo','bar','foobar' | Should -AnyElementMatch 'oob'

        This should pass because 'oob' is a substring of 'foobar'.
    #>

    $filtered = $ActualValue | Where-Object { $_ -match $ExpectedValue }
    [bool] $succeeded = @($filtered).Count -gt 0
    if ($Negate) { $succeeded = -not $succeeded }

    if (-not $succeeded)
    {
        if ($Negate)
        {
            $failureMessage = "Expected string '$ExpectedValue' to match no elements in collection @($($ActualValue -join ', '))$(if($Because) { " because $Because"})."
        }
        else
        {
            $failureMessage = "Expected string '$ExpectedValue' to match any element in collection @($($ActualValue -join ', '))$(if($Because) { " because $Because"})."
        }
    }
    else
    {
        $failureMessage = $null
    }

    return [pscustomobject]@{
        Succeeded      = $succeeded
        FailureMessage = $failureMessage
    }
}

BeforeDiscovery {
    Add-ShouldOperator -Name AnyElementMatch `
        -InternalName 'ShouldAnyElementMatch' `
        -Test ${function:ShouldAnyElementMatch} `
        -SupportsArrayInput
}

function RunDotnet([string] $action, [string]$project, [bool]$Symbols, [bool]$Sources, [string]$TargetFramework = 'net7.0')
{
    $rootDir = "$(Get-Item $PSScriptRoot/../../)"

    $result = Invoke-SentryServer {
        Param([string]$url)
        Write-Host "::group::${action}ing $project"
        try
        {
            dotnet $action $project `
                -c Release `
                --nologo `
                --framework $TargetFramework `
                /p:SentryUploadSymbols=$Symbols `
                /p:SentryUploadSources=$Sources `
                /p:SentryOrg=org `
                /p:SentryProject=project `
                /p:SentryUrl=$url `
                /p:SentryAuthToken=dummy `
            | ForEach-Object {
                if ($_ -match "^Time Elapsed ")
                {
                    "Time Elapsed [value removed]"
                }
                elseif ($_ -match "\[[0-9/]+\]")
                {
                    # Skip lines like `[102/103] Sentry.Samples.Maui.dll -> Sentry.Samples.Maui.dll.so`
                }
                else
                {
                    "$_". `
                        Replace($rootDir, '').  `
                        Replace('\', '/')
                }
            }
            | ForEach-Object {
                Write-Host "  $_"
                $_
            }
        }
        finally
        {
            Write-Host "::endgroup::"
        }
    }

    if ($action -eq "build")
    {
        $result.ScriptOutput | Should -Contain 'Build succeeded.'
    }
    elseif ($action -eq "publish")
    {
        $result.ScriptOutput | Should -AnyElementMatch "$((Get-Item $project).Basename) -> .*$project/bin/Release/$TargetFramework/.*/publish"
    }
    $result.ScriptOutput | Should -Not -AnyElementMatch "Preparing upload to Sentry for project 'Sentry'"
    $result.HasErrors() | Should -BeFalse
    $result
}
