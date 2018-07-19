$ErrorActionPreference = "Stop"

# If this was solved: https://github.com/Microsoft/msbuild/issues/1333
# .NET CLI could be used instead of msbuild (i.e: dotnet test)

function GetMsbuild()
{
    if ($global:msbuild) { return $global:msbuild }

    if (Get-Command "msbuild.exe" -ErrorAction SilentlyContinue)
    {
        $msbuild = (Get-Command "msbuild.exe").Path
    }
    else
    {
        $msbuild = Get-ChildItem -Path "C:\Program Files (x86)\Microsoft Visual Studio\2017" `
            -Filter msbuild.exe -Recurse `
            | Select-Object -First 1 -ExpandProperty FullName
    }

    if (!$msbuild) { Write-Error "msbuild.exe not found!" }
    $global:msbuild = $msbuild
    Write-Host msbuild found at: $msbuild -ForegroundColor Green
    return $msbuild
}

function GetTestRunner()
{
    if ($global:testRunner) { return $global:testRunner }

    $xunit = "xunit.console.exe"
    $xunitDir = ".xunit\tools\net462\"
    $localxunit = "$xunitDir\$xunit"

    if (Get-Command $xunit -ErrorAction SilentlyContinue)
    {
        $testRunner = $xunit
    }
    elseif ([System.IO.File]::Exists($localxunit))
    {
        $testRunner = $localxunit
    }
    else
    {
        $file = "xunit.runner.console.2.4.0.zip"
        [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
        Invoke-WebRequest -Uri "https://www.nuget.org/api/v2/package/xunit.runner.console/2.4.0" -OutFile $file
        Expand-Archive $file -DestinationPath $xunitDir\..\..
        Remove-Item $file
        $testRunner = $localxunit
    }

    if (!$testRunner) { Write-Error "Test runner not found!" }
    $global:testRunner = $testRunner
    Write-Host test runner found at: $testRunner -ForegroundColor Green
    return $testRunner
}

Set-Variable -name msbuild -value (GetMsbuild)
& $msbuild /t:restore /p:Configuration=Release
if ($lastexitcode -ne 0) { Write-Error "Restore failed!" }

& $msbuild /t:build /p:Configuration=Release
if ($lastexitcode -ne 0) { Write-Error "Build failed!" }

& $msbuild /t:pack src\Sentry.EntityFramework\Sentry.EntityFramework.csproj /p:Configuration=Release
if ($lastexitcode -ne 0) { Write-Error "Pack failed!" }

Set-Variable -name runner -value (GetTestRunner)
& $runner  "test\Sentry.EntityFramework.Tests\bin\Release\net462\Sentry.EntityFramework.Tests.dll"
if ($lastexitcode -ne 0) { Set-Variable -name fail -value true }
