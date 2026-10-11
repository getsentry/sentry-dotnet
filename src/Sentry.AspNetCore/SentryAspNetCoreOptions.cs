using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Sentry.Extensibility;
using Sentry.Extensions.Logging;

namespace Sentry.AspNetCore;

/// <summary>
/// An options class for the ASP.NET Core Sentry integration
/// </summary>
/// <inheritdoc />
public class SentryAspNetCoreOptions : SentryHostOptions
{
    /// <summary>
    /// Whether the <see cref="Microsoft.Extensions.Logging.ILogger"/> integration that <c>UseSentry</c> adds sends log entries to Sentry as structured logs.
    /// </summary>
    /// <remarks>
    /// Defaults to <see langword="false"/>, including when the app also calls <c>AddSentry()</c> on its
    /// <see cref="Microsoft.Extensions.Logging.ILoggingBuilder"/>.
    /// </remarks>
    public bool EnableLogs
    {
        get => LogsEnabled;
        set => LogsEnabled = value;
    }

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
    /// Will capture an event if a blocking call is detected.
    /// </summary>
    /// <remarks>
    /// Get a Sentry event for a sync over async call such as (await SomeTask).Result
    /// Note that when ConfigureAwait(false) is used, blocking won't be detected.
    /// Additionally, detects blocking calls over CLR waits such as lock, ReaderWriterLock, etc.
    /// Blocking calls through system calls are also not captured.
    /// </remarks>
    /// <seealso href="https://github.com/getsentry/Ben.BlockingDetector/"/>
    public bool CaptureBlockingCalls { get; set; }

    /// <summary>
    /// The strategy to define the name of a transaction based on the <see cref="HttpContext"/>.
    /// </summary>
    /// <remarks>
    /// The SDK can name transactions automatically when using MVC or Endpoint Routing. In other cases, like when serving static files, it will fallback to the URL path.
    /// This hook allows custom code to define a transaction name given a <see cref="HttpContext"/>.
    /// </remarks>
    public TransactionNameProvider? TransactionNameProvider { get; set; }

    /// <summary>
    /// Controls whether <see cref="TransactionNameProvider"/> runs on every request, or only
    /// when routing fails to resolve a template.
    /// </summary>
    /// <remarks>
    /// When <c>false</c> (default), the provider is only invoked as a fallback for unresolved routes —
    /// matching the legacy behaviour. When <c>true</c>, the provider is invoked for every request
    /// after routing; a non-empty return value overrides the route-derived name. If the provider
    /// returns <c>null</c> or <see cref="string.Empty"/>, the routed name is preserved (or the URL-path
    /// fallback applies when no route was resolved).
    /// </remarks>
    public bool PreferTransactionNameProvider { get; set; }

    /// <summary>
    /// <para>
    /// When true (by default) Sentry automatically registers its tracing middleware immediately after
    /// `EndpointRoutingApplicationBuilderExtensions.UseRouting`.
    /// </para>
    /// <para>
    /// If you need to control when Sentry's tracing middleware is registered, you can set
    /// <see cref="AutoRegisterTracing"/> to false and call
    /// <see cref="SentryTracingMiddlewareExtensions.UseSentryTracing"/> yourself, sometime after calling
    /// `EndpointRoutingApplicationBuilderExtensions.UseRouting` and before calling
    /// `EndpointRoutingApplicationBuilderExtensions.UseEndpoints`.
    /// </para>
    /// </summary>
    public bool AutoRegisterTracing { get; set; } = true;

    /// <summary>
    /// Creates a new instance of <see cref="SentryAspNetCoreOptions"/>.
    /// </summary>
    public SentryAspNetCoreOptions()
    {
        // Don't report Environment.UserName as the user.
        IsEnvironmentUser = false;
        SetSdk(Constants.SdkName, SentryMiddleware.NameAndVersion);
    }
}
