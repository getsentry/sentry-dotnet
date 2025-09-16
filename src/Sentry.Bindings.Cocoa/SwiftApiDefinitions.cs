/*
 * This file defines iOS API contracts for the members we need from Sentry-Swift.h
 * Note that we are **not** using Objective Sharpie to generate contracts (instead they're maintained manually).
 */
using System;
using Foundation;
using ObjCRuntime;
using UIKit;

namespace Sentry.CocoaSdk;

[BaseType(typeof(NSObject), Name = "_TtC6Sentry14SentryFeedback")]
[DisableDefaultCtor] // Marks the default constructor as unavailable
[Internal]
interface SentryFeedback
{
    [Export("name", ArgumentSemantic.Copy)]
    string Name { get; set; }

    [Export("email", ArgumentSemantic.Copy)]
    string Email { get; set; }

    [Export("message", ArgumentSemantic.Copy)]
    string Message { get; set; }

    [Export("source")]
    SentryFeedbackSource Source { get; set; }

    [Export("eventId", ArgumentSemantic.Strong)]
    SentryId EventId { get; }

    [Export("associatedEventId", ArgumentSemantic.Strong)]
    SentryId AssociatedEventId { get; set; }

    [Export("initWithMessage:name:email:source:associatedEventId:attachments:")]
    [DesignatedInitializer]
    IntPtr Constructor(string message, [NullAllowed] string name, [NullAllowed] string email, SentryFeedbackSource source, [NullAllowed] SentryId associatedEventId, [NullAllowed] NSData[] attachments);
}

// @interface SentryId : NSObject
[BaseType (typeof(NSObject), Name = "_TtC6Sentry8SentryId")]
[Internal]
interface SentryId
{
    // @property (nonatomic, strong, class) SentryId * _Nonnull empty;
    [Static]
    [Export ("empty", ArgumentSemantic.Strong)]
    SentryId Empty { get; set; }

    // @property (readonly, copy, nonatomic) NSString * _Nonnull sentryIdString;
    [Export ("sentryIdString")]
    string SentryIdString { get; }

    // -(instancetype _Nonnull)initWithUuid:(NSUUID * _Nonnull)uuid __attribute__((objc_designated_initializer));
    [Export ("initWithUuid:")]
    [DesignatedInitializer]
    NativeHandle Constructor (NSUuid uuid);

    // -(instancetype _Nonnull)initWithUUIDString:(NSString * _Nonnull)uuidString __attribute__((objc_designated_initializer));
    [Export ("initWithUUIDString:")]
    [DesignatedInitializer]
    NativeHandle Constructor (string uuidString);

    // @property (readonly, nonatomic) NSUInteger hash;
    [Export ("hash")]
    nuint Hash { get; }
}

// @interface SentryLog : NSObject
[BaseType (typeof(NSObject), Name = "_TtC6Sentry9SentryLog")]
[DisableDefaultCtor]
[Internal]
interface SentryLog
{
    // @property (copy, nonatomic) NSDate * _Nonnull timestamp;
    [Export ("timestamp", ArgumentSemantic.Copy)]
    NSDate Timestamp { get; set; }

    // @property (nonatomic, strong) SentryId * _Nonnull traceId;
    [Export ("traceId", ArgumentSemantic.Strong)]
    SentryId TraceId { get; set; }

    // @property (nonatomic) enum SentryStructuredLogLevel level;
    [Export ("level", ArgumentSemantic.Assign)]
    SentryStructuredLogLevel Level { get; set; }

    // @property (copy, nonatomic) NSString * _Nonnull body;
    [Export ("body")]
    string Body { get; set; }

    // @property (copy, nonatomic) NSDictionary<NSString *,SentryStructuredLogAttribute *> * _Nonnull attributes;
    [Export ("attributes", ArgumentSemantic.Copy)]
    NSDictionary<NSString, NSObject> Attributes { get; set; }

    // @property (nonatomic, strong) NSNumber * _Nullable severityNumber;
    [NullAllowed, Export ("severityNumber", ArgumentSemantic.Strong)]
    NSNumber SeverityNumber { get; set; }
}

// @interface SentryLogger : NSObject
[BaseType (typeof(NSObject), Name = "_TtC6Sentry12SentryLogger")]
[DisableDefaultCtor]
[Internal]
interface SentryLogger
{
    // -(void)trace:(NSString * _Nonnull)body;
    [Export ("trace:")]
    void Trace (string body);

    // -(void)trace:(NSString * _Nonnull)body attributes:(NSDictionary<NSString *,id> * _Nonnull)attributes;
    [Export ("trace:attributes:")]
    void Trace (string body, NSDictionary<NSString, NSObject> attributes);

    // -(void)debug:(NSString * _Nonnull)body;
    [Export ("debug:")]
    void Debug (string body);

    // -(void)debug:(NSString * _Nonnull)body attributes:(NSDictionary<NSString *,id> * _Nonnull)attributes;
    [Export ("debug:attributes:")]
    void Debug (string body, NSDictionary<NSString, NSObject> attributes);

    // -(void)info:(NSString * _Nonnull)body;
    [Export ("info:")]
    void Info (string body);

    // -(void)info:(NSString * _Nonnull)body attributes:(NSDictionary<NSString *,id> * _Nonnull)attributes;
    [Export ("info:attributes:")]
    void Info (string body, NSDictionary<NSString, NSObject> attributes);

    // -(void)warn:(NSString * _Nonnull)body;
    [Export ("warn:")]
    void Warn (string body);

    // -(void)warn:(NSString * _Nonnull)body attributes:(NSDictionary<NSString *,id> * _Nonnull)attributes;
    [Export ("warn:attributes:")]
    void Warn (string body, NSDictionary<NSString, NSObject> attributes);

    // -(void)error:(NSString * _Nonnull)body;
    [Export ("error:")]
    void Error (string body);

    // -(void)error:(NSString * _Nonnull)body attributes:(NSDictionary<NSString *,id> * _Nonnull)attributes;
    [Export ("error:attributes:")]
    void Error (string body, NSDictionary<NSString, NSObject> attributes);

    // -(void)fatal:(NSString * _Nonnull)body;
    [Export ("fatal:")]
    void Fatal (string body);

    // -(void)fatal:(NSString * _Nonnull)body attributes:(NSDictionary<NSString *,id> * _Nonnull)attributes;
    [Export ("fatal:attributes:")]
    void Fatal (string body, NSDictionary<NSString, NSObject> attributes);
}

// @interface SentryProfileOptions : NSObject
[BaseType(typeof(NSObject), Name = "_TtC6Sentry20SentryProfileOptions")]
[DisableDefaultCtor]
[Internal]
interface SentryProfileOptions
{
    // @property(nonatomic) enum SentryProfileLifecycle lifecycle;
    [Export("lifecycle", ArgumentSemantic.Assign)]
    SentryProfileLifecycle Lifecycle { get; set; }

    // @property(nonatomic) float sessionSampleRate;
    [Export("sessionSampleRate")]
    float SessionSampleRate { get; set; }

    // @property(nonatomic) BOOL profileAppStarts;
    [Export("profileAppStarts")]
    bool ProfileAppStarts { get; set; }

    // - (nonnull instancetype) init OBJC_DESIGNATED_INITIALIZER;
    [Export("init")]
    [DesignatedInitializer]
    NativeHandle Constructor();
}

// @interface SentrySessionReplayIntegration : SentryBaseIntegration
[BaseType (typeof(NSObject))]
[Internal]
interface SentrySessionReplayIntegration
{
    // -(instancetype _Nonnull)initForManualUse:(SentryOptions * _Nonnull)options;
    [Export ("initForManualUse:")]
    NativeHandle Constructor (SentryOptions options);
    // -(BOOL)captureReplay;
    [Export ("captureReplay")]
    bool CaptureReplay();
    // -(void)configureReplayWith:(id<SentryReplayBreadcrumbConverter> _Nullable)breadcrumbConverter screenshotProvider:(id<SentryViewScreenshotProvider> _Nullable)screenshotProvider;
    [Export ("configureReplayWith:screenshotProvider:")]
    void ConfigureReplayWith ([NullAllowed] SentryReplayBreadcrumbConverter breadcrumbConverter, [NullAllowed] SentryViewScreenshotProvider screenshotProvider);
    // -(void)pause;
    [Export ("pause")]
    void Pause ();
    // -(void)resume;
    [Export ("resume")]
    void Resume ();
    // -(void)stop;
    [Export ("stop")]
    void Stop ();
    // -(void)start;
    [Export ("start")]
    void Start ();
    // +(id<SentryRRWebEvent> _Nonnull)createBreadcrumbwithTimestamp:(NSDate * _Nonnull)timestamp category:(NSString * _Nonnull)category message:(NSString * _Nullable)message level:(enum SentryLevel)level data:(NSDictionary<NSString *,id> * _Nullable)data;
    [Static]
    [Export ("createBreadcrumbwithTimestamp:category:message:level:data:")]
    SentryRRWebEvent CreateBreadcrumbwithTimestamp (NSDate timestamp, string category, [NullAllowed] string message, SentryLevel level, [NullAllowed] NSDictionary<NSString, NSObject> data);
    // +(id<SentryRRWebEvent> _Nonnull)createNetworkBreadcrumbWithTimestamp:(NSDate * _Nonnull)timestamp endTimestamp:(NSDate * _Nonnull)endTimestamp operation:(NSString * _Nonnull)operation description:(NSString * _Nonnull)description data:(NSDictionary<NSString *,id> * _Nonnull)data;
    [Static]
    [Export ("createNetworkBreadcrumbWithTimestamp:endTimestamp:operation:description:data:")]
    SentryRRWebEvent CreateNetworkBreadcrumbWithTimestamp (NSDate timestamp, NSDate endTimestamp, string operation, string description, NSDictionary<NSString, NSObject> data);
    // +(id<SentryReplayBreadcrumbConverter> _Nonnull)createDefaultBreadcrumbConverter;
    [Static]
    [Export ("createDefaultBreadcrumbConverter")]
    SentryReplayBreadcrumbConverter CreateDefaultBreadcrumbConverter();
}

[Protocol(Name = "_TtP6Sentry19SentryRedactOptions_")]
[Model]
[BaseType(typeof(NSObject))]
[Internal]
internal interface SentryRedactOptions
{
    [Abstract]
    [Export("maskAllText")]
    bool MaskAllText { get; }

    [Abstract]
    [Export("maskAllImages")]
    bool MaskAllImages { get; }

    [Abstract]
    [Export("maskedViewClasses", ArgumentSemantic.Copy)]
    Class[] MaskedViewClasses { get; }

    [Abstract]
    [Export("unmaskedViewClasses", ArgumentSemantic.Copy)]
    Class[] UnmaskedViewClasses { get; }
}

// @protocol SentryReplayBreadcrumbConverter <NSObject>
[Protocol (Name = "_TtP6Sentry31SentryReplayBreadcrumbConverter_")]
[BaseType (typeof(NSObject), Name = "_TtP6Sentry31SentryReplayBreadcrumbConverter_")]
[Model]
[Internal]
interface SentryReplayBreadcrumbConverter
{
    // @required -(id<SentryRRWebEvent> _Nullable)convertFrom:(SentryBreadcrumb * _Nonnull)breadcrumb __attribute__((warn_unused_result("")));
    [Abstract]
    [Export ("convertFrom:")]
    [return: NullAllowed]
    SentryRRWebEvent ConvertFrom (SentryBreadcrumb breadcrumb);
}

// @interface SentryReplayOptions : NSObject <SentryRedactOptions>
[BaseType (typeof(NSObject), Name = "_TtC6Sentry19SentryReplayOptions")]
[Internal]
interface SentryReplayOptions //: ISentryRedactOptions
{
    // @property (nonatomic) float sessionSampleRate;
    [Export ("sessionSampleRate")]
    float SessionSampleRate { get; set; }
    // @property (nonatomic) float onErrorSampleRate;
    [Export ("onErrorSampleRate")]
    float OnErrorSampleRate { get; set; }
    // @property (nonatomic) BOOL maskAllText;
    [Export ("maskAllText")]
    bool MaskAllText { get; set; }
    // @property (nonatomic) BOOL maskAllImages;
    [Export ("maskAllImages")]
    bool MaskAllImages { get; set; }
    // @property (nonatomic) enum SentryReplayQuality quality;
    [Export ("quality", ArgumentSemantic.Assign)]
    SentryReplayQuality Quality { get; set; }
    /*
    // @property (copy, nonatomic) NSArray<Class> * _Nonnull maskedViewClasses;
    //[Export ("maskedViewClasses", ArgumentSemantic.Copy)]
    //Class[] MaskedViewClasses { get; set; }
    // @property (copy, nonatomic) NSArray<Class> * _Nonnull unmaskedViewClasses;
    //[Export ("unmaskedViewClasses", ArgumentSemantic.Copy)]
    //Class[] UnmaskedViewClasses { get; set; }
    // @property (readonly, nonatomic) NSInteger replayBitRate;
    [Export ("replayBitRate")]
    nint ReplayBitRate { get; }
    // @property (readonly, nonatomic) float sizeScale;
    [Export ("sizeScale")]
    float SizeScale { get; }
    // @property (nonatomic) NSUInteger frameRate;
    [Export ("frameRate")]
    nuint FrameRate { get; set; }
    // @property (readonly, nonatomic) NSTimeInterval errorReplayDuration;
    [Export ("errorReplayDuration")]
    double ErrorReplayDuration { get; }
    // @property (readonly, nonatomic) NSTimeInterval sessionSegmentDuration;
    [Export ("sessionSegmentDuration")]
    double SessionSegmentDuration { get; }
    // @property (readonly, nonatomic) NSTimeInterval maximumDuration;
    [Export ("maximumDuration")]
    double MaximumDuration { get; }
    // -(instancetype _Nonnull)initWithSessionSampleRate:(float)sessionSampleRate onErrorSampleRate:(float)onErrorSampleRate maskAllText:(BOOL)maskAllText maskAllImages:(BOOL)maskAllImages __attribute__((objc_designated_initializer));
    [Export ("initWithSessionSampleRate:onErrorSampleRate:maskAllText:maskAllImages:")]
    [DesignatedInitializer]
    NativeHandle Constructor (float sessionSampleRate, float onErrorSampleRate, bool maskAllText, bool maskAllImages);
    // -(instancetype _Nonnull)initWithDictionary:(NSDictionary<NSString *,id> * _Nonnull)dictionary;
    [Export ("initWithDictionary:")]
    NativeHandle Constructor (NSDictionary<NSString, NSObject> dictionary);
    */
}

// @interface SentryRRWebEvent : NSObject <SentryRRWebEvent>
[BaseType (typeof(NSObject), Name = "_TtC6Sentry16SentryRRWebEvent")]
[Protocol]
[Model]
[DisableDefaultCtor]
[Internal]
interface SentryRRWebEvent : SentrySerializable
{
    // @property (readonly, nonatomic) enum SentryRRWebEventType type;
    [Export ("type")]
    SentryRRWebEventType Type { get; }
    // @property (readonly, copy, nonatomic) NSDate * _Nonnull timestamp;
    [Export ("timestamp", ArgumentSemantic.Copy)]
    NSDate Timestamp { get; }
    // @property (readonly, copy, nonatomic) NSDictionary<NSString *,id> * _Nullable data;
    [NullAllowed, Export ("data", ArgumentSemantic.Copy)]
    NSDictionary<NSString, NSObject> Data { get; }
    // -(instancetype _Nonnull)initWithType:(enum SentryRRWebEventType)type timestamp:(NSDate * _Nonnull)timestamp data:(NSDictionary<NSString *,id> * _Nullable)data __attribute__((objc_designated_initializer));
    [Export ("initWithType:timestamp:data:")]
    [DesignatedInitializer]
    NativeHandle Constructor (SentryRRWebEventType type, NSDate timestamp, [NullAllowed] NSDictionary<NSString, NSObject> data);
    // -(NSDictionary<NSString *,id> * _Nonnull)serialize __attribute__((warn_unused_result("")));
    [Export ("serialize")]
    new NSDictionary<NSString, NSObject> Serialize();
}

// @interface SentrySDK : NSObject
[BaseType(typeof(NSObject), Name = "_TtC6Sentry9SentrySDK")]
[DisableDefaultCtor]
[Internal]
interface SentrySDK
{
    // @property (readonly, nonatomic, strong, class) id<SentrySpan> _Nullable span;
    [Static]
    [NullAllowed, Export ("span", ArgumentSemantic.Strong)]
    SentrySpan Span { get; }

    // @property (readonly, nonatomic, class) BOOL isEnabled;
    [Static]
    [Export ("isEnabled")]
    bool IsEnabled { get; }

    // @property (readonly, nonatomic, strong, class) SentryReplayApi * _Nonnull replay;
    [Static]
    [Export ("replay", ArgumentSemantic.Strong)]
    SentryReplayApi Replay { get; }

    // @property (readonly, nonatomic, strong, class) SentryLogger * _Nonnull logger;
    [Static]
    [Export ("logger", ArgumentSemantic.Strong)]
    SentryLogger Logger { get; }

    // +(void)startWithOptions:(SentryOptions * _Nonnull)options;
    [Static]
    [Export ("startWithOptions:")]
    void StartWithOptions (SentryOptions options);

    // +(void)startWithConfigureOptions:(void (^ _Nonnull)(SentryOptions * _Nonnull))configureOptions;
    [Static]
    [Export ("startWithConfigureOptions:")]
    void StartWithConfigureOptions (Action<SentryOptions> configureOptions);

    // +(SentryId * _Nonnull)captureEvent:(SentryEvent * _Nonnull)event;
    [Static]
    [Export ("captureEvent:")]
    SentryId CaptureEvent (SentryEvent @event);

    // +(SentryId * _Nonnull)captureEvent:(SentryEvent * _Nonnull)event withScope:(SentryScope * _Nonnull)scope;
    [Static]
    [Export ("captureEvent:withScope:")]
    SentryId CaptureEvent (SentryEvent @event, SentryScope scope);

    // +(SentryId * _Nonnull)captureEvent:(SentryEvent * _Nonnull)event withScopeBlock:(void (^ _Nonnull)(SentryScope * _Nonnull))block;
    [Static]
    [Export ("captureEvent:withScopeBlock:")]
    SentryId CaptureEvent (SentryEvent @event, Action<SentryScope> block);

    // +(id<SentrySpan> _Nonnull)startTransactionWithName:(NSString * _Nonnull)name operation:(NSString * _Nonnull)operation;
    [Static]
    [Export ("startTransactionWithName:operation:")]
    SentrySpan StartTransactionWithName (string name, string operation);

    // +(id<SentrySpan> _Nonnull)startTransactionWithName:(NSString * _Nonnull)name operation:(NSString * _Nonnull)operation bindToScope:(BOOL)bindToScope;
    [Static]
    [Export ("startTransactionWithName:operation:bindToScope:")]
    SentrySpan StartTransactionWithName (string name, string operation, bool bindToScope);

    // +(id<SentrySpan> _Nonnull)startTransactionWithContext:(SentryTransactionContext * _Nonnull)transactionContext;
    [Static]
    [Export ("startTransactionWithContext:")]
    SentrySpan StartTransactionWithContext (SentryTransactionContext transactionContext);

    // +(id<SentrySpan> _Nonnull)startTransactionWithContext:(SentryTransactionContext * _Nonnull)transactionContext bindToScope:(BOOL)bindToScope;
    [Static]
    [Export ("startTransactionWithContext:bindToScope:")]
    SentrySpan StartTransactionWithContext (SentryTransactionContext transactionContext, bool bindToScope);

    // +(id<SentrySpan> _Nonnull)startTransactionWithContext:(SentryTransactionContext * _Nonnull)transactionContext bindToScope:(BOOL)bindToScope customSamplingContext:(NSDictionary<NSString *,id> * _Nonnull)customSamplingContext;
    [Static]
    [Export ("startTransactionWithContext:bindToScope:customSamplingContext:")]
    SentrySpan StartTransactionWithContext (SentryTransactionContext transactionContext, bool bindToScope, NSDictionary<NSString, NSObject> customSamplingContext);

    // +(id<SentrySpan> _Nonnull)startTransactionWithContext:(SentryTransactionContext * _Nonnull)transactionContext customSamplingContext:(NSDictionary<NSString *,id> * _Nonnull)customSamplingContext;
    [Static]
    [Export ("startTransactionWithContext:customSamplingContext:")]
    SentrySpan StartTransactionWithContext (SentryTransactionContext transactionContext, NSDictionary<NSString, NSObject> customSamplingContext);

    // +(SentryId * _Nonnull)captureError:(NSError * _Nonnull)error;
    [Static]
    [Export ("captureError:")]
    SentryId CaptureError (NSError error);

    // +(SentryId * _Nonnull)captureError:(NSError * _Nonnull)error withScope:(SentryScope * _Nonnull)scope;
    [Static]
    [Export ("captureError:withScope:")]
    SentryId CaptureError (NSError error, SentryScope scope);

    // +(SentryId * _Nonnull)captureError:(NSError * _Nonnull)error withScopeBlock:(void (^ _Nonnull)(SentryScope * _Nonnull))block;
    [Static]
    [Export ("captureError:withScopeBlock:")]
    SentryId CaptureError (NSError error, Action<SentryScope> block);

    // +(SentryId * _Nonnull)captureException:(NSException * _Nonnull)exception;
    [Static]
    [Export ("captureException:")]
    SentryId CaptureException (NSException exception);

    // +(SentryId * _Nonnull)captureException:(NSException * _Nonnull)exception withScope:(SentryScope * _Nonnull)scope;
    [Static]
    [Export ("captureException:withScope:")]
    SentryId CaptureException (NSException exception, SentryScope scope);

    // +(SentryId * _Nonnull)captureException:(NSException * _Nonnull)exception withScopeBlock:(void (^ _Nonnull)(SentryScope * _Nonnull))block;
    [Static]
    [Export ("captureException:withScopeBlock:")]
    SentryId CaptureException (NSException exception, Action<SentryScope> block);

    // +(SentryId * _Nonnull)captureMessage:(NSString * _Nonnull)message;
    [Static]
    [Export ("captureMessage:")]
    SentryId CaptureMessage (string message);

    // +(SentryId * _Nonnull)captureMessage:(NSString * _Nonnull)message withScope:(SentryScope * _Nonnull)scope;
    [Static]
    [Export ("captureMessage:withScope:")]
    SentryId CaptureMessage (string message, SentryScope scope);

    // +(SentryId * _Nonnull)captureMessage:(NSString * _Nonnull)message withScopeBlock:(void (^ _Nonnull)(SentryScope * _Nonnull))block;
    [Static]
    [Export ("captureMessage:withScopeBlock:")]
    SentryId CaptureMessage (string message, Action<SentryScope> block);

    // +(void)captureUserFeedback:(SentryUserFeedback * _Nonnull)userFeedback __attribute__((deprecated("Use SentrySDK.back or use or configure our new managed UX with SentryOptions.configureUserFeedback.")));
    [Static]
    [Export ("captureUserFeedback:")]
    void CaptureUserFeedback (SentryUserFeedback userFeedback);

    // +(void)captureFeedback:(SentryFeedback * _Nonnull)feedback;
    [Static]
    [Export ("captureFeedback:")]
    void CaptureFeedback (SentryFeedback feedback);

    // @property (readonly, nonatomic, strong, class) SentryFeedbackAPI * _Nonnull feedback __attribute__((availability(ios, introduced=13.0)));
    [Static]
    [Export ("feedback", ArgumentSemantic.Strong)]
    SentryFeedbackAPI Feedback { get; }

    // +(void)addBreadcrumb:(SentryBreadcrumb * _Nonnull)crumb;
    [Static]
    [Export ("addBreadcrumb:")]
    void AddBreadcrumb (SentryBreadcrumb crumb);

    // +(void)configureScope:(void (^ _Nonnull)(SentryScope * _Nonnull))callback;
    [Static]
    [Export ("configureScope:")]
    void ConfigureScope (Action<SentryScope> callback);

    // @property (readonly, nonatomic, class) BOOL crashedLastRun;
    [Static]
    [Export ("crashedLastRun")]
    bool CrashedLastRun { get; }

    // @property (readonly, nonatomic, class) BOOL detectedStartUpCrash;
    [Static]
    [Export ("detectedStartUpCrash")]
    bool DetectedStartUpCrash { get; }

    // +(void)setUser:(SentryUser * _Nullable)user;
    [Static]
    [Export ("setUser:")]
    void SetUser ([NullAllowed] SentryUser user);

    // +(void)startSession;
    [Static]
    [Export ("startSession")]
    void StartSession ();

    // +(void)endSession;
    [Static]
    [Export ("endSession")]
    void EndSession ();

    // +(void)crash;
    [Static]
    [Export ("crash")]
    void Crash ();

    // +(void)reportFullyDisplayed;
    [Static]
    [Export ("reportFullyDisplayed")]
    void ReportFullyDisplayed ();

    // +(void)pauseAppHangTracking;
    [Static]
    [Export ("pauseAppHangTracking")]
    void PauseAppHangTracking ();

    // +(void)resumeAppHangTracking;
    [Static]
    [Export ("resumeAppHangTracking")]
    void ResumeAppHangTracking ();

    // +(void)flush:(NSTimeInterval)timeout;
    [Static]
    [Export ("flush:")]
    void Flush (double timeout);

    // +(void)close;
    [Static]
    [Export ("close")]
    void Close ();

    // +(void)startProfiler;
    [Static]
    [Export ("startProfiler")]
    void StartProfiler ();

    // +(void)stopProfiler;
    [Static]
    [Export ("stopProfiler")]
    void StopProfiler ();

    // +(void)clearLogger;
    [Static]
    [Export ("clearLogger")]
    void ClearLogger ();
}

// @interface SentrySession : NSObject <NSCopying>
[BaseType (typeof(NSObject), Name = "_TtC6Sentry13SentrySession")]
[DisableDefaultCtor]
[Internal]
interface SentrySession
{
    // -(instancetype _Nonnull)initWithReleaseName:(NSString * _Nonnull)releaseName distinctId:(NSString * _Nonnull)distinctId __attribute__((objc_designated_initializer));
    [Export ("initWithReleaseName:distinctId:")]
    [DesignatedInitializer]
    NativeHandle Constructor (string releaseName, string distinctId);

    // -(instancetype _Nullable)initWithJSONObject:(NSDictionary<NSString *,id> * _Nonnull)jsonObject __attribute__((objc_designated_initializer));
    [Export ("initWithJSONObject:")]
    [DesignatedInitializer]
    NativeHandle Constructor (NSDictionary<NSString, NSObject> jsonObject);

    // -(void)endSessionExitedWithTimestamp:(NSDate * _Nonnull)timestamp;
    [Export ("endSessionExitedWithTimestamp:")]
    void EndSessionExitedWithTimestamp (NSDate timestamp);

    // -(void)endSessionCrashedWithTimestamp:(NSDate * _Nonnull)timestamp;
    [Export ("endSessionCrashedWithTimestamp:")]
    void EndSessionCrashedWithTimestamp (NSDate timestamp);

    // -(void)endSessionAbnormalWithTimestamp:(NSDate * _Nonnull)timestamp;
    [Export ("endSessionAbnormalWithTimestamp:")]
    void EndSessionAbnormalWithTimestamp (NSDate timestamp);

    // -(void)incrementErrors;
    [Export ("incrementErrors")]
    void IncrementErrors ();

    // @property (readonly, copy, nonatomic) NSUUID * _Nonnull sessionId;
    [Export ("sessionId", ArgumentSemantic.Copy)]
    NSUuid SessionId { get; }

    // @property (readonly, copy, nonatomic) NSDate * _Nonnull started;
    [Export ("started", ArgumentSemantic.Copy)]
    NSDate Started { get; }

    // @property (readonly, nonatomic) enum SentrySessionStatus status;
    [Export ("status")]
    SentrySessionStatus Status { get; }

    // @property (nonatomic) NSUInteger errors;
    [Export ("errors")]
    nuint Errors { get; set; }

    // @property (readonly, nonatomic) NSUInteger sequence;
    [Export ("sequence")]
    nuint Sequence { get; }

    // @property (readonly, copy, nonatomic) NSString * _Nonnull distinctId;
    [Export ("distinctId")]
    string DistinctId { get; }

    // @property (readonly, nonatomic, strong) NSNumber * _Nullable flagInit;
    [NullAllowed, Export ("flagInit", ArgumentSemantic.Strong)]
    NSNumber FlagInit { get; }

    // @property (readonly, copy, nonatomic) NSDate * _Nullable timestamp;
    [NullAllowed, Export ("timestamp", ArgumentSemantic.Copy)]
    NSDate Timestamp { get; }

    // @property (readonly, nonatomic, strong) NSNumber * _Nullable duration;
    [NullAllowed, Export ("duration", ArgumentSemantic.Strong)]
    NSNumber Duration { get; }

    // @property (readonly, copy, nonatomic) NSString * _Nullable releaseName;
    [NullAllowed, Export ("releaseName")]
    string ReleaseName { get; }

    // @property (copy, nonatomic) NSString * _Nullable environment;
    [NullAllowed, Export ("environment")]
    string Environment { get; set; }

    // @property (nonatomic, strong) SentryUser * _Nullable user;
    [NullAllowed, Export ("user", ArgumentSemantic.Strong)]
    SentryUser User { get; set; }

    // @property (copy, nonatomic) NSString * _Nullable abnormalMechanism;
    [NullAllowed, Export ("abnormalMechanism")]
    string AbnormalMechanism { get; set; }

    // -(NSDictionary<NSString *,id> * _Nonnull)serialize __attribute__((warn_unused_result("")));
    [Export ("serialize")]
    new NSDictionary<NSString, NSObject> Serialize();

    // -(void)setFlagInit;
    [Export ("setFlagInit")]
    void SetFlagInit ();

    // -(id _Nonnull)copyWithZone:(struct _NSZone * _Nullable)zone __attribute__((warn_unused_result("")));
    // [Export ("copyWithZone:")]
    // unsafe NSObject CopyWithZone ([NullAllowed] _NSZone* zone);
}

// @interface SentryUserFeedback : NSObject <SentrySerializable>
[BaseType(typeof(NSObject))]
[DisableDefaultCtor]
[Internal]
interface SentryUserFeedback : SentrySerializable
{
    // @property (nonatomic, readonly, strong) SentryId * _Nonnull eventId;
    [Export("eventId", ArgumentSemantic.Strong)]
    SentryId EventId { get; }

    // @property (nonatomic, copy) NSString * _Nonnull name;
    [Export("name")]
    string Name { get; set; }

    // @property (nonatomic, copy) NSString * _Nonnull email;
    [Export("email")]
    string Email { get; set; }

    // @property (nonatomic, copy) NSString * _Nonnull comments;
    [Export("comments")]
    string Comments { get; set; }

    // - (nonnull instancetype)initWithEventId:(SentryId * _Nonnull)eventId OBJC_DESIGNATED_INITIALIZER;
    [Export("initWithEventId:")]
    NativeHandle Constructor(SentryId eventId);
}

[BaseType(typeof(NSObject), Name = "_TtC6Sentry31SentryUserFeedbackConfiguration")]
[DisableDefaultCtor]
[Internal]
interface SentryUserFeedbackConfiguration
{
    [Export("animations")]
    bool Animations { get; set; }

    [NullAllowed, Export("configureWidget", ArgumentSemantic.Copy)]
    Action<SentryUserFeedbackWidgetConfiguration> ConfigureWidget { get; set; }

    [Export("widgetConfig", ArgumentSemantic.Strong)]
    SentryUserFeedbackWidgetConfiguration WidgetConfig { get; set; }

    [Export("useShakeGesture")]
    bool UseShakeGesture { get; set; }

    [Export("showFormForScreenshots")]
    bool ShowFormForScreenshots { get; set; }

    // [NullAllowed, Export("configureForm", ArgumentSemantic.Copy)]
    // Action<SentryUserFeedbackFormConfiguration> ConfigureForm { get; set; }

    // [Export("formConfig", ArgumentSemantic.Strong)]
    // SentryUserFeedbackFormConfiguration FormConfig { get; set; }

    [NullAllowed, Export("tags", ArgumentSemantic.Copy)]
    NSDictionary<NSString, NSObject> Tags { get; set; }

    [NullAllowed, Export("onFormOpen", ArgumentSemantic.Copy)]
    Action OnFormOpen { get; set; }

    [NullAllowed, Export("onFormClose", ArgumentSemantic.Copy)]
    Action OnFormClose { get; set; }

    [NullAllowed, Export("onSubmitSuccess", ArgumentSemantic.Copy)]
    Action<NSDictionary<NSString, NSObject>> OnSubmitSuccess { get; set; }

    [NullAllowed, Export("onSubmitError", ArgumentSemantic.Copy)]
    Action<NSError> OnSubmitError { get; set; }

    // [NullAllowed, Export("configureTheme", ArgumentSemantic.Copy)]
    // Action<SentryUserFeedbackThemeConfiguration> ConfigureTheme { get; set; }
    //
    // [Export("theme", ArgumentSemantic.Strong)]
    // SentryUserFeedbackThemeConfiguration Theme { get; set; }
    //
    // [NullAllowed, Export("configureDarkTheme", ArgumentSemantic.Copy)]
    // Action<SentryUserFeedbackThemeConfiguration> ConfigureDarkTheme { get; set; }
    //
    // [Export("darkTheme", ArgumentSemantic.Strong)]
    // SentryUserFeedbackThemeConfiguration DarkTheme { get; set; }

    [Export("textEffectiveHeightCenter")]
    nfloat TextEffectiveHeightCenter { get; set; }

    [Export("scaleFactor")]
    nfloat ScaleFactor { get; set; }

    [Export("calculateScaleFactor")]
    nfloat CalculateScaleFactor();

    [Export("paddingScaleFactor")]
    nfloat PaddingScaleFactor { get; set; }

    [Export("calculatePaddingScaleFactor")]
    nfloat CalculatePaddingScaleFactor();

    [Export("recalculateScaleFactors")]
    void RecalculateScaleFactors();

    [Export("padding")]
    nfloat Padding { get; }

    [Export("spacing")]
    nfloat Spacing { get; }

    [Export("margin")]
    nfloat Margin { get; }

    [Export("init")]
    [DesignatedInitializer]
    IntPtr Constructor();
}

// [BaseType(typeof(NSObject), Name = "_TtC6Sentry37SentryUserFeedbackThemeConfiguration")]
// [DisableDefaultCtor]
// [Internal]
// interface SentryUserFeedbackThemeConfiguration
// {
//     [Export("backgroundColor", ArgumentSemantic.Strong)]
//     UIColor BackgroundColor { get; set; }
//
//     [Export("textColor", ArgumentSemantic.Strong)]
//     UIColor TextColor { get; set; }
//
//     [Export("buttonColor", ArgumentSemantic.Strong)]
//     UIColor ButtonColor { get; set; }
//
//     [Export("buttonTextColor", ArgumentSemantic.Strong)]
//     UIColor ButtonTextColor { get; set; }
//
//     [Export("init")]
//     [DesignatedInitializer]
//     IntPtr Constructor();
// }

[BaseType(typeof(NSObject), Name = "_TtC6Sentry37SentryUserFeedbackWidgetConfiguration")]
[DisableDefaultCtor]
[Internal]
interface SentryUserFeedbackWidgetConfiguration
{
    [Export("autoInject")]
    bool AutoInject { get; set; }

    [Export("defaultLabelText", ArgumentSemantic.Copy)]
    string DefaultLabelText { get; }

    [NullAllowed, Export("labelText", ArgumentSemantic.Copy)]
    string LabelText { get; set; }

    [Export("showIcon")]
    bool ShowIcon { get; set; }

    [NullAllowed, Export("widgetAccessibilityLabel", ArgumentSemantic.Copy)]
    string WidgetAccessibilityLabel { get; set; }

    [Export("windowLevel")]
    nfloat WindowLevel { get; set; }

    [Export("location")]
    NSDirectionalRectEdge Location { get; set; }

    [Export("layoutUIOffset")]
    UIOffset LayoutUIOffset { get; set; }

    [Export("init")]
    [DesignatedInitializer]
    IntPtr Constructor();
}


// [BaseType(typeof(NSObject), Name = "_TtC6Sentry37SentryUserFeedbackFormConfiguration")]
// [DisableDefaultCtor]
// [Internal]
// interface SentryUserFeedbackFormConfiguration
// {
//     [Export("title", ArgumentSemantic.Copy)]
//     string Title { get; set; }
//
//     [Export("subtitle", ArgumentSemantic.Copy)]
//     string Subtitle { get; set; }
//
//     [Export("submitButtonTitle", ArgumentSemantic.Copy)]
//     string SubmitButtonTitle { get; set; }
//
//     [Export("cancelButtonTitle", ArgumentSemantic.Copy)]
//     string CancelButtonTitle { get; set; }
//
//     [Export("thankYouMessage", ArgumentSemantic.Copy)]
//     string ThankYouMessage { get; set; }
//
//     [Export("init")]
//     [DesignatedInitializer]
//     IntPtr Constructor();
// }

// @interface SentryViewScreenshotOptions : NSObject <SentryRedactOptions>
[BaseType (typeof(NSObject), Name = "_TtC6Sentry27SentryViewScreenshotOptions")]
[Internal]
interface SentryViewScreenshotOptions //: ISentryRedactOptions
{
    // @property (nonatomic) BOOL enableViewRendererV2;
    [Export ("enableViewRendererV2")]
    bool EnableViewRendererV2 { get; set; }

    // @property (nonatomic) BOOL enableFastViewRendering;
    [Export ("enableFastViewRendering")]
    bool EnableFastViewRendering { get; set; }

    // @property (nonatomic) BOOL maskAllImages;
    [Export ("maskAllImages")]
    bool MaskAllImages { get; set; }

    // @property (nonatomic) BOOL maskAllText;
    [Export ("maskAllText")]
    bool MaskAllText { get; set; }

    // @property (copy, nonatomic) NSArray<Class> * _Nonnull maskedViewClasses;
    [Export ("maskedViewClasses", ArgumentSemantic.Copy)]
    Class[] MaskedViewClasses { get; set; }

    // @property (copy, nonatomic) NSArray<Class> * _Nonnull unmaskedViewClasses;
    [Export ("unmaskedViewClasses", ArgumentSemantic.Copy)]
    Class[] UnmaskedViewClasses { get; set; }

    // -(instancetype _Nonnull)initWithEnableViewRendererV2:(BOOL)enableViewRendererV2 enableFastViewRendering:(BOOL)enableFastViewRendering maskAllText:(BOOL)maskAllText maskAllImages:(BOOL)maskAllImages maskedViewClasses:(NSArray<Class> * _Nonnull)maskedViewClasses unmaskedViewClasses:(NSArray<Class> * _Nonnull)unmaskedViewClasses __attribute__((objc_designated_initializer));
    [Export ("initWithEnableViewRendererV2:enableFastViewRendering:maskAllText:maskAllImages:maskedViewClasses:unmaskedViewClasses:")]
    [DesignatedInitializer]
    NativeHandle Constructor (bool enableViewRendererV2, bool enableFastViewRendering, bool maskAllText, bool maskAllImages, Class[] maskedViewClasses, Class[] unmaskedViewClasses);
}

// @protocol SentryViewScreenshotProvider <NSObject>
[Protocol (Name = "_TtP6Sentry28SentryViewScreenshotProvider_")]
[Model]
[BaseType (typeof(NSObject), Name = "_TtP6Sentry28SentryViewScreenshotProvider_")]
[Internal]
interface SentryViewScreenshotProvider
{
    // @required -(void)imageWithView:(UIView * _Nonnull)view onComplete:(void (^ _Nonnull)(UIImage * _Nonnull))onComplete;
    [Abstract]
    [Export ("imageWithView:onComplete:")]
    void OnComplete (UIView view, Action<UIImage> onComplete);
}
