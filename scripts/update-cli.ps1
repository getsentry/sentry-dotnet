$file = 'Directory.Build.props'
$property = 'SentryCLIVersion'
$repo = 'https://github.com/getsentry/sentry-cli'

. "$PSScriptRoot/update-project-xml.ps1" @args

if ("$($args[0])" -eq "set-version")
{
    $specUrl = "https://release-registry.services.sentry.io/apps/sentry-cli/$value";
    Write-Host "Loading $specUrl"
    $spec = (Invoke-WebRequest -Uri $specUrl).Content | ConvertFrom-Json

    $hashesFile = "$PSScriptRoot/../src/Sentry/Sentry.csproj"
    $content = Get-Content $hashesFile
    Select-Xml -Path $hashesFile -XPath 'Project/Target/ItemGroup/SentryCLIDownload' | ForEach-Object {
        $cliFile = $_.Node.Attributes.GetNamedItem('Include').Value
        $oldHash = $_.Node.Attributes.GetNamedItem('FileHash').Value
        if ("$cliFile" -eq "")
        {
            throw "$hashesFile - Failed to read 'Include' attribute on 'SentryCLIDownload'"
        }
        if ("$oldHash" -eq "")
        {
            throw "$hashesFile - Failed to read 'FileHash' attribute on 'SentryCLIDownload'"
        }
        $newHash = $spec.files."$cliFile".checksums.'sha256-hex'
        Write-Host "Updating hash for $cliFile from $oldHash to $newHash"
        $content = $content.Replace($oldHash, $newHash)
    }
    $content | Out-File $hashesFile
}
