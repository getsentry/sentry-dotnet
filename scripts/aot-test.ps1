$package = Get-ChildItem -Path "$PSScriptRoot/../src/Sentry/bin/Release/Sentry.*.nupkg" -File | Select-Object -First 1
if (-not $package) {
  Write-Error "No NuGet package found in src/Sentry/bin/Release."
  exit 1
}

$tempDir = Join-Path ([System.IO.Path]::GetTempPath()) ([System.IO.Path]::GetRandomFileName())
Write-Host "Testing $package in $tempDir"
dotnet --info

# Setup local NuGet source
$localPackages = Join-Path $tempDir "packages"
New-Item -ItemType Directory -Path $localPackages -Force | Out-Null
Copy-Item $package $localPackages
$localConfig = Join-Path $tempDir "nuget.conf"
Copy-Item $PSScriptRoot/../integration-test/nuget.config $localConfig
dotnet nuget list source --configfile $localConfig

# Setup test project
Set-Location $tempDir
dotnet new console --aot --name hello-sentry --output .
dotnet add package Sentry --prerelease --source $localPackages
@"
SentrySdk.Init(options =>
{
    options.Dsn = "https://foo@sentry.invalid/42";
    options.Debug = true;
});
Console.WriteLine("Hello, Sentry!");
"@ | Set-Content Program.cs

# Test AOT
dotnet publish
$tfm = (Get-ChildItem -Path "bin/Release" -Directory | Select-Object -First 1).Name
$rid = (Get-ChildItem -Path "bin/Release/$tfm" -Directory | Select-Object -First 1).Name
& "bin/Release/$tfm/$rid/publish/hello-sentry"

# Test Container
if ($IsLinux -and (Get-Command docker -ErrorAction SilentlyContinue)) {
  dotnet publish -p:EnableSdkContainerSupport=true -t:PublishContainer
  docker run hello-sentry
}
