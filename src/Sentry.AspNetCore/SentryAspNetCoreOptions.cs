using Microsoft.Extensions.Logging;

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

        public LoggingOptions Logging { get; set; }
    }

    public class LoggingOptions
    {
        public LogLevel? MinimumBreadcrumbLevel { get; set; }
        public LogLevel? MinimumEventLevel { get; set; }
    }
}
