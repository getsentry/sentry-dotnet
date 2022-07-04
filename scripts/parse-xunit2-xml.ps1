param([string] $File)

Set-StrictMode -Version Latest

[xml]$xml = Get-Content $File

$summary = "## Summary`n`n"
$summary += "| Assembly | Passed | Failed | Skipped |`n"
$summary += "| -------- | -----: | -----: | ------: |`n"
$failures = ""
foreach ($assembly in $xml.assemblies.assembly)
{
    $summary += "| $($assembly.name) | $($assembly.passed) | $($assembly.failed) | $($assembly.skipped) |`n"

    if ($assembly.failed -gt 0)
    {
        $failures += "### $($assembly.name)`n"
        foreach ($test in $assembly.collection.test)
        {
            if ($test.result -eq "Pass")
            {
                continue
            }

            if ($test.result -eq "Skip")
            {
                $failures += "#### $($test.name) - Skipped`n"
                $failures += "$($test.reason.InnerText)"
            }
            else
            {
                $failures += "#### $($test.name) - $($test.result)ed`n"
                $failures += '```' + "`n"
                $failures += "$($test.failure.message.InnerText)`n"
                $failures += "$($test.failure['stack-trace'].InnerText)`n"
                $failures += '```'
            }
            $failures += "`n"
        }
    }
}

$summary

if ($failures.Length -gt 0)
{
    "## Unsuccessful tests`n$failures"
}
