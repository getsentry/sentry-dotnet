using Microsoft.Extensions.Options;
using Quartz;
using Sentry.Extensions.Logging;

namespace Sentry.Quartz;

// Quartz's ErrorLogger logs each exception a job throws again, wrapped in a JobExecutionProcessException.
// SentryScopeMiddleware already captured it, and the default DeduplicateMode doesn't look at inner exceptions.
// Open generic, so it applies to whichever SentryLoggingOptions the logging integration uses, such as
// SentryAspNetCoreOptions.
internal sealed class QuartzErrorLogFilterSetup<TOptions> : IConfigureOptions<TOptions>
    where TOptions : SentryLoggingOptions
{
    // SchedulerErrorForFire in https://github.com/quartznet/quartznet/blob/v4.0.0/src/Quartz/Core/CoreLog.cs
    internal const string Category = "Quartz.Core.ErrorLogger";
    internal const int EventId = 1081;

    public void Configure(TOptions options) =>
        options.AddLogEntryFilter((category, _, eventId, exception) =>
            eventId.Id == EventId
            && string.Equals(category, Category, StringComparison.Ordinal)
            && exception is JobExecutionProcessException { InnerException: { } jobException }
            && SentryScopeMiddleware.WasCaptured(jobException));
}
