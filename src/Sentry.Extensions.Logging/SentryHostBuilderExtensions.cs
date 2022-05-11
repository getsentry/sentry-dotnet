using System.ComponentModel;
using Sentry;
using Sentry.Extensions.Logging;
using Sentry.Extensions.Logging.Internal;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.Hosting
{
    /// <summary>
    /// Extension methods to <see cref="IHostBuilder"/>
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class SentryHostBuilderExtensions
    {
        /// <summary>
        /// Uses Sentry integration.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <returns>The <paramref name="builder"/>.</returns>
        public static IHostBuilder UseSentry(this IHostBuilder builder) =>
            builder.UseSentry<SentryLoggingOptions>();

        /// <summary>
        /// Uses Sentry integration.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="dsn">The DSN.</param>
        /// <returns>The <paramref name="builder"/>.</returns>
        public static IHostBuilder UseSentry(this IHostBuilder builder, string dsn) =>
            builder.UseSentry<SentryLoggingOptions>(o => o.Dsn = dsn);

        /// <summary>
        /// Uses Sentry integration.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="configureOptions">An action that will configure Sentry.</param>
        /// <returns>The <paramref name="builder"/>.</returns>
        public static IHostBuilder UseSentry(this IHostBuilder builder,
            Action<SentryOptions>? configureOptions) =>
            builder.UseSentry<SentryLoggingOptions>(configureOptions);

        /// <summary>
        /// Uses Sentry integration.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="configureOptions">An action that will configure Sentry.</param>
        /// <returns>The <paramref name="builder"/>.</returns>
        public static IHostBuilder UseSentry(this IHostBuilder builder,
            Action<HostBuilderContext, SentryOptions>? configureOptions) =>
            builder.UseSentry<SentryLoggingOptions>(configureOptions);

        /// <summary>
        /// Uses Sentry integration.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="configureOptions">An action that will configure Sentry.</param>
        /// <returns>The <paramref name="builder"/>.</returns>
        public static IHostBuilder UseSentry(this IHostBuilder builder,
            Action<SentryLoggingOptions>? configureOptions) =>
            builder.UseSentry<SentryLoggingOptions>(configureOptions);

        /// <summary>
        /// Uses Sentry integration.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="configureOptions">An action that will configure Sentry.</param>
        /// <returns>The <paramref name="builder"/>.</returns>
        public static IHostBuilder UseSentry(this IHostBuilder builder,
            Action<HostBuilderContext, SentryLoggingOptions>? configureOptions) =>
            builder.UseSentry<SentryLoggingOptions>(configureOptions);
    }
}
