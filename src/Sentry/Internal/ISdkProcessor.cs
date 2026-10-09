namespace Sentry.Internal;

// Marker interface for SDK processors. 
// Must not be applied to processors that run user code (e.g. DelegateEventProcessor)
internal interface ISdkProcessor;
