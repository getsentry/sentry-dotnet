Set-StrictMode -Version Latest

$ErrorActionPreference = 'Stop'

# Any value will be truthy in PS so if our check returns something, we've got tracked changes
$changes = git status --untracked-files=no --porcelain
Write-Output $changes
if($changes){
    Write-Error 'Git working directory is dirty' `
    -CategoryActivity Error -ErrorAction Stop
}
else
{
    Write-Output 'Working directory clean excluding untracked files'
}

