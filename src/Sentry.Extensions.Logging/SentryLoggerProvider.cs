using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sentry.Infrastructure;
using Sentry.Protocol;
using Sentry.Reflection;

namespace Sentry.Extensions.Logging
{
    /// <summary>
    /// Sentry Logger Provider.
    /// </summary>
    [ProviderAlias("Sentry")]
    public class SentryLoggerProvider : ILoggerProvider
    {
        private readonly ISystemClock _clock;
        private readonly SentryLoggingOptions _options;
        private readonly IDisposable? _scope;
        private readonly IDisposable? _disposableHub;

        internal IHub Hub { get; }

        internal static readonly SdkVersion NameAndVersion
            = typeof(SentryLogger).Assembly.GetNameAndVersion();

        private static readonly string ProtocolPackageName = "nuget:" + NameAndVersion.Name;

        /// <summary>
        /// Creates a new instance of <see cref="SentryLoggerProvider"/>.
        /// </summary>
        /// <param name="options">The Options.</param>
        /// <param name="hub">The Hub.</param>
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
            _disposableHub = hub as IDisposable;

            Hub = hub;
            _clock = clock;
            _options = options;

            if (hub.IsEnabled)
            {
                _scope = hub.PushScope();
                hub.ConfigureScope(s =>
                {
                    if (s.Sdk is { } sdk)
                    {
                        sdk.Name = Constants.SdkName;
                        sdk.Version = NameAndVersion.Version;

                        if (NameAndVersion.Version is {} version)
                        {
                            sdk.AddPackage(ProtocolPackageName, version);
                        }
                    }
                });

                // Add scope configuration to hub from options
                foreach (var callback in options.ConfigureScopeCallbacks)
                {
                    hub.ConfigureScope(callback);
                }
            }
        }

        /// <summary>
        /// Creates a logger for the category.
        /// </summary>
        /// <param name="categoryName">Category name.</param>
        /// <returns>A logger.</returns>
        public ILogger CreateLogger(string categoryName) => new SentryLogger(categoryName, _options, _clock, Hub);

        /// <summary>
        /// Dispose.
        /// </summary>
        public void Dispose()
        {
            _scope?.Dispose();
            _disposableHub?.Dispose();
        }
    }
}
