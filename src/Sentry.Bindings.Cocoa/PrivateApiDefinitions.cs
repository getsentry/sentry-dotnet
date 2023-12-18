using System;
using Foundation;
using ObjCRuntime;

namespace Sentry.CocoaSdk;

// SentryScope.ApplyToEvent was made private in 8.x, but we use it in our CocoaEventProcessor class.
// TODO: Find a better way than using the private API.
partial interface SentryScope
{
    // -(SentryEvent * _Nullable)applyToEvent:(SentryEvent * _Nonnull)event maxBreadcrumb:(NSUInteger)maxBreadcrumbs;
    [Export ("applyToEvent:maxBreadcrumb:")]
    [return: NullAllowed]
    SentryEvent ApplyToEvent (SentryEvent @event, nuint maxBreadcrumbs);
}

// The following types are type-forwarded in various public headers, but have no headers of their own.
// Generate stub classes so the APIs that use them can still operate.

[Internal]
[DisableDefaultCtor]
[BaseType (typeof(NSObject))]
interface SentryBaggage
{
}

[Internal]
[DisableDefaultCtor]
[BaseType (typeof(NSObject))]
interface SentrySession
{
}

[Internal]
[DisableDefaultCtor]
[BaseType (typeof(NSObject))]
interface SentryTracer
{
}
