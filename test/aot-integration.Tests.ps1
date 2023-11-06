# This file contains test cases for https://pester.dev/
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. $PSScriptRoot/integration-test-setup.ps1

Describe 'Console app (<framework>)' -ForEach @(
    @{ framework = "net7.0" },
    @{ framework = "net8.0" }
) {
    BeforeAll {
        $path = './temp/console-app'
        DotnetNew 'console' $path $framework
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

    It "sends native debug images" {
        runConsoleApp | Should -AnyElementMatch '"debug_meta":{"images":\[{"type":"pe","image_addr":"0x[a-f0-9]+","image_size":[0-9]+,"debug_id":"[a-f0-9\-]+"'
    }

    It "sends stack trace with " {
        runConsoleApp | Should -AnyElementMatch '"stacktrace":{"frames":\[{"in_app":true,"image_addr":"0x[a-f0-9]+","instruction_addr":"0x[a-f0-9]+"}'
    }

    # TODO test the contents of the publish directory (there should be no sentry-native.a)
}
