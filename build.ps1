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

    $nunit = "nunit3-console.exe"
    $nunitDir = ".nunit"
    $localNunit = "$nunitDir\$nunit"

    if (Get-Command $nunit -ErrorAction SilentlyContinue)
    {
        $testRunner = $nunit
    }
    elseif ([System.IO.File]::Exists($localNunit))
    {
        $testRunner = $localNunit
    }
    else
    {
        $file = "NUnit.Console-3.8.0.zip"
        [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
        Invoke-WebRequest -Uri "https://github.com/nunit/nunit-console/releases/download/3.8/$file" -OutFile $file
        Expand-Archive $file -DestinationPath $nunitDir
        Remove-Item $file
        $testRunner = $localNunit
    }

    if (!$testRunner) { Write-Error "Test runner not found!" }
    $global:testRunner = $testRunner
    Write-Host test runner found at: $testRunner -ForegroundColor Green
    return $testRunner
}

function TestAllTargets($project)
{
    $fail = False
    [xml]$projectContent = Get-Content $project
    foreach ($target in $projectContent.SelectSingleNode("//TargetFrameworks").InnerText.Split(";"))
    {
        Write-Host Testing target $target -ForegroundColor Green
        if ($target -eq "net35")
        {
            Set-Variable -name runner -value (GetTestRunner)
            $dll = Split-Path (Split-Path $project -Parent) -Leaf
            $dll = "test\$dll\bin\Release\$target\$dll.dll"
            Write-Host With NUnit and DLL: $dll -ForegroundColor Green
            & $runner $dll
            if ($lastexitcode -ne 0) { Set-Variable -name fail -value true }
        }
        else
        {
            dotnet test -c Release -f $target $project
            if ($lastexitcode -ne 0) { Set-Variable -name fail -value true }
        }
    }

    if ($fail) { Write-Error "Tests failed." }
}

Set-Variable -name msbuild -value (GetMsbuild)
& $msbuild /t:restore,build,pack  /p:IncludeSymbols=true  /p:Configuration=Release
if ($lastexitcode -ne 0) { Write-Error "Build failed!" }

$test = "test\Sentry.PlatformAbstractions.Tests\Sentry.PlatformAbstractions.Tests.csproj"
TestAllTargets($test)
