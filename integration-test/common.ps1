. $PSScriptRoot/pester.ps1

$global:longTermFramework = 'net8.0'
$global:previousFramework = 'net9.0'
$global:latestFramework = 'net10.0'
$global:currentFrameworks = @($longTermFramework, $previousFramework, $latestFramework)

AfterAll {
    Pop-Location
}

BeforeAll {
    Push-Location $PSScriptRoot
    $env:SENTRY_LOG_LEVEL = 'debug';

    function GetAndroidTpv($framework)
    {
        switch ($framework) {
            'net9.0' { return '35.0' }   # matches PreviousAndroidTfm (net9.0-android35.0)
            'net10.0' { return '36.0' }  # matches LatestAndroidTfm (net10.0-android36.0)
            default { throw "Unsupported framework '$framework' for Android target platform version." }
        }
    }

    function GetIosTpv($framework)
    {
        switch ($framework) {
            'net9.0' { return '18.0' }   # matches PreviousIosTfm / PreviousMacCatalystTfm
            'net10.0' { return '26' }    # aligns with ios26 / maccatalyst26
            default { throw "Unsupported framework '$framework' for iOS target platform version." }
        }
    }

    function GetSentryPackageVersion()
    {
        # Read version directly from Directory.Build.props
        $propsFile = Join-Path $PSScriptRoot '..\Directory.Build.props'

        if (-not (Test-Path $propsFile)) {
            throw "Directory.Build.props not found at $propsFile"
        }

        # Parse the props file using PowerShell XML parsing
        Write-Host "Parsing props file as XML..."
        [xml]$propsXml = Get-Content $propsFile

        # Look for VersionPrefix and VersionSuffix in PropertyGroup elements
        $versionPrefix = ""
        $versionSuffix = ""

        foreach ($propGroup in $propsXml.Project.PropertyGroup) {
            if ($propGroup.PSObject.Properties["VersionPrefix"]) {
                $versionPrefix = $propGroup.VersionPrefix
                Write-Host "Found VersionPrefix: '$versionPrefix'"
            }

            # For VersionSuffix, we need to be careful about conditions
            # Only use VersionSuffix if it's not in a conditional PropertyGroup
            # or if it's explicitly set (not the 'dev' fallback for non-Release)
            if ($propGroup.PSObject.Properties["VersionSuffix"]) {
                $condition = $null
                if ($propGroup.PSObject.Properties["Condition"]) {
                    $condition = $propGroup.Condition
                    Write-Host "Ignoring VersionSuffix: '$($propGroup.VersionSuffix)' with condition: '$condition'"
                    # Skip conditional VersionSuffix as we're building in Release mode
                }
                else {
                    # No condition - this is the explicit VersionSuffix we want
                    $versionSuffix = $propGroup.VersionSuffix
                    Write-Host "Found VersionSuffix: '$versionSuffix'"
                }
            }
        }

        if (-not $versionPrefix) {
            throw "Could not find VersionPrefix in $propsFile"
        }

        # Combine prefix and suffix
        $fullVersion = if ($versionSuffix) { "$versionPrefix-$versionSuffix" } else { $versionPrefix }
        Write-Host "Full Version: '$fullVersion'"

        return $fullVersion
    }

    function RegisterLocalPackage([string] $name)
    {
        $packageVersion = GetSentryPackageVersion
        $packagePath = "$PSScriptRoot/../src/$name/bin/Release/$name.$packageVersion.nupkg"
        if (-not (Test-Path env:CI))
        {
            Write-Host "Packaging $name, expected output path: $packagePath"
            dotnet pack "$PSScriptRoot/../src/$name" -c Release --nologo -p:Version=$packageVersion -p:IsPackable=true | ForEach-Object { Write-Host $_ }
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

    Remove-Item -Path "$PSScriptRoot/packages" -Recurse -Force -ErrorAction SilentlyContinue
    New-Item -ItemType Directory -Path "$PSScriptRoot/packages" | Out-Null
    RegisterLocalPackage 'Sentry'

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
