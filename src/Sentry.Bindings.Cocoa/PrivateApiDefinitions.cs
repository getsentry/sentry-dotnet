using System;
using Foundation;
using ObjCRuntime;

namespace Sentry.CocoaSdk;

// -----------------------------------------------------------------------------
// Classic Sentry.framework holdouts.
//
// getsentry/sentry-dotnet#5444 migrated the generated bindings from the classic
// `Sentry*` Objective-C classes to the new `SentryObjC*` surface. Two consumers
// still depend on classic APIs that the `SentryObjC*` surface does not (yet) expose,
// so we keep binding just those classic types here by hand until follow-up work lands:
//
//   1. CocoaEventProcessor enriches .NET-captured events with the native SDK's contexts
//      by applying the current scope to a throwaway event. `SentryObjCScope` exposes
//      `serialize` but no `applyToEvent:maxBreadcrumb:`, so we still use classic
//      `SentrySDK.configureScope` + `SentryScope.applyToEvent` + `SentryEvent.context`.
//
//   2. The envelope/JSON serialization path type-tests native objects against cocoa's
//      `SentrySerializable` protocol. `SentryObjC*` types have `serialize` methods but
//      expose no exported protocol to test against, so we keep the classic protocol.
//
// These classes still ship in Sentry.framework (linked alongside SentryObjC), so the
// hand-written bindings below resolve at runtime. See getsentry/sentry-dotnet#5444.
// -----------------------------------------------------------------------------

// SentrySDK.configureScope is used by CocoaEventProcessor to obtain the classic scope.
[Internal]
[BaseType(typeof(NSObject), Name = "_TtC6Sentry9SentrySDK")]
[DisableDefaultCtor]
interface SentrySDK
{
    // +(void)configureScope:(void (^ _Nonnull)(SentryScope * _Nonnull))callback;
    [Static]
    [Export("configureScope:")]
    void ConfigureScope(Action<SentryScope> callback);
}

// SentryScope.applyToEvent was made private in 8.x, but we use it in CocoaEventProcessor.
// TODO: Find a better way than using the private API (getsentry/sentry-dotnet#5444).
[Internal]
[BaseType(typeof(NSObject))]
[DisableDefaultCtor]
interface SentryScope
{
    // -(SentryEvent * _Nullable)applyToEvent:(SentryEvent * _Nonnull)event maxBreadcrumb:(NSUInteger)maxBreadcrumbs;
    [Export("applyToEvent:maxBreadcrumb:")]
    [return: NullAllowed]
    SentryEvent ApplyToEvent(SentryEvent @event, nuint maxBreadcrumbs);
}

// A minimal binding of the classic SentryEvent - CocoaEventProcessor creates a throwaway
// instance and reads its populated `context` dictionary.
[Internal]
[BaseType(typeof(NSObject))]
interface SentryEvent
{
    // @property (nonatomic, strong) NSDictionary<NSString *,NSDictionary<NSString *,id> *> * _Nullable context;
    [NullAllowed, Export("context", ArgumentSemantic.Strong)]
    NSDictionary<NSString, NSDictionary<NSString, NSObject>> Context { get; set; }
}

// The classic SentrySerializable protocol. Envelope serialization type-tests native
// objects against ISentrySerializable and calls serialize() before JSON-encoding.
[Internal]
[Protocol, Model]
[BaseType(typeof(NSObject))]
interface SentrySerializable
{
    // @required -(NSDictionary<NSString *,id> * _Nonnull)serialize;
    [Abstract]
    [Export("serialize")]
    NSDictionary<NSString, NSObject> Serialize();
}
