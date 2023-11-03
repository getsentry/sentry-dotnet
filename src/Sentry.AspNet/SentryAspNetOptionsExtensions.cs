using Sentry.AspNet.Internal;
using Sentry.Extensibility;
using Sentry.Infrastructure;

namespace Sentry.AspNet;

/// <summary>
/// SentryOptions extensions.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class SentryAspNetOptionsExtensions
{
    /// <summary>
    /// Adds ASP.NET classic integration.
    /// </summary>
    public static SentryOptions AddAspNet(this SentryOptions options, RequestSize maxRequestBodySize = RequestSize.None)
    {
        if (options.EventProcessors.ContainsKey(typeof(SystemWebRequestEventProcessor)))
        {
            options.LogWarning($"{nameof(AddAspNet)} has already been called. Subsequent call will be ignored.");

            return options;
        }

        var payloadExtractor = new RequestBodyExtractionDispatcher(
            new IRequestPayloadExtractor[] { new FormRequestPayloadExtractor(), new DefaultRequestPayloadExtractor() },
            options,
            () => maxRequestBodySize);

        var eventProcessor = new SystemWebRequestEventProcessor(payloadExtractor, options);

        // Ignore options.IsGlobalModeEnable, we always want to use HttpContext as backing store here
        options.ScopeStackContainer = new HttpContextScopeStackContainer();

        options.DiagnosticLogger ??= new TraceDiagnosticLogger(options.DiagnosticLevel);
        options.Release ??= SystemWebVersionLocator.Resolve(options, HttpContext.Current);
        options.AddEventProcessor(eventProcessor);
        options.AddDiagnosticSourceIntegration();

        return options;
    }
}
