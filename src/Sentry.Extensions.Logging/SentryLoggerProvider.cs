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
        private readonly ISystemClock _clock;
        private readonly SentryLoggingOptions _options;
        private readonly IDisposable _scope;
        private readonly IDisposable _disposableHub;

        internal IHub Hub { get; }

        internal static readonly (string Name, string Version) NameAndVersion
            = typeof(SentryLogger).Assembly.GetNameAndVersion();

        private static readonly string ProtocolPackageName = "nuget:" + NameAndVersion.Name;

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

            _disposableHub = hub as IDisposable;

            Hub = hub;
            _clock = clock;
            _options = options;

            if (hub.IsEnabled)
            {
                _scope = hub.PushScope();
                hub.ConfigureScope(s =>
                {
                    s.Sdk.Name = Constants.SdkName;
                    s.Sdk.Version = NameAndVersion.Version;
                    s.Sdk.AddPackage(ProtocolPackageName, NameAndVersion.Version);
                });
            }
        }

        public ILogger CreateLogger(string categoryName) => new SentryLogger(categoryName, _options, _clock, Hub);

        public void Dispose()
        {
            _scope?.Dispose();
            _disposableHub?.Dispose();
        }
    }
}
