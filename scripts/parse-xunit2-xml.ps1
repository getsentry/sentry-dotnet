param([string] $File)

Set-StrictMode -Version Latest

[xml]$xml = Get-Content $File

function ElementText([System.Xml.XmlElement] $element)
{
    $element.InnerText.Replace('\n', "`n").Replace('\t', "`t").Replace('\"', '"').Trim()
}

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

            $failures += "#### $($test.name.Replace('\"', '"'))"
            if ($test.result -eq "Skip")
            {
                $failures += " - Skipped`n"
                $failures += "$(ElementText $test.reason)"
            }
            else
            {
                $failures += " - $($test.result)ed`n"
                if ($test.PSobject.Properties.name -match "output")
                {
                    $failures += "$(ElementText $test.output)`n"
                }
                $failures += '```' + "`n"
                $failures += "$(ElementText $test.failure.message)`n"
                $failures += "$(ElementText $test.failure['stack-trace'])`n"
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
