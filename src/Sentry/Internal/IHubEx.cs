namespace Sentry.Internal;

internal interface IHubEx : IHub
{
    SentryId CaptureEventInternal(SentryEvent evt, Scope? scope = null);
}
