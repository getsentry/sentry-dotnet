$file = 'src/Sentry.Bindings.Android/Sentry.Bindings.Android.csproj'
$property = 'SentryAndroidSdkVersion'
$repo = 'https://github.com/getsentry/sentry-java'

. "$PSScriptRoot/update-project-xml.ps1" @args
