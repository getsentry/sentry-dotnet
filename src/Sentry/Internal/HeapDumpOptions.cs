namespace Sentry.Internal;

internal record HeapDumpOptions(HeapDumpTrigger Trigger, Debouncer Debouncer, SentryLevel Level);
