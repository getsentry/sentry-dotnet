using Sentry.Extensibility;

namespace Sentry.Internal;

internal static class SentryEventHelper
{
    public static SentryEvent? ProcessEvent(SentryEvent? evt, IEnumerable<ISentryEventProcessor> processors,
        SentryHint? hint, SentryOptions options, DataCategory dataCategory)
    {
        if (evt == null)
        {
            return evt;
        }

        var processedEvent = evt;
        var effectiveHint = hint ?? new SentryHint(options);

        foreach (var processor in processors)
        {
            processedEvent = processor.DoProcessEvent(processedEvent, effectiveHint);
            if (processedEvent == null)
            {
                options.ClientReportRecorder.RecordDiscardedEvent(DiscardReason.EventProcessor, dataCategory);
                options.LogInfo("Event dropped by processor {0}", processor.GetType().Name);
                break;
            }
        }
        return processedEvent;
    }

#if NET6_0_OR_GREATER
    [UnconditionalSuppressMessage("Trimming", "IL2026: RequiresUnreferencedCode", Justification = AotHelper.AvoidAtRuntime)]
#endif
    public static SentryEvent? DoBeforeSend(SentryEvent? @event, SentryHint hint, SentryOptions options)
    {
        if (@event is null || options.BeforeSendInternal is null)
        {
            return @event;
        }

        options.LogDebug("Calling the BeforeSend callback.");
        try
        {
            @event = options.BeforeSendInternal?.Invoke(@event, hint);
            if (@event == null) // Rejected event
            {
                options.ClientReportRecorder.RecordDiscardedEvent(DiscardReason.BeforeSend, DataCategory.Error);
                options.LogInfo("Event dropped by BeforeSend callback.");
            }
        }
        catch (Exception e)
        {
            if (!AotHelper.IsTrimmed)
            {
                // Attempt to demystify exceptions before adding them as breadcrumbs.
                e.Demystify();
            }

            options.LogError(e, "The BeforeSend callback threw an exception. It will be added as breadcrumb and continue.");
            var data = new Dictionary<string, string>
            {
                {"message", e.Message}
            };
            if (e.StackTrace is not null)
            {
                data.Add("stackTrace", e.StackTrace);
            }
            @event?.AddBreadcrumb(
                "BeforeSend callback failed.",
                category: "SentryClient",
                data: data,
                level: BreadcrumbLevel.Error);
        }

        return @event;
    }
}
