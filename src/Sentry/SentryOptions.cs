using System;
using Sentry.Extensibility;
using Sentry.Http;

namespace Sentry
{
    /// TODO: the SDK options
    public class SentryOptions : IScopeOptions
    {
        // TODO: This will be set via AsmInfo.cs?
        // Used on AUTH header and also SDK payload interface?
        internal string ClientVersion
        {
            get;
            set; // Cannot be null!
        } = "Sentry.NET";

        // TODO: Where does this go?
        // Version protocol this SDK is written to support
        internal int SentryVersion { get; } = 7;

        public TimeSpan ShutdownTimeout { get; set; } = TimeSpan.FromSeconds(3);

        public Dsn Dsn { get; set; }

        /// <summary>
        /// Gets or sets the maximum breadcrumbs.
        /// </summary>
        /// <remarks>
        /// When the number of events reach this configuration value,
        /// older breadcrumbs start dropping to make room for new ones.
        /// </remarks>
        /// <value>
        /// The maximum breadcrumbs per scope.
        /// </value>
        public int MaxBreadcrumbs { get; set; } = 100;

        public Func<SentryEvent, SentryEvent> BeforeSend { get; set; }

        internal Action<BackgroundWorkerOptions> ConfigureBackgroundWorkerOptions { get; private set; }

        internal Action<HttpOptions> ConfigureHttpTransportOptions { get; private set; }

        internal Func<SentryOptions, ITransport> TransportFactory { get; set; }

        public void Worker(Action<BackgroundWorkerOptions> configure) => ConfigureBackgroundWorkerOptions = configure;

        public void Http(Action<HttpOptions> configure) => ConfigureHttpTransportOptions = configure;
    }
}
