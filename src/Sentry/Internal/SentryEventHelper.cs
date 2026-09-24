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
            try
            {
                processedEvent = processor.DoProcessEvent(processedEvent, effectiveHint);
            }
            catch (Exception e)
            {
                options.ClientReportRecorder.RecordDiscardedEvent(DiscardReason.CallbackError, dataCategory);
                options.LogError(e, "Event processor {0} threw an exception. The event will be dropped.", processor.GetType().Name);
                return null;
            }

            if (processedEvent == null)
            {
                options.ClientReportRecorder.RecordDiscardedEvent(DiscardReason.EventProcessor, dataCategory);
                options.LogInfo("Event dropped by processor {0}", processor.GetType().Name);
                break;
            }
        }
        return processedEvent;
    }

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
            options.ClientReportRecorder.RecordDiscardedEvent(DiscardReason.BeforeSend, DataCategory.Error);
            options.LogError(e, "The BeforeSend callback threw an exception. The event will be dropped.");
            return null;
        }

        return @event;
    }

    public static SentryEvent? DoBeforeSendFeedback(SentryEvent? @event, SentryHint hint, SentryOptions options)
    {
        if (@event is null || options.BeforeSendFeedbackInternal is null)
        {
            return @event;
        }

        options.LogDebug("Calling the BeforeSendFeedback callback.");
        try
        {
            @event = options.BeforeSendFeedbackInternal.Invoke(@event, hint);
            if (@event == null)
            {
                options.ClientReportRecorder.RecordDiscardedEvent(DiscardReason.BeforeSend, DataCategory.Feedback);
                options.LogInfo("Feedback dropped by BeforeSendFeedback callback.");
            }
        }
        catch (Exception e)
        {
            options.ClientReportRecorder.RecordDiscardedEvent(DiscardReason.CallbackError, DataCategory.Feedback);
            options.LogError(e, "The BeforeSendFeedback callback threw an exception. The feedback will be dropped.");
            return null;
        }

        return @event;
    }
}
