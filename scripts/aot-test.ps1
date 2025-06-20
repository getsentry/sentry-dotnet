$package = Get-ChildItem -Path "$PSScriptRoot/../src/Sentry/bin/Release/Sentry.*.nupkg" -File | Select-Object -First 1
if (-not $package) {
  Write-Error "No NuGet package found in src/Sentry/bin/Release."
  exit 1
}

$tempDir = Join-Path ([System.IO.Path]::GetTempPath()) ([System.IO.Path]::GetRandomFileName())
New-Item -ItemType Directory -Path $tempDir -Force | Out-Null
Set-Location $tempDir
Write-Host "Testing $package in $tempDir"
Write-Host "::group::.NET info"
dotnet --info
Write-Host "::endgroup::"

Write-Host "::group::Setup local NuGet source"
$localPackages = Join-Path $tempDir "packages"
New-Item -ItemType Directory -Path $localPackages -Force | Out-Null
Copy-Item $package $localPackages
$localConfig = Join-Path $tempDir "nuget.conf"
Copy-Item $PSScriptRoot/../integration-test/nuget.config $localConfig
dotnet nuget list source --configfile $localConfig
Write-Host "::endgroup::"

Write-Host "::group::Create test project"
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
Write-Host "::endgroup::"

Write-Host "::group::Test PublishAot"
dotnet publish -c Release -v:detailed
$tfm = (Get-ChildItem -Path "bin/Release" -Directory | Select-Object -First 1).Name
$rid = (Get-ChildItem -Path "bin/Release/$tfm" -Directory | Select-Object -First 1).Name
& "bin/Release/$tfm/$rid/publish/hello-sentry"
Write-Host "::endgroup::"

if ($IsLinux -and (Get-Command docker -ErrorAction SilentlyContinue)) {
  Write-Host "::group::Test PublishContainer"
  dotnet publish -p:EnableSdkContainerSupport=true -t:PublishContainer -v:detailed
  docker run --rm hello-sentry
  Write-Host "::endgroup::"
}
