# This file contains test cases for https://pester.dev/
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. $PSScriptRoot/common.ps1

Describe 'MAUI (<framework>)' -ForEach @(
    @{ framework = $previousFramework }
) -Skip:($env:NO_MOBILE -eq "true") {
    BeforeAll {
        if ($env:NO_ANDROID -ne "true")
        {
            RegisterLocalPackage 'Sentry.Android.AssemblyReader'
            RegisterLocalPackage 'Sentry.Bindings.Android'
        }
        RegisterLocalPackage 'Sentry.Extensions.Logging'
        RegisterLocalPackage 'Sentry.Maui'
        if ($IsMacOS -and $env:NO_IOS -ne "true")
        {
            RegisterLocalPackage 'Sentry.Bindings.Cocoa'
        }

        $name = 'maui-app'
        $androidTpv = GetAndroidTpv $framework
        $iosTpv = GetIosTpv $framework

        DotnetNew 'maui' $name $framework

        # Workaround for the missing "ios" workload on Linux, see https://github.com/dotnet/maui/pull/18580
        $tfs = $IsMacos ? "$framework-android$androidTpv;$framework-ios$iosTpv;$framework-maccatalyst$iosTpv" : "$framework-android$androidTpv"
        (Get-Content $name/$name.csproj) -replace '<TargetFrameworks>[^<]+</TargetFrameworks>', "<TargetFrameworks>$tfs</TargetFrameworks>" | Set-Content $name/$name.csproj

        dotnet remove $name/$name.csproj package 'Microsoft.Extensions.Logging.Debug' | ForEach-Object { Write-Host $_ }
        if ($LASTEXITCODE -ne 0)
        {
            throw "Failed to remove package"
        }

        if (Test-Path env:CI)
        {
            dotnet workload restore $name/$name.csproj | ForEach-Object { Write-Host $_ }
            if ($LASTEXITCODE -ne 0)
            {
                throw "Failed to restore workloads."
            }
        }

        AddPackageReference $name 'Sentry.Maui'
    }

    It "uploads symbols and sources for an Android build" -Skip:($env:NO_ANDROID -eq "true") {
        $result = RunDotnetWithSentryCLI 'build' 'maui-app' $True $True "$framework-android$androidTpv"
        Write-Host "UploadedDebugFiles: $($result.UploadedDebugFiles() | Out-String)"
        $result.UploadedDebugFiles() | Sort-Object -Unique | Should -Be @(
            'libsentry-android.so',
            'libsentry.so',
            'libsentrysupplemental.so',
            'libxamarin-app.so',
            'maui-app.pdb'
        )
        $result.ScriptOutput | Should -AnyElementMatch 'Uploaded a total of 1 new mapping files'
        $result.ScriptOutput | Should -AnyElementMatch "Found 23 debug information files \(1 with embedded sources\)"
    }

    It "uploads symbols and sources for an iOS build" -Skip:(!$IsMacOS -or $env:NO_IOS -eq "true") {
        $result = RunDotnetWithSentryCLI 'build' 'maui-app' $True $True "$framework-ios$iosTpv"
        Write-Host "UploadedDebugFiles: $($result.UploadedDebugFiles() | Out-String)"
        $result.UploadedDebugFiles() | Sort-Object -Unique | Should -Be @(
            'libmono-component-debugger.dylib',
            'libmono-component-diagnostics_tracing.dylib',
            'libmono-component-hot_reload.dylib',
            'libmono-component-marshal-ilgen.dylib',
            'libmonosgen-2.0.dylib',
            'libSystem.Globalization.Native.dylib',
            'libSystem.IO.Compression.Native.dylib',
            'libSystem.Native.dylib',
            'libSystem.Net.Security.Native.dylib',
            'libSystem.Security.Cryptography.Native.Apple.dylib',
            'libxamarin-dotnet-debug.dylib',
            'libxamarin-dotnet.dylib',
            'maui-app',
            'maui-app.pdb',
            'Microsoft.iOS.pdb',
            'Microsoft.Maui.Controls.pdb',
            'Microsoft.Maui.Controls.Xaml.pdb',
            'Microsoft.Maui.Essentials.pdb',
            'Microsoft.Maui.Graphics.pdb',
            'Microsoft.Maui.pdb',
            'Sentry'
        )
        # The specific number of debug information files seems to change with different SDK - so we just check for non-zero
        $nonZeroNumberRegex = '[1-9][0-9]*';
        $result.ScriptOutput | Should -AnyElementMatch "Found $nonZeroNumberRegex debug information files \($nonZeroNumberRegex with embedded sources\)"
    }
}
