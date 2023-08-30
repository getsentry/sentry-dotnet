Set-StrictMode -Version Latest

$pathToCheck = $args[0]

$ErrorActionPreference = 'Stop'

# Any value will be truthy in PS so if our check returns something, we've got tracked changes
$changes = git diff --name-only $pathToCheck
if($changes){
    Write-Output "Path: $pathToCheck"
    Write-Output "Changes:`n$changes"
    Write-Error 'Dirty files detected.' `
        -CategoryActivity Error -ErrorAction Stop
}
else
{
    Write-Output '$pathToCheck matches HEAD.'
}

