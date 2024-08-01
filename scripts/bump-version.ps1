param([string] $newVersion)

$utf8NoBomEncoding = New-Object System.Text.UTF8Encoding($False) 
function Replace-TextInFile {
    param([string] $filePath, [string] $pattern, [string] $replacement)

    $content = [IO.File]::ReadAllText($filePath)
    $content = [Text.RegularExpressions.Regex]::Replace($content, $pattern, $replacement)
    [IO.File]::WriteAllText($filePath, $content, $utf8NoBomEncoding)
}

# Version of .NET assemblies:
Replace-TextInFile "$PSScriptRoot/../Directory.Build.props" '(?<=<VersionPrefix>)(.*?)(?=</VersionPrefix>)' $newVersion
