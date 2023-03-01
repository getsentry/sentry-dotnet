using System;
using Foundation;
using ObjCRuntime;

namespace Sentry.CocoaSdk;

// SentryScope.ApplyToEvent was made private in 8.x, but we use it in our IosEventProcessor class.
// TODO: Find a better way than using the private API.
partial interface SentryScope
{
    // -(SentryEvent * _Nullable)applyToEvent:(SentryEvent * _Nonnull)event maxBreadcrumb:(NSUInteger)maxBreadcrumbs;
    [Export ("applyToEvent:maxBreadcrumb:")]
    [return: NullAllowed]
    SentryEvent ApplyToEvent (SentryEvent @event, nuint maxBreadcrumbs);
}
