using System;
using Foundation;
using ObjCRuntime;

namespace Sentry.CocoaSdk;

partial interface SentryScope
{
    // -(SentryEvent * _Nullable)applyToEvent:(SentryEvent * _Nonnull)event maxBreadcrumb:(NSUInteger)maxBreadcrumbs;
    [Export ("applyToEvent:maxBreadcrumb:")]
    [return: NullAllowed]
    SentryEvent ApplyToEvent (SentryEvent @event, nuint maxBreadcrumbs);
}
