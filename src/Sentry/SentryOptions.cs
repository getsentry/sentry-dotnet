using System;
using System.Collections.Generic;
using Sentry.Extensibility;
using Sentry.Http;
using static Sentry.Internal.Constants;

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

        internal int SentryVersion { get; } = ProtocolVersion;

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
        public int MaxBreadcrumbs { get; set; } = DefaultMaxBreadcrumbs;

        public Dsn Dsn { get; set; }

        public Func<SentryEvent, SentryEvent> BeforeSend { get; set; }

        internal Action<BackgroundWorkerOptions> ConfigureBackgroundWorkerOptions { get; private set; }

        internal List<Action<HttpOptions>> ConfigureHttpTransportOptions { get; private set; }

        internal Func<SentryOptions, ITransport> TransportFactory { get; set; }

        public void Worker(Action<BackgroundWorkerOptions> configure) => ConfigureBackgroundWorkerOptions = configure;

        public void Http(Action<HttpOptions> configure)
        {
            if (ConfigureHttpTransportOptions == null)
            {
                ConfigureHttpTransportOptions = new List<Action<HttpOptions>>(1);
            }
            ConfigureHttpTransportOptions.Add(configure);
        }
    }
}
