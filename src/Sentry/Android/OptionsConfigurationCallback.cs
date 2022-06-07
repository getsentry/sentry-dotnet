namespace Sentry.Android
{
    internal class OptionsConfigurationCallback : JavaObject, Java.Sentry.IOptionsConfiguration
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
}
