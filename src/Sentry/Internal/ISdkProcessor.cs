namespace Sentry.Internal;

// Must not be applied to processors that run user code, such as DelegateEventProcessor.
internal interface ISdkProcessor;
