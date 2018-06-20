using System;
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
        public string Dsn { get; set; }

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

        public LoggingOptions Logging { get; set; } = new LoggingOptions();

        // An optional convinience callback to initialize the SDK
        internal Action<SentryOptions> ConfigureOptions { get; private set; }

        /// <summary>
        /// Initializes the SDK: This action should be done only once per application lifetime.
        /// </summary>
        /// <remarks>
        /// Using this initialization method is an alternative to calling <see cref="SentryCore.Init(string)"/> or any overload.
        ///
        /// Initializing the SDK multiple times simply means a new instance is set to the static <see cref="SentryCore"/>.
        /// Any scope data like breadcrumbs added up to calling Init will be not be included in future events.
        ///
        /// The caller of Init is responsible for disposing the instance returned. If the SDK is initialized
        /// via this logging integration, the <see cref="SentryLoggerProvider"/> will dispose the SDK when it is itself disposed.
        /// </remarks>
        /// <param name="configureOptions">The configure options.</param>
        public void Init(Action<SentryOptions> configureOptions) => ConfigureOptions = configureOptions;
    }

    public class LoggingOptions
    {
        public LogLevel? MinimumBreadcrumbLevel { get; set; }
        public LogLevel? MinimumEventLevel { get; set; }
    }
}
