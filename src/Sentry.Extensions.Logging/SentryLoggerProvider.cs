using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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

        public SentryLoggerProvider(IOptions<SentryLoggingOptions> options, IHub hub)
            : this(hub,
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

            if (hub.IsEnabled)
            {
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
