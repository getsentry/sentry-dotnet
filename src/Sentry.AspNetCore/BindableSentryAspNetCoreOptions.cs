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

/// <inheritdoc cref="BindableSentryOptions"/>
internal class BindableSentryAspNetCoreOptions : BindableSentryLoggingOptions
{
    public bool? IncludeActivityData { get; set; }
    public RequestSize? MaxRequestBodySize { get; set; }
    public bool? FlushOnCompletedRequest { get; set; }
    public bool? FlushBeforeRequestCompleted { get; set; }
    public bool? AdjustStandardEnvironmentNameCasing { get; set; }
    public bool? AutoRegisterTracing { get; set; }

    public void ApplyTo(SentryAspNetCoreOptions options)
    {
        base.ApplyTo(options);
        options.IncludeActivityData = IncludeActivityData ?? options.IncludeActivityData;
        options.MaxRequestBodySize = MaxRequestBodySize ?? options.MaxRequestBodySize;
        options.FlushOnCompletedRequest = FlushOnCompletedRequest ?? options.FlushOnCompletedRequest;
        options.FlushBeforeRequestCompleted = FlushBeforeRequestCompleted ?? options.FlushBeforeRequestCompleted;
        options.AdjustStandardEnvironmentNameCasing = AdjustStandardEnvironmentNameCasing ?? options.AdjustStandardEnvironmentNameCasing;
        options.AutoRegisterTracing = AutoRegisterTracing ?? options.AutoRegisterTracing;
    }
}
