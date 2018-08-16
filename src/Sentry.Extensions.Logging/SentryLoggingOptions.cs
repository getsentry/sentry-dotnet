using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Sentry.Protocol;

namespace Sentry.Extensions.Logging
{
    /// <summary>
    /// Sentry logging integration options
    /// </summary>
    /// <inheritdoc />
    public class SentryLoggingOptions : SentryOptions
    {
        /// <summary>
        /// Gets or sets the minimum breadcrumb level.
        /// </summary>
        /// <remarks>Events with this level or higher will be stored as <see cref="Breadcrumb"/></remarks>
        /// <value>
        /// The minimum breadcrumb level.
        /// </value>
        public LogLevel MinimumBreadcrumbLevel { get; set; } = LogLevel.Information;

        /// <summary>
        /// Gets or sets the minimum event level.
        /// </summary>
        /// <remarks>
        /// Events with this level or higher will be sent to Sentry
        /// </remarks>
        /// <value>
        /// The minimum event level.
        /// </value>
        public LogLevel MinimumEventLevel { get; set; } = LogLevel.Error;

        /// <summary>
        /// The DSN which defines where events are sent
        /// </summary>
        public new string Dsn
        {
            get => base.Dsn?.ToString();
            set
            {
                if (value != null && !Sentry.Dsn.IsDisabled(value))
                {
                    base.Dsn = new Dsn(value);
                }
            }
        }

        /// <summary>
        /// Whether to initialize this SDK through this integration
        /// </summary>
        public bool InitializeSdk { get; set; } = true;

        public IReadOnlyCollection<ILogEventFilter> Filters { get; set; }

        //// An optional convenience callback to initialize the SDK
        //internal List<Action<SentryOptions>> ConfigureOptionsActions { get; } = new List<Action<SentryOptions>>();

        ///// <summary>
        ///// Initializes the SDK: This action should be done only once per application lifetime.
        ///// </summary>
        ///// <remarks>
        ///// Using this initialization method is an alternative to calling <see cref="SentrySdk.Init(string)"/> or any overload.
        /////
        ///// Initializing the SDK multiple times simply means a new instance is set to the static <see cref="SentrySdk"/>.
        ///// Any scope data like breadcrumbs added up to calling Init will be not be included in future events.
        /////
        ///// The caller of Init is responsible for disposing the instance returned. If the SDK is initialized
        ///// via this logging integration, the <see cref="SentryLoggerProvider"/> will dispose the SDK when it is itself disposed.
        ///// </remarks>
        ///// <param name="configureOptions">The configure options.</param>
        //public void Init(Action<SentryOptions> configureOptions) => ConfigureOptionsActions.Add(configureOptions);
    }
}
