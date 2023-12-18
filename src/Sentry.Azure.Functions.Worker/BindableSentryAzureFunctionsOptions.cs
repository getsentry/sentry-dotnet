using Sentry.Extensions.Logging;

namespace Sentry.Azure.Functions.Worker;

/// <inheritdoc cref="BindableSentryOptions"/>
internal class BindableSentryAzureFunctionsOptions : BindableSentryLoggingOptions
{
    public void ApplyTo(SentryAzureFunctionsOptions options)
    {
        base.ApplyTo(options);
    }
}
