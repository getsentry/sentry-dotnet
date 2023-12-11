using Sentry.Android;

// ReSharper disable once CheckNamespace
namespace Sentry;

public partial class SentryOptions
{
    /// <summary>
    /// The .NET SDK specific options for the Android platform.
    /// </summary>
    public AndroidOptions Android { get; }

    public class AndroidOptions
    {
        /// <summary>
        /// Gets or sets whether when LogCat logs are attached to events.
        /// The default is <see cref="LogCatIntegrationType.None"/>
        /// </summary>
        /// <seealso cref="LogCatMaxLines" />
        public LogCatIntegrationType LogCatIntegration { get; set; } = LogCatIntegrationType.None;

        /// <summary>
        /// Gets or sets the maximum number of lines to read from LogCat logs.
        /// The default value is 1000.
        /// </summary>
        /// <seealso cref="LogCatIntegration" />
        public int LogCatMaxLines { get; set; } = 1000;
    }
}
