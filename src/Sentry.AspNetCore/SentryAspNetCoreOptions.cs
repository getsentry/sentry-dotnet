using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Sentry.Extensibility;
using Sentry.Extensions.Logging;

#if NETSTANDARD2_0
using IWebHostEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;
#else
using Microsoft.Extensions.Hosting;
#endif

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
    /// The strategy to define the name of a transaction based on the <see cref="HttpContext"/>.
    /// </summary>
    /// <remarks>
    /// The SDK can name transactions automatically when using MVC or Endpoint Routing. In other cases, like when serving static files, it will fallback to the URL path.
    /// This hook allows custom code to define a transaction name given a <see cref="HttpContext"/>.
    /// </remarks>
    public TransactionNameProvider? TransactionNameProvider { get; set; }

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
    /// <para>
    /// When true (by default) Sentry automatically registers its tracing middleware immediately after
    /// `EndpointRoutingApplicationBuilderExtensions.UseRouting`.
    /// </para>
    /// <para>
    /// If you need to control when Sentry's tracing middleware is registered, you can set
    /// <see cref="AutoRegisterTracing"/> to false call
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
    }

    internal void SetEnvironment(IWebHostEnvironment hostingEnvironment)
    {
        // Set environment from AspNetCore hosting environment name, if not set already
        // Note: The SettingLocator will take care of the default behavior and assignment, which takes precedence.
        //       We only need to do anything here if nothing was found by the locator.
        if (SettingLocator.GetEnvironment(useDefaultIfNotFound: false) is not null)
        {
            return;
        }

        if (AdjustStandardEnvironmentNameCasing)
        {
            // NOTE: Sentry prefers to have its environment setting to be all lower case.
            //       .NET Core sets the ENV variable to 'Production' (upper case P),
            //       'Development' (upper case D) or 'Staging' (upper case S) which conflicts with
            //       the Sentry recommendation. As such, we'll be kind and override those values,
            //       here ... if applicable.
            // Assumption: The Hosting Environment is always set.
            //             If not set by a developer, then the framework will auto set it.
            //             Alternatively, developers might set this to a CUSTOM value, which we
            //             need to respect (especially the case-sensitivity).
            //             REF: https://docs.microsoft.com/en-us/aspnet/core/fundamentals/environments

            if (hostingEnvironment.IsProduction())
            {
                Environment = Internal.Constants.ProductionEnvironmentSetting;
            }
            else if (hostingEnvironment.IsStaging())
            {
                Environment = Internal.Constants.StagingEnvironmentSetting;
            }
            else if (hostingEnvironment.IsDevelopment())
            {
                Environment = Internal.Constants.DevelopmentEnvironmentSetting;
            }
            else
            {
                // Use the value set by the developer.
                Environment = hostingEnvironment.EnvironmentName;
            }
        }
        else
        {
            Environment = hostingEnvironment.EnvironmentName;
        }
    }
}
