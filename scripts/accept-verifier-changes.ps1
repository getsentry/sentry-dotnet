# Similar to `dotnet verify accept` but doesn't create new runtime-specific ".verified" files if a common one exists.
param([switch] $DryRun)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$receivedFiles = Get-ChildItem . -Recurse -File -Include "*.received.txt"

$receivedFiles | ForEach-Object {
    $nameWithRuntime = ($_.BaseName.Split(".") | Select-Object -SkipLast 1) -join "."
    $nameWithoutRuntime = ($_.BaseName.Split(".") | Select-Object -SkipLast 2) -join "."
    $ext = ".verified.txt"

    $targetFile = (Test-Path "$($_.Directory)/$nameWithoutRuntime$ext") `
        ? "$($_.Directory)/$nameWithoutRuntime$ext" `
        : "$($_.Directory)/$nameWithRuntime$ext"

    Write-Host "Updating $($targetFile.Replace((Get-Item .).FullName, ''))"
    if (-not ($DryRun))
    {
        Move-Item $_ $targetFile -Force
    }
}
