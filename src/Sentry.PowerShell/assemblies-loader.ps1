$dir = Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) 'lib'

# TODO move libs to this package and remove the following code
$dir = Join-Path $dir '../../Sentry/bin/Debug'

function GetTFM
{
    # Source https://learn.microsoft.com/en-us/powershell/scripting/whats-new/differences-from-windows-powershell?view=powershell-7.4#net-framework-vs-net-core
    # PowerShell 7.4 - Built on .NET 8.0
    # PowerShell 7.3 - Built on .NET 7.0
    # PowerShell 7.2 (LTS-current) - Built on .NET 6.0 (LTS-current)
    # PowerShell 7.1 - Built on .NET 5.0
    # PowerShell 7.0 (LTS) - Built on .NET Core 3.1 (LTS)
    # PowerShell 6.2 - Built on .NET Core 2.1
    # PowerShell 6.1 - Built on .NET Core 2.1
    # PowerShell 6.0 - Built on .NET Core 2.0
    if ($PSVersionTable.PSVersion -ge '7.4')
    {
        return 'net8.0'
    }
    elseif ($PSVersionTable.PSVersion -ge '7.2')
    {
        return 'net6.0'
    }
    elseif ($PSVersionTable.PSVersion -ge '6.0')
    {
        return 'netstandard2.0'
    }
    else
    {
        return 'net462'
    }
}

$dir = Join-Path $dir (GetTFM)
$lib = Join-Path $dir 'Sentry.dll'

# Check if the assembly is already loaded.
$type = 'Sentry.SentrySdk' -as [type]
if ($type)
{
    $loadedAsssembly = $type.Assembly
    $expectedAssembly = [Reflection.Assembly]::LoadFile($lib)

    if ($loadedAsssembly.ToString() -ne $expectedAssembly.ToString())
    {
        throw "Sentry assembly is already loaded but it's not the expected version.
        Found:    ($loadedAsssembly), location: $($loadedAsssembly.Location)
        Expected: ($expectedAssembly), location: $($expectedAssembly.Location)"
    }
}
else
{
    # TODO remove write-output & pipe output of LoadFrom() to out-null if we don't wont users to see it.
    Write-Output "Loading Sentry assembly from $lib"
    [Reflection.Assembly]::LoadFrom($lib)
}
