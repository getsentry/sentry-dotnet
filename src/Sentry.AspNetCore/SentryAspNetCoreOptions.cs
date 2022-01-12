using Sentry.Extensibility;
using Sentry.Extensions.Logging;

namespace Sentry.AspNetCore;

/// <summary>
/// An options class for the ASP.NET Core Sentry integration
/// </summary>
/// <inheritdoc />
public class SentryAspNetCoreOptions : SentryLoggingOptions
{
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
    /// Controls the size of the request body to extract if any.
    /// </summary>
    /// <remarks>
    /// No truncation is done by the SDK.
    /// If the request body is larger than the accepted size, nothing is sent.
    /// </remarks>
    public RequestSize MaxRequestBodySize { get; set; }

    /// <summary>
    /// Flush on completed request
    /// </summary>
    public bool FlushOnCompletedRequest { get; set; }

    /// <summary>
    /// Flush before the request gets completed.
    /// </summary>
    internal bool FlushBeforeRequestCompleted { get; set; }

    /// <summary>
    /// How long to wait for the flush to finish. Defaults to 2 seconds.
    /// </summary>
    public TimeSpan FlushTimeout { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>
    /// The strategy to define the name of a transaction based on the HttpContext
    /// </summary>
    /// <remarks>
    /// The SDK can name transactions automatically when using MVC or Endpoint Routing. In other cases, like when serving static files, it fallback to Unknown Route. This hook allows custom code to define a transaction name given a HttpContext.
    /// </remarks>
    public ITransactionNameProvider? TransactionNameProvider { get; set; }

    /// <summary>
    /// Controls whether the casing of the standard (Production, Development and Staging) environment name supplied by <see cref="Microsoft.AspNetCore.Hosting.IHostingEnvironment" />
    /// is adjusted when setting the Sentry environment. Defaults to true.
    /// </summary>
    /// <remarks>
    /// The default .NET Core environment names include Production, Development and Staging (note Pascal casing), whereas Sentry prefers
    /// to have its environment setting be all lower case.
    /// </remarks>
    public bool AdjustStandardEnvironmentNameCasing { get; set; } = true;

    /// <summary>
    /// Creates a new instance of <see cref="SentryAspNetCoreOptions"/>.
    /// </summary>
    public SentryAspNetCoreOptions()
    {
        // Don't report Environment.UserName as the user.
        IsEnvironmentUser = false;
    }
}
