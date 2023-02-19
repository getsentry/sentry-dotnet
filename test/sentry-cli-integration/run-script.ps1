param(
    [Parameter(Mandatory = $true)][string] $ServerUri
)

$env:SENTRY_URL = $ServerUri
dotnet build samples/Sentry.Samples.Maui/Sentry.Samples.Maui.csproj -c Release --no-restore --nologo `
    /p:CopyLocalLockFileAssemblies=true /p:SentryOrg=org /p:SentryProject=project