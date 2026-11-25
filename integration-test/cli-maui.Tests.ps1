# This file contains test cases for https://pester.dev/
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. $PSScriptRoot/common.ps1

Describe 'MAUI (<framework>)' -ForEach @(
    # Each .NET for iOS SDK pins an Xcode version. Our CI builds use a runner matching
    # the latest. Only app builds are affected (not libraries/packages).
    @{ framework = $latestFramework }
) -Skip:($env:NO_MOBILE -eq "true" -or $env:SKIP_MAUI_TESTS -eq "true") {
    BeforeAll {
        ResetLocalPackages
        RegisterLocalPackage 'Sentry.Android.AssemblyReader'
        RegisterLocalPackage 'Sentry.Bindings.Android'
        RegisterLocalPackage 'Sentry.Extensions.Logging'
        RegisterLocalPackage 'Sentry.Maui'
        if ($IsMacOS)
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

        Push-Location $name
        try
        {
            dotnet remove "${name}.csproj" package 'Microsoft.Extensions.Logging.Debug' | ForEach-Object { Write-Host $_ }
            if ($LASTEXITCODE -ne 0)
            {
                throw "Failed to remove package"
            }
        }
        finally
        {
            Pop-Location
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

        # Match on the PDB GUID: IL stripping rewrites the stamp in the bundle's CodeView entries
        function UploadedPdbsNotInAppBundle($result, [string] $targetFramework)
        {
            $shippedGuids = Get-ChildItem "$name/bin/Release/$targetFramework" -Recurse -Directory -Filter "$name.app" `
            | Get-ChildItem -Recurse -Filter '*.dll' `
            | ForEach-Object {
                $reader = [System.Reflection.PortableExecutable.PEReader]::new([System.IO.File]::OpenRead($_.FullName))
                try
                {
                    $reader.ReadDebugDirectory() | Where-Object Type -EQ CodeView | ForEach-Object {
                        "$($reader.ReadCodeViewDebugDirectoryData($_).Guid)"
                    }
                }
                finally
                {
                    $reader.Dispose()
                }
            }
            $result.ScriptOutput `
            | Select-String 'json body: (\{.*"debug_id".*\})$' `
            | ForEach-Object { ($_.Matches[0].Groups[1].Value | ConvertFrom-Json -AsHashtable).Values } `
            | Where-Object { $_.name -like '*.pdb' -and $shippedGuids -notcontains $_.debug_id.Substring(0, 36) } `
            | ForEach-Object { $_.name }
        }
    }

    It "uploads symbols and sources for an Android build" {
        $result = RunDotnetWithSentryCLI 'build' 'maui-app' $True $True "$framework-android$androidTpv"
        Write-Host "UploadedDebugFiles: $($result.UploadedDebugFiles() | Out-String)"
        $result.UploadedDebugFiles() | Sort-Object -Unique | Should -Be @(
            '/proguard/a5fb4278-bcb5-4464-8585-d811dc3c3959.txt',
            'libsentry-android.so',
            'libsentry.so',
            'libsentrysupplemental.so',
            # .NET 11 splits the debug symbols into a separate .dbg.so
            'libxamarin-app.dbg.so',
            'libxamarin-app.so',
            'maui-app.pdb'
        )
        # The exact count differs per SDK version (went from 23 to 25 in .NET 11)
        # So only assert it's non-zero (same as we do for iOS)
        $nonZeroNumberRegex = '[1-9][0-9]*';
        $result.ScriptOutput | Should -AnyElementMatch "Found $nonZeroNumberRegex debug information files \($nonZeroNumberRegex with embedded sources\)"
    }

    It "passes the Sentry ProGuard rules to R8 for an Android build" {
        $r8Configuration = "$PWD/maui-app/obj/r8-configuration.txt".Replace('\', '/')
        "-printconfiguration $r8Configuration" | Out-File maui-app/print-r8-configuration.cfg
        @'
<Project>
  <ItemGroup>
    <ProguardConfiguration Include="print-r8-configuration.cfg" />
  </ItemGroup>
</Project>
'@ | Out-File maui-app/Directory.Build.targets
        try
        {
            RunDotnetWithSentryCLI 'build' 'maui-app' $False $False "$framework-android$androidTpv" | Out-Null
            Get-Content $r8Configuration | Should -AnyElementMatch 'io\.sentry\.protocol\.\*\*'
        }
        finally
        {
            Remove-Item maui-app/print-r8-configuration.cfg, maui-app/Directory.Build.targets, $r8Configuration -ErrorAction SilentlyContinue
        }
    }

    # Linux only, to keep the macOS and Windows legs short
    It "uploads the native debug file and preloads the NDK for an Android Native AOT build without --framework" -Skip:(!$IsLinux) {
        $aotName = 'android-aot-app'
        DotnetNew 'android' $aotName "$framework-android"
        @'
<Project>
  <PropertyGroup>
    <PublishAot>true</PublishAot>
    <RuntimeIdentifier>android-arm64</RuntimeIdentifier>
  </PropertyGroup>
</Project>
'@ | Out-File $aotName/Directory.Build.props
        AddPackageReference $aotName 'Sentry'

        $result = RunDotnetWithSentryCLI 'build' $aotName $True $False "$framework-android" -OmitFrameworkArgument
        $result.UploadedDebugFiles() | Should -Contain "lib$aotName.dbg.so"
        Get-Content "$aotName/obj/Release/$framework-android/android-arm64/android/AndroidManifest.xml" `
        | Should -AnyElementMatch 'io\.sentry\.ndk\.preload'
    }

    It "uploads symbols and sources for an iOS build" -Skip:(!$IsMacOS) {
        $result = RunDotnetWithSentryCLI 'build' 'maui-app' $True $True "$framework-ios$iosTpv"
        Write-Host "UploadedDebugFiles: $($result.UploadedDebugFiles() | Out-String)"
        $result.UploadedDebugFiles() | Sort-Object -Unique | Should -Be @(
            'libclrjit.dylib',
            'libcoreclr.dylib',
            'libmscordaccore.dylib',
            'libmscordbi.dylib',
            'libSystem.Globalization.Native.dylib',
            'libSystem.IO.Compression.Native.dylib',
            'libSystem.Native.dylib',
            'libSystem.Net.Security.Native.dylib',
            'libSystem.Security.Cryptography.Native.Apple.dylib',
            'libxamarin-dotnet-coreclr-debug.dylib',
            'libxamarin-dotnet-coreclr.dylib',
            'libxamarin-dotnet-debug.dylib',
            'libxamarin-dotnet.dylib',
            'maui-app',
            'maui-app.pdb',
            'maui-app.r2r',
            'Microsoft.iOS.pdb',
            'Microsoft.Maui.Controls.pdb',
            'Microsoft.Maui.Controls.Xaml.pdb',
            'Microsoft.Maui.Essentials.pdb',
            'Microsoft.Maui.Graphics.pdb',
            'Microsoft.Maui.pdb',
            'SentryObjC'
        )
        UploadedPdbsNotInAppBundle $result "$framework-ios$iosTpv" | Should -BeNullOrEmpty
        # The specific number of debug information files seems to change with different SDK - so we just check for non-zero
        $nonZeroNumberRegex = '[1-9][0-9]*';
        $result.ScriptOutput | Should -AnyElementMatch "Found $nonZeroNumberRegex debug information files \($nonZeroNumberRegex with embedded sources\)"
    }

    It "uploads symbols and sources for a Mac Catalyst build" -Skip:(!$IsMacOS) {
        $result = RunDotnetWithSentryCLI 'build' 'maui-app' $True $True "$framework-maccatalyst$iosTpv"
        Write-Host "UploadedDebugFiles: $($result.UploadedDebugFiles() | Out-String)"
        $result.UploadedDebugFiles() | Sort-Object -Unique | Should -Be @(
            'libclrjit.dylib',
            'libcoreclr.dylib',
            'libmscordaccore.dylib',
            'libmscordbi.dylib',
            'libSystem.Globalization.Native.dylib',
            'libSystem.IO.Compression.Native.dylib',
            'libSystem.Native.dylib',
            'libSystem.Net.Security.Native.dylib',
            'libSystem.Security.Cryptography.Native.Apple.dylib',
            'libxamarin-dotnet-coreclr-debug.dylib',
            'libxamarin-dotnet-coreclr.dylib',
            'libxamarin-dotnet-debug.dylib',
            'libxamarin-dotnet.dylib',
            'maui-app',
            'maui-app.pdb',
            'maui-app.r2r.dylib',
            'Microsoft.MacCatalyst.pdb',
            'Microsoft.Maui.Controls.pdb',
            'Microsoft.Maui.Controls.Xaml.pdb',
            'Microsoft.Maui.Essentials.pdb',
            'Microsoft.Maui.Graphics.pdb',
            'Microsoft.Maui.pdb',
            'SentryObjC'
        )
        UploadedPdbsNotInAppBundle $result "$framework-maccatalyst$iosTpv" | Should -BeNullOrEmpty
    }
}
