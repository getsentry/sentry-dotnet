# This file contains test cases for https://pester.dev/
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

BeforeAll {
    $env:SENTRY_LOG_LEVEL = 'debug';
    . $PSScriptRoot/test-functions.ps1
    function GetSentryPackageVersion()
    {
        (Select-Xml -Path "$PSScriptRoot/../Directory.Build.props" -XPath "/Project/PropertyGroup/Version").Node.InnerText
    }

    $packageVersion = GetSentryPackageVersion
    $packagePath = "src/Sentry/bin/Release/Sentry.$packageVersion.nupkg"
    if (-not (Test-Path env:CI))
    {
        Write-Host "Packaging $packagePath"
        dotnet pack src/Sentry -c Release --nologo -p:Version=$packageVersion | ForEach-Object { Write-Host $_ }
        if ($LASTEXITCODE -ne 0)
        {
            throw "Failed to package sentry."
        }
    }
    Write-Host "Using package $packagePath - $((Get-Item $packagePath).Length) bytes"
    Remove-Item -Path ./temp/packages -Recurse -Force -ErrorAction SilentlyContinue

    nuget add $packagePath -source ./temp/packages | ForEach-Object { Write-Host $_ }
    if ($LASTEXITCODE -ne 0)
    {
        throw "Failed to add package sentry to a local nuget source."
    }

    # We need to remove the package from cache or it won't re resolved properly
    Remove-Item -Path ~/.nuget/packages/sentry/$packageVersion -Recurse -Force -ErrorAction SilentlyContinue
}

Describe 'Console app (<framework>)' -ForEach @(
    @{ framework = "net7.0" },
    @{ framework = "net8.0" }
) {
    BeforeAll {
        $path = './temp/console-app'
        Remove-Item -Path $path -Recurse -Force
        dotnet new console --framework $framework --output $path
        if ($LASTEXITCODE -ne 0)
        {
            throw "Failed to create the test app from template."
        }
        @"
using Sentry;
using Sentry.Extensibility;
using Sentry.Protocol.Envelopes;

// Initialize the Sentry SDK.  (It is not necessary to dispose it.)
SentrySdk.Init(options =>
{
    options.Dsn = "http://key@127.0.0.1:9999/123";
    options.Debug = true;
    options.Transport = new FakeTransport();
});

throw new ApplicationException("Something happened!");

internal class FakeTransport : ITransport
{
    public virtual Task SendEnvelopeAsync(Envelope envelope, CancellationToken cancellationToken = default)
    {
        envelope.Serialize(Console.OpenStandardOutput(), null);
        return Task.CompletedTask;
    }
}

"@ | Out-File $path/Program.cs
        @"
<Project>
  <PropertyGroup>
    <PublishAot>true</PublishAot>
  </PropertyGroup>
</Project>
"@ | Out-File $path/Directory.build.props
        Push-Location $path
        try
        {
            $packageVersion = GetSentryPackageVersion
            dotnet add package sentry --source ../packages --version $packageVersion | ForEach-Object { Write-Host $_ }
            if ($LASTEXITCODE -ne 0)
            {
                throw "Failed to add package dependency to the test app project."
            }
        }
        finally
        {
            Pop-Location
        }

        # Publish once, then run the executable in actual tests.
        dotnet publish temp/console-app -c Release --nologo --framework $framework | ForEach-Object { Write-Host $_ }
        if ($LASTEXITCODE -ne 0)
        {
            throw "Failed to publish the test app project."
        }

        function runConsoleApp()
        {
            if ($IsMacOS)
            {
                $path = "./temp/console-app/bin/Release/$framework/osx-x64/publish/console-app"
            }
            elseif ($IsWindows)
            {
                $path = "./temp/console-app/bin/Release/$framework/win-x64/publish/console-app.exe"
            }
            else
            {
                $path = "./temp/console-app/bin/Release/$framework/linux-x64/publish/console-app"
            }

            Write-Host "::group::Executing $path"
            try
            {
                & $path | ForEach-Object {
                    Write-Host "  $_"
                    $_
                }
            }
            finally
            {
                Write-Host "::endgroup::"
            }
        }
    }

    # TODO migrate CLI integration tests to run on blank template app, such as the following code.
    # As is, this does an additional publish which we don't need to run app-specific integration tests.
    # It "uploads symbols and sources during build" {
    #     $result = RunDotnet 'publish' 'temp/console-app' $True $True $framework
    #     $result.UploadedDebugFiles() | Sort-Object -Unique | Should -Be @('console-app.pdb')
    #     $result.ScriptOutput | Should -AnyElementMatch 'Found 1 debug information file'
    #     $result.ScriptOutput | Should -AnyElementMatch 'Resolved source code for 1 debug information file'
    # }

    It "sends native debug images" {
        runConsoleApp | Should -AnyElementMatch '"debug_meta":{"images":\[{"type":"pe","image_addr":"0x[a-f0-9]+","image_size":[0-9]+,"debug_id":"[a-f0-9\-]+"'
    }

    It "sends stack trace with " {
        runConsoleApp | Should -AnyElementMatch '"stacktrace":{"frames":\[{"in_app":true,"image_addr":"0x[a-f0-9]+","instruction_addr":"0x[a-f0-9]+"}'
    }

    # TODO test the contents of the publish directory (there should be no sentry-native.a)
}
