param(
    [Parameter(Mandatory = $true)][string] $ServerUri
)

$sample = 'Sentry.Samples.Maui'
$rootDir = "$(Get-Item $PSScriptRoot/../../)"

dotnet build "samples/$sample/$sample.csproj" -c Release --no-restore --nologo `
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
    elseif ($_ -match "\[[0-9/]+\]")
    {
        # Skip lines like `[102/103] Sentry.Samples.Maui.dll -> Sentry.Samples.Maui.dll.so`
    }
    else
    {
        "$_". `
            Replace($rootDir, '').  `
            Replace('\', '/')
    }
}

