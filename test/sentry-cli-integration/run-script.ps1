param(
    [Parameter(Mandatory = $true)][string] $ServerUri
)

$sample = 'Sentry.Samples.Console.Basic'
$rootDir = "$(Get-Item $PSScriptRoot/../../)"

dotnet build "samples/$sample/$sample.csproj" -c Release --framework net6.0 --no-restore --nologo `
    /p:CopyLocalLockFileAssemblies=true `
    /p:SentryOrg=org `
    /p:SentryProject=project `
    /p:SentryUrl=$ServerUri `
    /p:SentryAuthToken=dummy `
| ForEach-Object {
    if ($_ -match "^Time Elapsed ")
    {
        "Time Elapsed [value removed]"
    }
    else
    {
        "$_". `
            Replace($rootDir, '').  `
            Replace('\', '/')
    }
}

