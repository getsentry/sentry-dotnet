# Note: this is not meant to be invoked directly. See for example `./update-cli.ps1` for usage.
param([string] $action, [string] $value)

Set-StrictMode -Version latest
$ErrorActionPreference = "Stop"

$file = "$PSScriptRoot/../$file"
$currentVersion = Select-Xml -Path $file -XPath "Project/PropertyGroup/$property" | ForEach-Object { $_.Node.InnerXML }

switch ($action)
{
    "get-version"
    {
        $currentVersion
    }
    "get-repo"
    {
        $repo
    }
    "set-version"
    {
        $content = Get-Content $file
        $newContent = $content.Replace("<$property>$currentVersion</$property>", "<$property>$value</$property>")
        if (($content -eq $newContent) -and ("$currentVersion" -ne "$value"))
        {
            throw "Failed to update version in $file - the new content is the same"
        }
        $newContent | Out-File $file
    }
    Default { throw "Unknown action $action" }
}
