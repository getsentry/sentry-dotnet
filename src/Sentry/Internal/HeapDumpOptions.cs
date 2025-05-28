namespace Sentry.Internal;

#if MEMORY_DUMP_SUPPORTED

internal record HeapDumpOptions(HeapDumpTrigger Trigger, Debouncer Debouncer, SentryLevel Level);

#endif
