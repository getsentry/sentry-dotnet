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

BeforeAll {
    $env:SENTRY_LOG_LEVEL = 'debug';

    function GetSentryPackageVersion()
    {
        (Select-Xml -Path "$PSScriptRoot/../Directory.Build.props" -XPath "/Project/PropertyGroup/Version").Node.InnerText
    }

    function RegisterLocalPackage([string] $name)
    {
        $packageVersion = GetSentryPackageVersion
        $packagePath = "src/$name/bin/Release/$name.$packageVersion.nupkg"
        if (-not (Test-Path env:CI))
        {
            Write-Host "Packaging $name, expected output path: $packagePath"
            dotnet pack src/$name -c Release --nologo --no-restore -p:Version=$packageVersion -p:IsPackable=true | ForEach-Object { Write-Host $_ }
            if ($LASTEXITCODE -ne 0)
            {
                throw "Failed to package $name."
            }
        }
        Write-Host "Using package $packagePath - $((Get-Item $packagePath).Length) bytes"

        nuget add $packagePath -source ./temp/packages | ForEach-Object { Write-Host $_ }
        if ($LASTEXITCODE -ne 0)
        {
            throw "Failed to add package $name to a local nuget source."
        }

        # We need to remove the package from cache or it won't re resolved properly
        Remove-Item -Path ~/.nuget/packages/$name/$packageVersion -Recurse -Force -ErrorAction SilentlyContinue
    }

    Remove-Item -Path ./temp/packages -Recurse -Force -ErrorAction SilentlyContinue
    RegisterLocalPackage 'Sentry'

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

    function AddPackageReference([string] $projectPath, [string] $package)
    {
        Push-Location $projectPath
        try
        {
            dotnet restore | ForEach-Object { Write-Host $_ }
            if ($LASTEXITCODE -ne 0)
            {
                throw "Failed to add restore test app project."
            }

            $packageVersion = GetSentryPackageVersion
            dotnet add package $package --source $PSScriptRoot/../temp/packages --version $packageVersion | ForEach-Object { Write-Host $_ }
            if ($LASTEXITCODE -ne 0)
            {
                throw "Failed to add package dependency to the test app project."
            }
        }
        finally
        {
            Pop-Location
        }
    }
    function DotnetNew([string] $type, [string] $path, [string] $framework)
    {
        Remove-Item -Path $path -Recurse -Force -ErrorAction SilentlyContinue
        dotnet new $type --output $path --framework $framework | ForEach-Object { Write-Host $_ }
        if ($LASTEXITCODE -ne 0)
        {
            throw "Failed to create the test app '$path' from template '$type'."
        }

        if ($type -eq 'maui')
        {
            if (Test-Path env:CI)
            {
                Push-Location $path
                try
                {
                    dotnet workload restore | ForEach-Object { Write-Host $_ }
                    if ($LASTEXITCODE -ne 0)
                    {
                        throw "Failed to restore workloads."
                    }
                    dotnet restore | ForEach-Object { Write-Host $_ }
                    if ($LASTEXITCODE -ne 0)
                    {
                        throw "Failed to restore."
                    }
                }
                finally
                {
                    Pop-Location
                }
            }
            AddPackageReference $path 'Sentry.Maui'
        }
        else
        {
            AddPackageReference $path 'Sentry'
            if (!$IsMacOS -or $framework -eq 'net8.0')
            {
                @"
<Project>
  <PropertyGroup>
    <PublishAot>true</PublishAot>
  </PropertyGroup>
</Project>
"@ | Out-File $path/Directory.build.props
            }
        }
    }
}
