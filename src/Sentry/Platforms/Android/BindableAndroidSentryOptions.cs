using Sentry.Android;

// ReSharper disable once CheckNamespace
namespace Sentry;

internal partial class BindableSentryOptions
{
    public AndroidOptions Android { get; } = new AndroidOptions();

    /// <summary>
    /// The .NET SDK specific options for the Android platform.
    /// </summary>
    public class AndroidOptions
    {
        public LogCatIntegrationType? LogCatIntegration { get; set; }
        public int? LogCatMaxLines { get; set; }

        public void ApplyTo(SentryOptions.AndroidOptions options)
        {
            options.LogCatIntegration = LogCatIntegration ?? options.LogCatIntegration;
            options.LogCatMaxLines = LogCatMaxLines ?? options.LogCatMaxLines;
        }
    }
}
