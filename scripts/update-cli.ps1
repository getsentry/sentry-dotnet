$file = 'Directory.Build.props'
$property = 'SentryCLIVersion'
$repo = 'https://github.com/getsentry/sentry-cli'

. "$PSScriptRoot/update-project-xml.ps1" @args
