# https://learn.microsoft.com/en-us/powershell/scripting/developer/module/how-to-write-a-powershell-module-manifest
@{
    # Script module or binary module file associated with this manifest.
    # RootModule = ''

    # Version number of this module.
    ModuleVersion        = '0.0.1'

    # Supported PSEditions
    CompatiblePSEditions = @('Desktop', 'Core')

    # ID used to uniquely identify this module
    GUID                 = '4062b4a0-74d3-4aee-a3ec-9889342d4025'

    # Author of this module
    Author               = 'Sentry'

    # Company or vendor of this module
    CompanyName          = 'Sentry'

    # Copyright statement for this module
    Copyright            = '(c) Sentry. All rights reserved.'

    # Description of the functionality provided by this module
    Description          = 'An error reporting module that sends reports to Sentry.io'

    # Minimum version of the PowerShell engine required by this module
    PowerShellVersion    = '5.0'

    # Script files (.ps1) that are run in the caller's environment prior to importing this module.
    ScriptsToProcess     = @('assemblies-loader.ps1')

    # Functions to export from this module, for best performance, do not use wildcards and do not delete the entry, use an empty array if there are no functions to export.
    FunctionsToExport    = @()

    # Cmdlets to export from this module, for best performance, do not use wildcards and do not delete the entry, use an empty array if there are no cmdlets to export.
    CmdletsToExport      = @()

    # Variables to export from this module
    VariablesToExport    = ''

    # Aliases to export from this module, for best performance, do not use wildcards and do not delete the entry, use an empty array if there are no aliases to export.
    AliasesToExport      = @()

    # Private data to pass to the module specified in RootModule/ModuleToProcess. This may also contain a PSData hashtable with additional module metadata used by PowerShell.
    PrivateData          = @{
        PSData = @{
            # Tags applied to this module. These help with module discovery in online galleries.
            Tags         = @('Sentry', 'PSEdition_Core', 'PSEdition_Desktop', 'Windows', 'Linux', 'macOS')


            # A URL to the license for this module.
            LicenseUri   = 'https://raw.githubusercontent.com/getsentry/sentry-dotnet/main/LICENSE'

            # A URL to the main website for this project.
            ProjectUri   = 'https://github.com/getsentry/sentry-dotnet'

            # A URL to an icon representing this module.
            IconUri      = 'https://raw.githubusercontent.com/getsentry/sentry-unity/main/.github/sentry-wordmark-dark-400x119.svg'

            # ReleaseNotes of this module
            ReleaseNotes = 'https://raw.githubusercontent.com/getsentry/sentry-dotnet/main/CHANGELOG.md'

            # Prerelease string of this module
            Prerelease   = 'dev'

            # Flag to indicate whether the module requires explicit user acceptance for install/update/save
            # RequireLicenseAcceptance = $false

            # External dependent modules of this module
            # ExternalModuleDependencies = @()
        } # End of PSData hashtable
    } # End of PrivateData hashtable

    # HelpInfo URI of this module
    HelpInfoURI          = 'https://docs.sentry.io/platforms/dotnet'
}
