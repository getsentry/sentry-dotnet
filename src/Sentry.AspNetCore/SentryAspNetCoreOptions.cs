using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Sentry.Extensions.Logging;

namespace Sentry.AspNetCore
{
    /// <summary>
    /// An options class for the ASP.NET Core Sentry integration
    /// </summary>
    /// <remarks>
    /// POCO, to be used with ASP.NET Core configuration binding
    /// </remarks>
    public class SentryAspNetCoreOptions
    {
        internal SentryOptions SentryOptions { get; set; }

        /// <summary>
        /// The Data Source Name of a given project in Sentry.
        /// </summary>
        public string Dsn { get; set; }

        /// <summary>
        /// The release version of the application.
        /// </summary>
        /// <example>
        /// 721e41770371db95eee98ca2707686226b993eda
        /// </example>
        /// <remarks>
        /// This value will generally be something along the lines of the git SHA for the given project.
        /// If not explicitly defined via configuration. It will attempt o read it from:
        /// <see cref="System.Reflection.AssemblyInformationalVersionAttribute"/>
        /// </remarks>
        /// <seealso href="https://docs.sentry.io/learn/releases/"/>
        public string Release { get; set; }

        /// <summary>
        /// Whether to initialize the SDK or not.
        /// </summary>
        /// <remarks>
        /// By default, calling <see cref="SentryWebHostBuilderExtensions.UseSentry(IWebHostBuilder)"/>
        /// will enable the SDK. This flag helps you control this behavior.
        /// </remarks>
        public bool InitializeSdk { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to [include the request payload].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [the request payload shall be included in events]; otherwise, <c>false</c>.
        /// </value>
        public bool IncludeRequestPayload { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [include System.Diagnostic.Activity data] to events.
        /// </summary>
        /// <value>
        ///   <c>true</c> if [include activity data]; otherwise, <c>false</c>.
        /// </value>
        /// <see cref="System.Diagnostics.Activity"/>
        /// <seealso href="https://github.com/dotnet/corefx/blob/master/src/System.Diagnostics.DiagnosticSource/src/ActivityUserGuide.md"/>
        public bool IncludeActivityData { get; set; }

        /// <summary>
        /// Controls the integration with the logging integration
        /// </summary>
        public LoggingOptions Logging { get; set; } = new LoggingOptions();

        // Optional convinience callbacks to initialize the SDK
        internal List<Action<SentryOptions>> ConfigureOptionsActions { get; } = new List<Action<SentryOptions>>();

        /// <summary>
        /// Initializes the SDK: This action should be done only once per application lifetime.
        /// </summary>
        /// <remarks>
        /// Using this initialization method is an alternative to calling <see cref="SentrySdk.Init(string)"/> or any overload.
        ///
        /// Initializing the SDK multiple times simply means a new instance is set to the static <see cref="SentrySdk"/>.
        /// Any scope data like breadcrumbs added up to calling Init will be not be included in future events.
        ///
        /// The caller of Init is responsible for disposing the instance returned. If the SDK is initialized
        /// via this logging integration, the <see cref="SentryLoggerProvider"/> will dispose the SDK when it is itself disposed.
        /// </remarks>
        /// <param name="configureOptions">The configure options.</param>
        public void Init(Action<SentryOptions> configureOptions) => ConfigureOptionsActions.Add(configureOptions);
    }

    public class LoggingOptions
    {
        public LogLevel? MinimumBreadcrumbLevel { get; set; }
        public LogLevel? MinimumEventLevel { get; set; }

        public IReadOnlyCollection<ILogEventFilter> Filters { get; set; }
            = new[]
            {
                new DelegateLogEventFilter((category, level, eventId, exception)
                    // https://github.com/aspnet/KestrelHttpServer/blob/0aff4a0440c2f393c0b98e9046a8e66e30a56cb0/src/Kestrel.Core/Internal/Infrastructure/KestrelTrace.cs#L33
                    // 13 = Application unhandled exception, which is captured by the middleware so the LogError of kestrel ends up as a duplicate with less info
                    => eventId.Id == 13
                       && string.Equals(
                           category,
                           "Microsoft.AspNetCore.Server.Kestrel",
                           StringComparison.Ordinal))
            };
    }
}
