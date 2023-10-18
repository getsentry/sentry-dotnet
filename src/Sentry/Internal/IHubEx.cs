namespace Sentry.Internal;

internal interface IHubEx : IHub
{
    SentryId CaptureEventInternal(SentryEvent evt, Hint? hint, Scope? scope = null);
}
