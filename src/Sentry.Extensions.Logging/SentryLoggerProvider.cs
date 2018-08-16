using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sentry.Extensibility;
using Sentry.Infrastructure;
using Sentry.Reflection;

namespace Sentry.Extensions.Logging
{
    [ProviderAlias("Sentry")]
    public class SentryLoggerProvider : ILoggerProvider
    {
        private readonly IHub _hub;
        private readonly ISystemClock _clock;
        private readonly SentryLoggingOptions _options;

        private IDisposable _scope;
        private IDisposable _sdk;

        internal static readonly (string Name, string Version) NameAndVersion
            = typeof(SentryLogger).Assembly.GetNameAndVersion();

        public SentryLoggerProvider(IOptions<SentryLoggingOptions> options)
            : this(HubAdapter.Instance,
                SystemClock.Clock,
                options.Value)
        { }

        internal SentryLoggerProvider(
            IHub hub,
            ISystemClock clock,
            SentryLoggingOptions options)
        {
            Debug.Assert(options != null);
            Debug.Assert(clock != null);
            Debug.Assert(hub != null);

            _hub = hub;
            _clock = clock;
            _options = options;

            // SDK is being initialized through this integration
            // Lifetime is owned by this instance:
            if (_options.InitializeSdk && !SentrySdk.IsEnabled)
            {
                _sdk = SentrySdk.Init(_options);

                // Creates a scope so that Integration added below can be dropped when the logger is disposed
                _scope = hub.PushScope();
                hub.ConfigureScope(s =>
                {
                    s.Sdk.Name = NameAndVersion.Name;
                    s.Sdk.Version = NameAndVersion.Version;
                });
            }
        }

        public ILogger CreateLogger(string categoryName) => new SentryLogger(categoryName, _options, _clock, _hub);

        public void Dispose()
        {
            _scope?.Dispose();
            _scope = null;
            _sdk?.Dispose();
            _sdk = null;
        }
    }
}
