using Sentry.JavaSdk.Android.Core;

namespace Sentry.Android.Callbacks;

internal class OptionsConfigurationCallback : JavaObject, JavaSdk.Sentry.IOptionsConfiguration
{
    private readonly Action<SentryAndroidOptions> _configureOptions;

    public OptionsConfigurationCallback(Action<SentryAndroidOptions> configureOptions) =>
        _configureOptions = configureOptions;

    public void Configure(JavaObject optionsObject)
    {
        var options = (SentryAndroidOptions)optionsObject;
        _configureOptions(options);
    }
}
