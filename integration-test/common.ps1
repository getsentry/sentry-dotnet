. $PSScriptRoot/pester.ps1

$global:longTermFramework = 'net10.0'
$global:previousFramework = 'net10.0'
$global:latestFramework = 'net11.0'
$global:currentFrameworks = @($previousFramework, $latestFramework)

AfterAll {
    Pop-Location
}

BeforeAll {
    Push-Location $PSScriptRoot
    $env:SENTRY_LOG_LEVEL = 'debug';

    function GetAndroidTpv($framework)
    {
        switch ($framework) {
            'net10.0' { return '36.0' }  # matches PreviousAndroidTfm (net10.0-android36.0)
            'net11.0' { return '37.0' }  # matches LatestAndroidTfm (net11.0-android37.0)
            default { throw "Unsupported framework '$framework' for Android target platform version." }
        }
    }

    function GetIosTpv($framework)
    {
        switch ($framework) {
            'net10.0' { return '26' }     # matches PreviousIosTfm / PreviousMacCatalystTfm
            'net11.0' { return '27.0' }   # matches LatestIosTfm / LatestMacCatalystTfm
            default { throw "Unsupported framework '$framework' for iOS target platform version." }
        }
    }

    function GetSentryPackageVersion()
    {
        if (-not $script:SentryPackageVersion)
        {
            if (Test-Path env:CI)
            {
                # The publishing build runs these tests in a sparse checkout holding only the built packages
                $package = Get-ChildItem "$PSScriptRoot/../src/Sentry/bin/Release/Sentry.*.nupkg" -File | Select-Object -First 1
                if (-not $package)
                {
                    throw "No Sentry package found in src/Sentry/bin/Release"
                }
                $version = $package.Name -replace '^Sentry\.(.+)\.nupkg$', '$1'
            }
            else
            {
                $output = dotnet msbuild "$PSScriptRoot/../src/Sentry/Sentry.csproj" -p:Configuration=Release -getProperty:Version 2>&1 | ForEach-Object { "$_".Trim() }
                $version = $output | Where-Object { $_ -match '^\d+\.\d+\.\d+(-[0-9A-Za-z.-]+)?$' } | Select-Object -Last 1
                if ($LASTEXITCODE -ne 0 -or -not $version)
                {
                    throw "Could not evaluate the Sentry package version (exit code $LASTEXITCODE):`n$($output -join "`n")"
                }
            }
            $script:SentryPackageVersion = $version
            Write-Host "Sentry package version: $script:SentryPackageVersion"
        }
        return $script:SentryPackageVersion
    }

    function RegisterLocalPackage([string] $name)
    {
        $packageVersion = GetSentryPackageVersion
        $packagePath = "$PSScriptRoot/../src/$name/bin/Release/$name.$packageVersion.nupkg"
        if (-not (Test-Path env:CI))
        {
            Write-Host "Packaging $name, expected output path: $packagePath"
            # Exclude Windows TFMs when not packing on Windows. MAUI 11 generates a
            # .pri via the Windows-only makepri.exe. The Windows assets
            # aren't needed by the mobile and console apps these tests exercise.
            $packArgs = @("$PSScriptRoot/../src/$name", '-c', 'Release', '--nologo',
                "-p:Version=$packageVersion", '-p:IsPackable=true')
            if (-not $IsWindows)
            {
                $packArgs += '-p:NO_WINDOWS=true'
            }
            dotnet pack $packArgs | ForEach-Object { Write-Host $_ }
            if ($LASTEXITCODE -ne 0)
            {
                throw "Failed to package $name."
            }
        }
        Write-Host "Using package $packagePath - $((Get-Item $packagePath).Length) bytes"

        dotnet nuget push $packagePath --source integration-test | ForEach-Object { Write-Host $_ }
        if ($LASTEXITCODE -ne 0)
        {
            throw "Failed to add package $name to a local nuget source."
        }

        # We need to remove the package from cache or it won't re resolved properly
        Remove-Item -Path ~/.nuget/packages/$($name.ToLower())/$packageVersion -Recurse -Force -ErrorAction SilentlyContinue
    }

    function ResetLocalPackages()
    {
        Remove-Item -Path "$PSScriptRoot/packages" -Recurse -Force -ErrorAction SilentlyContinue
        New-Item -ItemType Directory -Path "$PSScriptRoot/packages" | Out-Null
        RegisterLocalPackage 'Sentry'
    }

    function RunDotnetWithSentryCLI([string] $action, [string]$project, [bool]$Symbols, [bool]$Sources, [string]$TargetFramework)
    {
        $rootDir = $PSScriptRoot

        $result = Invoke-SentryServer {
            Param([string]$url)
            Write-Host "::group::${action}ing $project"
            try
            {
                dotnet $action $project -flp:logfile=build.log `
                    -c Release `
                    --nologo `
                    --framework $TargetFramework `
                    /p:SentryUploadSymbols=$Symbols `
                    /p:SentryUploadSources=$Sources `
                    /p:SentryOrg=org `
                    /p:SentryProject=project `
                    /p:SentryUrl=$url `
                    /p:SentryAuthToken=dummy `
                    /p:SentryProGuardUUID=a5fb4278-bcb5-4464-8585-d811dc3c3959 `
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
                        "$_".Replace('\', '/').Replace($rootDir, '')
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
            $result.ScriptOutput | Should -AnyElementMatch "$((Get-Item $project).Basename) -> .*$project/bin/Release/$TargetFramework/.*publish.*"
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
            dotnet restore /p:CheckEolTargetFramework=false | ForEach-Object { Write-Host $_ }
            if ($LASTEXITCODE -ne 0)
            {
                throw "Failed to restore the test app project."
            }

            $packageVersion = GetSentryPackageVersion
            dotnet add package $package --source $PSScriptRoot/packages --version $packageVersion --no-restore | ForEach-Object { Write-Host $_ }
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
    function DotnetNew([string] $type, [string] $name, [string] $framework)
    {
        Remove-Item -Path $name -Recurse -Force -ErrorAction SilentlyContinue
        dotnet new $type --output $name --framework $framework | ForEach-Object { Write-Host $_ }
        if ($LASTEXITCODE -ne 0)
        {
            throw "Failed to create the test app '$name' from template '$type'."
        }

        if ($type -eq 'maui')
        {
                            @"
<Project>
  <PropertyGroup>
      <SentryUploadAndroidProguardMapping>true</SentryUploadAndroidProguardMapping>
      <AndroidLinkTool Condition=`" '`$(AndroidLinkTool)' == '' `">r8</AndroidLinkTool>
      <AndroidDexTool Condition=`" '`$(AndroidDexTool)' == '' `">d8</AndroidDexTool>
      <MtouchUseLlvm Condition=`"'`$(Configuration)' == 'Release'`">false</MtouchUseLlvm>
  </PropertyGroup>
</Project>
"@ | Out-File $name/Directory.Build.props
        }

        if ($type -eq 'console')
        {
            AddPackageReference $name 'Sentry'
            @"
<Project>
  <PropertyGroup>
    <PublishAot>true</PublishAot>
  </PropertyGroup>
</Project>
"@ | Out-File $name/Directory.Build.props
        }
    }
}
