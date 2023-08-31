param(
    [string]$PathToCheck,
    [string]$GuidanceOnFailure = "Dirty files detected."
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# Any value will be truthy in PS so if our check returns something, we've got tracked changes
$changes = git diff --name-only $PathToCheck
if($changes){
    Write-Debug "Path: $PathToCheck"
    Write-Debug "Changes:`n$changes"
    Write-Error "$GuidanceOnFailure" `
        -CategoryActivity Error -ErrorAction Stop
}
else
{
    Write-Debug '$PathToCheck matches HEAD.'
}

