Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# In CI, the module is loaded automatically
if (!(Test-Path env:CI ))
{
    Import-Module $PSScriptRoot/../../github-workflows/sentry-cli/integration-test/action.psm1 -Force
}

BeforeAll {
    function DotnetBuild([string]$Sample)
    {
        $rootDir = "$(Get-Item $PSScriptRoot/../../)"

        Invoke-SentryServer {
            Param([string]$url)
            Write-Host "Building $Sample"
            dotnet build "samples/$sample/$sample.csproj" -c Release --no-restore --nologo `
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
    }
}

Describe 'CLI-integration' {
    It "uploads symbols for a console app build" {
        $result = DotnetBuild 'Sentry.Samples.Console.Basic'
        $result.ScriptOutput | Should -Contain 'Build succeeded.'
        $result.UploadedDebugFiles() | Select-Object -Unique | Should -Be @('Sentry.pdb', 'Sentry.Samples.Console.Basic.pdb', 'apphost.exe')
    }

    It "uploads symbols for a MAUI app build" {
        $result = DotnetBuild 'Sentry.Samples.Maui'
        $result.ScriptOutput | Should -Contain 'Build succeeded.'
        $result.UploadedDebugFiles() | Select-Object -Unique | Should -Be @() # TODO
    }
}