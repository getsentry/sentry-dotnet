using System;
using Foundation;
using ObjCRuntime;

namespace SentryCocoa;

[Static]
[Internal]
interface Constants
{
    // extern double SentryVersionNumber;
    [Field ("SentryVersionNumber", "__Internal")]
    double SentryVersionNumber { get; }

    // extern const unsigned char[] SentryVersionString;
    [Field ("SentryVersionString", "__Internal")]
    [return: PlainString]
    NSString SentryVersionString { get; }

    // extern NSString *const _Nonnull SentryErrorDomain __attribute__((visibility("default")));
    [Field ("SentryErrorDomain", "__Internal")]
    NSString SentryErrorDomain { get; }
}

// Xamarin bug: Delegate types don't honor the [Internal] attribute.
// Workaround by using Func<T> or Action<T> instead.
// See: https://github.com/xamarin/xamarin-macios/issues/15299
// Leaving code commented here so we can more easily go back when the issue is fixed.

// // typedef void (^SentryRequestFinished)(NSError * _Nullable);
// [Internal]
// delegate void SentryRequestFinished ([NullAllowed] NSError arg0);

// // typedef void (^SentryRequestOperationFinished)(NSHTTPURLResponse * _Nullable, NSError * _Nullable);
// [Internal]
// delegate void SentryRequestOperationFinished ([NullAllowed] NSHttpUrlResponse arg0, [NullAllowed] NSError arg1);

// // typedef SentryBreadcrumb * _Nullable (^SentryBeforeBreadcrumbCallback)(SentryBreadcrumb * _Nonnull);
// [Internal]
// delegate SentryBreadcrumb SentryBeforeBreadcrumbCallback (SentryBreadcrumb arg0);

// // typedef SentryEvent * _Nullable (^SentryBeforeSendEventCallback)(SentryEvent * _Nonnull);
// [Internal]
// delegate SentryEvent SentryBeforeSendEventCallback (SentryEvent arg0);

// // typedef void (^SentryOnCrashedLastRunCallback)(SentryEvent * _Nonnull);
// [Internal]
// delegate void SentryOnCrashedLastRunCallback (SentryEvent arg0);

// // typedef BOOL (^SentryShouldQueueEvent)(NSHTTPURLResponse * _Nullable, NSError * _Nullable);
// [Internal]
// delegate bool SentryShouldQueueEvent ([NullAllowed] NSHttpUrlResponse arg0, [NullAllowed] NSError arg1);

// // typedef NSNumber * _Nullable (^SentryTracesSamplerCallback)(SentrySamplingContext * _Nonnull);
// [Internal]
// delegate NSNumber SentryTracesSamplerCallback (SentrySamplingContext arg0);

// typedef void (^SentrySpanCallback)(id<SentrySpan> _Nullable);
// [Internal]
// delegate void SentrySpanCallback ([NullAllowed] SentrySpan arg0);

// // typedef void (^SentryOnAppStartMeasurementAvailable)(SentryAppStartMeasurement * _Nullable);
// [Internal]
// delegate void SentryOnAppStartMeasurementAvailable ([NullAllowed] SentryAppStartMeasurement arg0);

// @interface PrivateSentrySDKOnly : NSObject
[BaseType (typeof(NSObject), Name="PrivateSentrySDKOnly")]
[Internal]
interface PrivateSentrySdkOnly
{
    // +(void)storeEnvelope:(SentryEnvelope * _Nonnull)envelope;
    [Static]
    [Export ("storeEnvelope:")]
    void StoreEnvelope (SentryEnvelope envelope);

    // +(void)captureEnvelope:(SentryEnvelope * _Nonnull)envelope;
    [Static]
    [Export ("captureEnvelope:")]
    void CaptureEnvelope (SentryEnvelope envelope);

    // +(SentryEnvelope * _Nullable)envelopeWithData:(NSData * _Nonnull)data;
    [Static]
    [Export ("envelopeWithData:")]
    [return: NullAllowed]
    SentryEnvelope EnvelopeWithData (NSData data);

    // +(NSArray<SentryDebugMeta *> * _Nonnull)getDebugImages;
    [Static]
    [Export ("getDebugImages")]
    SentryDebugMeta[] DebugImages { get; }

    // +(void)setSdkName:(NSString * _Nonnull)sdkName andVersionString:(NSString * _Nonnull)versionString;
    [Static]
    [Export ("setSdkName:andVersionString:")]
    void SetSdkName (string sdkName, string versionString);

    // @property (copy, nonatomic, class) SentryOnAppStartMeasurementAvailable _Nullable onAppStartMeasurementAvailable;
    [Static]
    [NullAllowed, Export ("onAppStartMeasurementAvailable", ArgumentSemantic.Copy)]
    Action<SentryAppStartMeasurement?> OnAppStartMeasurementAvailable { get; set; }

    // @property (readonly, nonatomic, class) SentryAppStartMeasurement * _Nullable appStartMeasurement;
    [Static]
    [NullAllowed, Export ("appStartMeasurement")]
    SentryAppStartMeasurement AppStartMeasurement { get; }

    // @property (readonly, copy, nonatomic, class) NSString * _Nonnull installationID;
    [Static]
    [Export ("installationID")]
    string InstallationID { get; }

    // @property (readonly, copy, nonatomic, class) SentryOptions * _Nonnull options;
    [Static]
    [Export ("options", ArgumentSemantic.Copy)]
    SentryOptions Options { get; }

    // @property (assign, nonatomic, class) BOOL appStartMeasurementHybridSDKMode;
    [Static]
    [Export ("appStartMeasurementHybridSDKMode")]
    bool AppStartMeasurementHybridSdkMode { get; set; }

    // @property (assign, nonatomic, class) BOOL framesTrackingMeasurementHybridSDKMode;
    [Static]
    [Export ("framesTrackingMeasurementHybridSDKMode")]
    bool FramesTrackingMeasurementHybridSdkMode { get; set; }

    // @property (readonly, assign, nonatomic, class) BOOL isFramesTrackingRunning;
    [Static]
    [Export ("isFramesTrackingRunning")]
    bool IsFramesTrackingRunning { get; }

    // @property (readonly, assign, nonatomic, class) SentryScreenFrames * _Nonnull currentScreenFrames;
    [Static]
    [Export ("currentScreenFrames", ArgumentSemantic.Assign)]
    SentryScreenFrames CurrentScreenFrames { get; }
}

// @interface SentryAppStartMeasurement : NSObject
[BaseType (typeof(NSObject))]
[DisableDefaultCtor]
[Internal]
interface SentryAppStartMeasurement
{
    // -(instancetype _Nonnull)initWithType:(SentryAppStartType)type appStartTimestamp:(NSDate * _Nonnull)appStartTimestamp duration:(NSTimeInterval)duration runtimeInitTimestamp:(NSDate * _Nonnull)runtimeInitTimestamp didFinishLaunchingTimestamp:(NSDate * _Nonnull)didFinishLaunchingTimestamp __attribute__((deprecated("Use initWithType:appStartTimestamp:duration:mainTimestamp:runtimeInitTimestamp:didFinishLaunchingTimestamp instead.")));
    [Export ("initWithType:appStartTimestamp:duration:runtimeInitTimestamp:didFinishLaunchingTimestamp:")]
    NativeHandle Constructor (SentryAppStartType type, NSDate appStartTimestamp, double duration, NSDate runtimeInitTimestamp, NSDate didFinishLaunchingTimestamp);

    // -(instancetype _Nonnull)initWithType:(SentryAppStartType)type appStartTimestamp:(NSDate * _Nonnull)appStartTimestamp duration:(NSTimeInterval)duration runtimeInitTimestamp:(NSDate * _Nonnull)runtimeInitTimestamp moduleInitializationTimestamp:(NSDate * _Nonnull)moduleInitializationTimestamp didFinishLaunchingTimestamp:(NSDate * _Nonnull)didFinishLaunchingTimestamp;
    [Export ("initWithType:appStartTimestamp:duration:runtimeInitTimestamp:moduleInitializationTimestamp:didFinishLaunchingTimestamp:")]
    NativeHandle Constructor (SentryAppStartType type, NSDate appStartTimestamp, double duration, NSDate runtimeInitTimestamp, NSDate moduleInitializationTimestamp, NSDate didFinishLaunchingTimestamp);

    // @property (readonly, assign, nonatomic) SentryAppStartType type;
    [Export ("type", ArgumentSemantic.Assign)]
    SentryAppStartType Type { get; }

    // @property (readonly, assign, nonatomic) NSTimeInterval duration;
    [Export ("duration")]
    double Duration { get; }

    // @property (readonly, nonatomic, strong) NSDate * _Nonnull appStartTimestamp;
    [Export ("appStartTimestamp", ArgumentSemantic.Strong)]
    NSDate AppStartTimestamp { get; }

    // @property (readonly, nonatomic, strong) NSDate * _Nonnull runtimeInitTimestamp;
    [Export ("runtimeInitTimestamp", ArgumentSemantic.Strong)]
    NSDate RuntimeInitTimestamp { get; }

    // @property (readonly, nonatomic, strong) NSDate * _Nonnull moduleInitializationTimestamp;
    [Export ("moduleInitializationTimestamp", ArgumentSemantic.Strong)]
    NSDate ModuleInitializationTimestamp { get; }

    // @property (readonly, nonatomic, strong) NSDate * _Nonnull didFinishLaunchingTimestamp;
    [Export ("didFinishLaunchingTimestamp", ArgumentSemantic.Strong)]
    NSDate DidFinishLaunchingTimestamp { get; }
}

// @protocol SentrySerializable <NSObject>
[Protocol] [Model]
[BaseType (typeof(NSObject))]
[DisableDefaultCtor]
[Internal]
interface SentrySerializable
{
    // @required -(NSDictionary<NSString *,id> * _Nonnull)serialize;
    [Abstract]
    [Export ("serialize")]
    NSDictionary<NSString, NSObject> Serialize();
}

// @interface SentryAttachment : NSObject
[BaseType (typeof(NSObject))]
[DisableDefaultCtor]
[Internal]
interface SentryAttachment
{
    // -(instancetype _Nonnull)initWithData:(NSData * _Nonnull)data filename:(NSString * _Nonnull)filename;
    [Export ("initWithData:filename:")]
    NativeHandle Constructor (NSData data, string filename);

    // -(instancetype _Nonnull)initWithData:(NSData * _Nonnull)data filename:(NSString * _Nonnull)filename contentType:(NSString * _Nonnull)contentType;
    [Export ("initWithData:filename:contentType:")]
    NativeHandle Constructor (NSData data, string filename, string contentType);

    // -(instancetype _Nonnull)initWithPath:(NSString * _Nonnull)path;
    [Export ("initWithPath:")]
    NativeHandle Constructor (string path);

    // -(instancetype _Nonnull)initWithPath:(NSString * _Nonnull)path filename:(NSString * _Nonnull)filename;
    [Export ("initWithPath:filename:")]
    NativeHandle Constructor (string path, string filename);

    // -(instancetype _Nonnull)initWithPath:(NSString * _Nonnull)path filename:(NSString * _Nonnull)filename contentType:(NSString * _Nonnull)contentType;
    [Export ("initWithPath:filename:contentType:")]
    NativeHandle Constructor (string path, string filename, string contentType);

    // @property (readonly, nonatomic, strong) NSData * _Nullable data;
    [NullAllowed, Export ("data", ArgumentSemantic.Strong)]
    NSData Data { get; }

    // @property (readonly, copy, nonatomic) NSString * _Nullable path;
    [NullAllowed, Export ("path")]
    string Path { get; }

    // @property (readonly, copy, nonatomic) NSString * _Nonnull filename;
    [Export ("filename")]
    string Filename { get; }

    // @property (readonly, copy, nonatomic) NSString * _Nonnull contentType;
    [Export ("contentType")]
    string ContentType { get; }
}

// @interface SentryBreadcrumb : NSObject <SentrySerializable>
[BaseType (typeof(NSObject))]
[Internal]
interface SentryBreadcrumb : SentrySerializable
{
    // @property (nonatomic) SentryLevel level;
    [Export ("level", ArgumentSemantic.Assign)]
    SentryLevel Level { get; set; }

    // @property (copy, nonatomic) NSString * _Nonnull category;
    [Export ("category")]
    string Category { get; set; }

    // @property (nonatomic, strong) NSDate * _Nullable timestamp;
    [NullAllowed, Export ("timestamp", ArgumentSemantic.Strong)]
    NSDate Timestamp { get; set; }

    // @property (copy, nonatomic) NSString * _Nullable type;
    [NullAllowed, Export ("type")]
    string Type { get; set; }

    // @property (copy, nonatomic) NSString * _Nullable message;
    [NullAllowed, Export ("message")]
    string Message { get; set; }

    // @property (nonatomic, strong) NSDictionary<NSString *,id> * _Nullable data;
    [NullAllowed, Export ("data", ArgumentSemantic.Strong)]
    NSDictionary<NSString, NSObject> Data { get; set; }

    // -(instancetype _Nonnull)initWithLevel:(SentryLevel)level category:(NSString * _Nonnull)category;
    [Export ("initWithLevel:category:")]
    NativeHandle Constructor (SentryLevel level, string category);

    // -(NSDictionary<NSString *,id> * _Nonnull)serialize;
    [Export ("serialize")]
    NSDictionary<NSString, NSObject> Serialize();

    // // -(BOOL)isEqual:(id _Nullable)other;
    // [Export ("isEqual:")]
    // bool IsEqual ([NullAllowed] NSObject other);

    // -(BOOL)isEqualToBreadcrumb:(SentryBreadcrumb * _Nonnull)breadcrumb;
    [Export ("isEqualToBreadcrumb:")]
    bool IsEqualToBreadcrumb (SentryBreadcrumb breadcrumb);

    // -(NSUInteger)hash;
    [Export ("hash")]
    nuint Hash { get; }
}

// @interface SentryClient : NSObject
[BaseType (typeof(NSObject))]
[DisableDefaultCtor]
[Internal]
interface SentryClient
{
    // @property (nonatomic, strong) SentryOptions * _Nonnull options;
    [Export ("options", ArgumentSemantic.Strong)]
    SentryOptions Options { get; set; }

    // -(instancetype _Nullable)initWithOptions:(SentryOptions * _Nonnull)options;
    [Export ("initWithOptions:")]
    NativeHandle Constructor (SentryOptions options);

    // -(SentryId * _Nonnull)captureEvent:(SentryEvent * _Nonnull)event __attribute__((swift_name("capture(event:)")));
    [Export ("captureEvent:")]
    SentryId CaptureEvent (SentryEvent @event);

    // -(SentryId * _Nonnull)captureEvent:(SentryEvent * _Nonnull)event withScope:(SentryScope * _Nonnull)scope __attribute__((swift_name("capture(event:scope:)")));
    [Export ("captureEvent:withScope:")]
    SentryId CaptureEvent (SentryEvent @event, SentryScope scope);

    // -(SentryId * _Nonnull)captureError:(NSError * _Nonnull)error __attribute__((swift_name("capture(error:)")));
    [Export ("captureError:")]
    SentryId CaptureError (NSError error);

    // -(SentryId * _Nonnull)captureError:(NSError * _Nonnull)error withScope:(SentryScope * _Nonnull)scope __attribute__((swift_name("capture(error:scope:)")));
    [Export ("captureError:withScope:")]
    SentryId CaptureError (NSError error, SentryScope scope);

    // -(SentryId * _Nonnull)captureException:(NSException * _Nonnull)exception __attribute__((swift_name("capture(exception:)")));
    [Export ("captureException:")]
    SentryId CaptureException (NSException exception);

    // -(SentryId * _Nonnull)captureException:(NSException * _Nonnull)exception withScope:(SentryScope * _Nonnull)scope __attribute__((swift_name("capture(exception:scope:)")));
    [Export ("captureException:withScope:")]
    SentryId CaptureException (NSException exception, SentryScope scope);

    // -(SentryId * _Nonnull)captureMessage:(NSString * _Nonnull)message __attribute__((swift_name("capture(message:)")));
    [Export ("captureMessage:")]
    SentryId CaptureMessage (string message);

    // -(SentryId * _Nonnull)captureMessage:(NSString * _Nonnull)message withScope:(SentryScope * _Nonnull)scope __attribute__((swift_name("capture(message:scope:)")));
    [Export ("captureMessage:withScope:")]
    SentryId CaptureMessage (string message, SentryScope scope);

    // -(void)captureUserFeedback:(SentryUserFeedback * _Nonnull)userFeedback __attribute__((swift_name("capture(userFeedback:)")));
    [Export ("captureUserFeedback:")]
    void CaptureUserFeedback (SentryUserFeedback userFeedback);

    // -(void)captureSession:(SentrySession * _Nonnull)session __attribute__((swift_name("capture(session:)")));
    [Export ("captureSession:")]
    void CaptureSession (SentrySession session);

    // -(void)captureEnvelope:(SentryEnvelope * _Nonnull)envelope __attribute__((swift_name("capture(envelope:)")));
    [Export ("captureEnvelope:")]
    void CaptureEnvelope (SentryEnvelope envelope);
}

// @interface SentryCrashExceptionApplication : NSObject
[BaseType (typeof(NSObject))]
[Internal]
interface SentryCrashExceptionApplication
{
}

// @interface SentryDebugImageProvider : NSObject
[BaseType (typeof(NSObject))]
[Internal]
interface SentryDebugImageProvider
{
    // -(NSArray<SentryDebugMeta *> * _Nonnull)getDebugImagesForThreads:(NSArray<SentryThread *> * _Nonnull)threads;
    [Export ("getDebugImagesForThreads:")]
    SentryDebugMeta[] GetDebugImagesForThreads (SentryThread[] threads);

    // -(NSArray<SentryDebugMeta *> * _Nonnull)getDebugImages;
    [Export ("getDebugImages")]
    SentryDebugMeta[] DebugImages { get; }
}

// @interface SentryDebugMeta : NSObject <SentrySerializable>
[BaseType (typeof(NSObject))]
[Internal]
interface SentryDebugMeta : SentrySerializable
{
    // @property (copy, nonatomic) NSString * _Nullable uuid;
    [NullAllowed, Export ("uuid")]
    string Uuid { get; set; }

    // @property (copy, nonatomic) NSString * _Nullable type;
    [NullAllowed, Export ("type")]
    string Type { get; set; }

    // @property (copy, nonatomic) NSString * _Nullable name;
    [NullAllowed, Export ("name")]
    string Name { get; set; }

    // @property (copy, nonatomic) NSNumber * _Nullable imageSize;
    [NullAllowed, Export ("imageSize", ArgumentSemantic.Copy)]
    NSNumber ImageSize { get; set; }

    // @property (copy, nonatomic) NSString * _Nullable imageAddress;
    [NullAllowed, Export ("imageAddress")]
    string ImageAddress { get; set; }

    // @property (copy, nonatomic) NSString * _Nullable imageVmAddress;
    [NullAllowed, Export ("imageVmAddress")]
    string ImageVmAddress { get; set; }
}

// @interface SentryDsn : NSObject
[BaseType (typeof(NSObject))]
[Internal]
interface SentryDsn
{
    // @property (readonly, nonatomic, strong) NSURL * _Nonnull url;
    [Export ("url", ArgumentSemantic.Strong)]
    NSUrl Url { get; }

    // -(instancetype _Nullable)initWithString:(NSString * _Nonnull)dsnString didFailWithError:(NSError * _Nullable * _Nullable)error;
    [Export ("initWithString:didFailWithError:")]
    NativeHandle Constructor (string dsnString, [NullAllowed] out NSError error);

    // -(NSString * _Nonnull)getHash;
    [Export ("getHash")]
    string Hash { get; }

    // -(NSURL * _Nonnull)getStoreEndpoint;
    [Export ("getStoreEndpoint")]
    NSUrl StoreEndpoint { get; }

    // -(NSURL * _Nonnull)getEnvelopeEndpoint;
    [Export ("getEnvelopeEndpoint")]
    NSUrl EnvelopeEndpoint { get; }
}

// @interface SentryEnvelopeHeader : NSObject
[BaseType (typeof(NSObject))]
[DisableDefaultCtor]
[Internal]
interface SentryEnvelopeHeader
{
    // -(instancetype _Nonnull)initWithId:(SentryId * _Nullable)eventId;
    [Export ("initWithId:")]
    NativeHandle Constructor ([NullAllowed] SentryId eventId);

    // // -(instancetype _Nonnull)initWithId:(SentryId * _Nullable)eventId traceContext:(SentryTraceContext * _Nullable)traceContext;
    // [Export ("initWithId:traceContext:")]
    // NativeHandle Constructor ([NullAllowed] SentryId eventId, [NullAllowed] SentryTraceContext traceContext);

    // // -(instancetype _Nonnull)initWithId:(SentryId * _Nullable)eventId sdkInfo:(SentrySdkInfo * _Nullable)sdkInfo traceContext:(SentryTraceContext * _Nullable)traceContext __attribute__((objc_designated_initializer));
    // [Export ("initWithId:sdkInfo:traceContext:")]
    // [DesignatedInitializer]
    // NativeHandle Constructor ([NullAllowed] SentryId eventId, [NullAllowed] SentrySdkInfo sdkInfo, [NullAllowed] SentryTraceContext traceContext);

    // @property (readonly, copy, nonatomic) SentryId * _Nullable eventId;
    [NullAllowed, Export ("eventId", ArgumentSemantic.Copy)]
    SentryId EventId { get; }

    // @property (readonly, copy, nonatomic) SentrySdkInfo * _Nullable sdkInfo;
    [NullAllowed, Export ("sdkInfo", ArgumentSemantic.Copy)]
    SentrySdkInfo SdkInfo { get; }

    // // @property (readonly, copy, nonatomic) SentryTraceContext * _Nullable traceContext;
    // [NullAllowed, Export ("traceContext", ArgumentSemantic.Copy)]
    // SentryTraceContext TraceContext { get; }
}

// @interface SentryEnvelopeItemHeader : NSObject
[BaseType (typeof(NSObject))]
[DisableDefaultCtor]
[Internal]
interface SentryEnvelopeItemHeader
{
    // -(instancetype _Nonnull)initWithType:(NSString * _Nonnull)type length:(NSUInteger)length __attribute__((objc_designated_initializer));
    [Export ("initWithType:length:")]
    [DesignatedInitializer]
    NativeHandle Constructor (string type, nuint length);

    // -(instancetype _Nonnull)initWithType:(NSString * _Nonnull)type length:(NSUInteger)length filenname:(NSString * _Nonnull)filename contentType:(NSString * _Nonnull)contentType;
    [Export ("initWithType:length:filenname:contentType:")]
    NativeHandle Constructor (string type, nuint length, string filename, string contentType);

    // @property (readonly, copy, nonatomic) NSString * _Nonnull type;
    [Export ("type")]
    string Type { get; }

    // @property (readonly, nonatomic) NSUInteger length;
    [Export ("length")]
    nuint Length { get; }

    // @property (readonly, copy, nonatomic) NSString * _Nullable filename;
    [NullAllowed, Export ("filename")]
    string Filename { get; }

    // @property (readonly, copy, nonatomic) NSString * _Nullable contentType;
    [NullAllowed, Export ("contentType")]
    string ContentType { get; }
}

// @interface SentryEnvelopeItem : NSObject
[BaseType (typeof(NSObject))]
[DisableDefaultCtor]
[Internal]
interface SentryEnvelopeItem
{
    // -(instancetype _Nonnull)initWithEvent:(SentryEvent * _Nonnull)event;
    [Export ("initWithEvent:")]
    NativeHandle Constructor (SentryEvent @event);

    // -(instancetype _Nonnull)initWithSession:(SentrySession * _Nonnull)session;
    [Export ("initWithSession:")]
    NativeHandle Constructor (SentrySession session);

    // -(instancetype _Nonnull)initWithUserFeedback:(SentryUserFeedback * _Nonnull)userFeedback;
    [Export ("initWithUserFeedback:")]
    NativeHandle Constructor (SentryUserFeedback userFeedback);

    // -(instancetype _Nullable)initWithAttachment:(SentryAttachment * _Nonnull)attachment maxAttachmentSize:(NSUInteger)maxAttachmentSize;
    [Export ("initWithAttachment:maxAttachmentSize:")]
    NativeHandle Constructor (SentryAttachment attachment, nuint maxAttachmentSize);

    // -(instancetype _Nonnull)initWithHeader:(SentryEnvelopeItemHeader * _Nonnull)header data:(NSData * _Nonnull)data __attribute__((objc_designated_initializer));
    [Export ("initWithHeader:data:")]
    [DesignatedInitializer]
    NativeHandle Constructor (SentryEnvelopeItemHeader header, NSData data);

    // @property (readonly, nonatomic, strong) SentryEnvelopeItemHeader * _Nonnull header;
    [Export ("header", ArgumentSemantic.Strong)]
    SentryEnvelopeItemHeader Header { get; }

    // @property (readonly, nonatomic, strong) NSData * _Nonnull data;
    [Export ("data", ArgumentSemantic.Strong)]
    NSData Data { get; }
}

// @interface SentryEnvelope : NSObject
[BaseType (typeof(NSObject))]
[DisableDefaultCtor]
[Internal]
interface SentryEnvelope
{
    // -(instancetype _Nonnull)initWithId:(SentryId * _Nullable)id singleItem:(SentryEnvelopeItem * _Nonnull)item;
    [Export ("initWithId:singleItem:")]
    NativeHandle Constructor ([NullAllowed] SentryId id, SentryEnvelopeItem item);

    // -(instancetype _Nonnull)initWithHeader:(SentryEnvelopeHeader * _Nonnull)header singleItem:(SentryEnvelopeItem * _Nonnull)item;
    [Export ("initWithHeader:singleItem:")]
    NativeHandle Constructor (SentryEnvelopeHeader header, SentryEnvelopeItem item);

    // -(instancetype _Nonnull)initWithId:(SentryId * _Nullable)id items:(NSArray<SentryEnvelopeItem *> * _Nonnull)items;
    [Export ("initWithId:items:")]
    NativeHandle Constructor ([NullAllowed] SentryId id, SentryEnvelopeItem[] items);

    // -(instancetype _Nonnull)initWithSession:(SentrySession * _Nonnull)session;
    [Export ("initWithSession:")]
    NativeHandle Constructor (SentrySession session);

    // -(instancetype _Nonnull)initWithSessions:(NSArray<SentrySession *> * _Nonnull)sessions;
    [Export ("initWithSessions:")]
    NativeHandle Constructor (SentrySession[] sessions);

    // -(instancetype _Nonnull)initWithHeader:(SentryEnvelopeHeader * _Nonnull)header items:(NSArray<SentryEnvelopeItem *> * _Nonnull)items __attribute__((objc_designated_initializer));
    [Export ("initWithHeader:items:")]
    [DesignatedInitializer]
    NativeHandle Constructor (SentryEnvelopeHeader header, SentryEnvelopeItem[] items);

    // -(instancetype _Nonnull)initWithEvent:(SentryEvent * _Nonnull)event;
    [Export ("initWithEvent:")]
    NativeHandle Constructor (SentryEvent @event);

    // -(instancetype _Nonnull)initWithUserFeedback:(SentryUserFeedback * _Nonnull)userFeedback;
    [Export ("initWithUserFeedback:")]
    NativeHandle Constructor (SentryUserFeedback userFeedback);

    // @property (readonly, nonatomic, strong) SentryEnvelopeHeader * _Nonnull header;
    [Export ("header", ArgumentSemantic.Strong)]
    SentryEnvelopeHeader Header { get; }

    // @property (readonly, nonatomic, strong) NSArray<SentryEnvelopeItem *> * _Nonnull items;
    [Export ("items", ArgumentSemantic.Strong)]
    SentryEnvelopeItem[] Items { get; }
}

// @interface SentryEvent : NSObject <SentrySerializable>
[BaseType (typeof(NSObject))]
[Internal]
interface SentryEvent : SentrySerializable
{
    // @property (nonatomic, strong) SentryId * _Nonnull eventId;
    [Export ("eventId", ArgumentSemantic.Strong)]
    SentryId EventId { get; set; }

    // @property (nonatomic, strong) SentryMessage * _Nullable message;
    [NullAllowed, Export ("message", ArgumentSemantic.Strong)]
    SentryMessage Message { get; set; }

    // @property (copy, nonatomic) NSError * _Nullable error;
    [NullAllowed, Export ("error", ArgumentSemantic.Copy)]
    NSError Error { get; set; }

    // @property (nonatomic, strong) NSDate * _Nullable timestamp;
    [NullAllowed, Export ("timestamp", ArgumentSemantic.Strong)]
    NSDate Timestamp { get; set; }

    // @property (nonatomic, strong) NSDate * _Nullable startTimestamp;
    [NullAllowed, Export ("startTimestamp", ArgumentSemantic.Strong)]
    NSDate StartTimestamp { get; set; }

    // @property (nonatomic) enum SentryLevel level;
    [Export ("level", ArgumentSemantic.Assign)]
    SentryLevel Level { get; set; }

    // @property (copy, nonatomic) NSString * _Nonnull platform;
    [Export ("platform")]
    string Platform { get; set; }

    // @property (copy, nonatomic) NSString * _Nullable logger;
    [NullAllowed, Export ("logger")]
    string Logger { get; set; }

    // @property (copy, nonatomic) NSString * _Nullable serverName;
    [NullAllowed, Export ("serverName")]
    string ServerName { get; set; }

    // @property (copy, nonatomic) NSString * _Nullable releaseName;
    [NullAllowed, Export ("releaseName")]
    string ReleaseName { get; set; }

    // @property (copy, nonatomic) NSString * _Nullable dist;
    [NullAllowed, Export ("dist")]
    string Dist { get; set; }

    // @property (copy, nonatomic) NSString * _Nullable environment;
    [NullAllowed, Export ("environment")]
    string Environment { get; set; }

    // @property (copy, nonatomic) NSString * _Nullable transaction;
    [NullAllowed, Export ("transaction")]
    string Transaction { get; set; }

    // @property (copy, nonatomic) NSString * _Nullable type;
    [NullAllowed, Export ("type")]
    string Type { get; set; }

    // @property (nonatomic, strong) NSDictionary<NSString *,NSString *> * _Nullable tags;
    [NullAllowed, Export ("tags", ArgumentSemantic.Strong)]
    NSDictionary<NSString, NSString> Tags { get; set; }

    // @property (nonatomic, strong) NSDictionary<NSString *,id> * _Nullable extra;
    [NullAllowed, Export ("extra", ArgumentSemantic.Strong)]
    NSDictionary<NSString, NSObject> Extra { get; set; }

    // @property (nonatomic, strong) NSDictionary<NSString *,id> * _Nullable sdk;
    [NullAllowed, Export ("sdk", ArgumentSemantic.Strong)]
    NSDictionary<NSString, NSObject> Sdk { get; set; }

    // @property (nonatomic, strong) NSDictionary<NSString *,NSString *> * _Nullable modules;
    [NullAllowed, Export ("modules", ArgumentSemantic.Strong)]
    NSDictionary<NSString, NSString> Modules { get; set; }

    // @property (nonatomic, strong) NSArray<NSString *> * _Nullable fingerprint;
    [NullAllowed, Export ("fingerprint", ArgumentSemantic.Strong)]
    string[] Fingerprint { get; set; }

    // @property (nonatomic, strong) SentryUser * _Nullable user;
    [NullAllowed, Export ("user", ArgumentSemantic.Strong)]
    SentryUser User { get; set; }

    // @property (nonatomic, strong) NSDictionary<NSString *,NSDictionary<NSString *,id> *> * _Nullable context;
    [NullAllowed, Export ("context", ArgumentSemantic.Strong)]
    NSDictionary<NSString, NSDictionary<NSString, NSObject>> Context { get; set; }

    // @property (nonatomic, strong) NSArray<SentryThread *> * _Nullable threads;
    [NullAllowed, Export ("threads", ArgumentSemantic.Strong)]
    SentryThread[] Threads { get; set; }

    // @property (nonatomic, strong) NSArray<SentryException *> * _Nullable exceptions;
    [NullAllowed, Export ("exceptions", ArgumentSemantic.Strong)]
    SentryException[] Exceptions { get; set; }

    // @property (nonatomic, strong) SentryStacktrace * _Nullable stacktrace;
    [NullAllowed, Export ("stacktrace", ArgumentSemantic.Strong)]
    SentryStacktrace Stacktrace { get; set; }

    // @property (nonatomic, strong) NSArray<SentryDebugMeta *> * _Nullable debugMeta;
    [NullAllowed, Export ("debugMeta", ArgumentSemantic.Strong)]
    SentryDebugMeta[] DebugMeta { get; set; }

    // @property (nonatomic, strong) NSArray<SentryBreadcrumb *> * _Nullable breadcrumbs;
    [NullAllowed, Export ("breadcrumbs", ArgumentSemantic.Strong)]
    SentryBreadcrumb[] Breadcrumbs { get; set; }

    // -(instancetype _Nonnull)initWithLevel:(enum SentryLevel)level __attribute__((objc_designated_initializer));
    [Export ("initWithLevel:")]
    [DesignatedInitializer]
    NativeHandle Constructor (SentryLevel level);

    // -(instancetype _Nonnull)initWithError:(NSError * _Nonnull)error;
    [Export ("initWithError:")]
    NativeHandle Constructor (NSError error);
}

// @interface SentryException : NSObject <SentrySerializable>
[BaseType (typeof(NSObject))]
[DisableDefaultCtor]
[Internal]
interface SentryException : SentrySerializable
{
    // @property (copy, nonatomic) NSString * _Nonnull value;
    [Export ("value")]
    string Value { get; set; }

    // @property (copy, nonatomic) NSString * _Nonnull type;
    [Export ("type")]
    string Type { get; set; }

    // @property (nonatomic, strong) SentryMechanism * _Nullable mechanism;
    [NullAllowed, Export ("mechanism", ArgumentSemantic.Strong)]
    SentryMechanism Mechanism { get; set; }

    // @property (copy, nonatomic) NSString * _Nullable module;
    [NullAllowed, Export ("module")]
    string Module { get; set; }

    // @property (copy, nonatomic) NSNumber * _Nullable threadId;
    [NullAllowed, Export ("threadId", ArgumentSemantic.Copy)]
    NSNumber ThreadId { get; set; }

    // @property (nonatomic, strong) SentryStacktrace * _Nullable stacktrace;
    [NullAllowed, Export ("stacktrace", ArgumentSemantic.Strong)]
    SentryStacktrace Stacktrace { get; set; }

    // -(instancetype _Nonnull)initWithValue:(NSString * _Nonnull)value type:(NSString * _Nonnull)type;
    [Export ("initWithValue:type:")]
    NativeHandle Constructor (string value, string type);
}

// @interface SentryFrame : NSObject <SentrySerializable>
[BaseType (typeof(NSObject))]
[Internal]
interface SentryFrame : SentrySerializable
{
    // @property (copy, nonatomic) NSString * _Nullable symbolAddress;
    [NullAllowed, Export ("symbolAddress")]
    string SymbolAddress { get; set; }

    // @property (copy, nonatomic) NSString * _Nullable fileName;
    [NullAllowed, Export ("fileName")]
    string FileName { get; set; }

    // @property (copy, nonatomic) NSString * _Nullable function;
    [NullAllowed, Export ("function")]
    string Function { get; set; }

    // @property (copy, nonatomic) NSString * _Nullable module;
    [NullAllowed, Export ("module")]
    string Module { get; set; }

    // @property (copy, nonatomic) NSString * _Nullable package;
    [NullAllowed, Export ("package")]
    string Package { get; set; }

    // @property (copy, nonatomic) NSString * _Nullable imageAddress;
    [NullAllowed, Export ("imageAddress")]
    string ImageAddress { get; set; }

    // @property (copy, nonatomic) NSString * _Nullable platform;
    [NullAllowed, Export ("platform")]
    string Platform { get; set; }

    // @property (copy, nonatomic) NSString * _Nullable instructionAddress;
    [NullAllowed, Export ("instructionAddress")]
    string InstructionAddress { get; set; }

    // @property (copy, nonatomic) NSNumber * _Nullable lineNumber;
    [NullAllowed, Export ("lineNumber", ArgumentSemantic.Copy)]
    NSNumber LineNumber { get; set; }

    // @property (copy, nonatomic) NSNumber * _Nullable columnNumber;
    [NullAllowed, Export ("columnNumber", ArgumentSemantic.Copy)]
    NSNumber ColumnNumber { get; set; }

    // @property (copy, nonatomic) NSNumber * _Nullable inApp;
    [NullAllowed, Export ("inApp", ArgumentSemantic.Copy)]
    NSNumber InApp { get; set; }

    // @property (copy, nonatomic) NSNumber * _Nullable stackStart;
    [NullAllowed, Export ("stackStart", ArgumentSemantic.Copy)]
    NSNumber StackStart { get; set; }
}

// @interface SentryOptions : NSObject
[BaseType (typeof(NSObject))]
[Internal]
interface SentryOptions
{
    // -(instancetype _Nullable)initWithDict:(NSDictionary<NSString *,id> * _Nonnull)options didFailWithError:(NSError * _Nullable * _Nullable)error;
    [Export ("initWithDict:didFailWithError:")]
    NativeHandle Constructor (NSDictionary<NSString, NSObject> options, [NullAllowed] out NSError error);

    // @property (nonatomic, strong) NSString * _Nullable dsn;
    [NullAllowed, Export ("dsn", ArgumentSemantic.Strong)]
    string Dsn { get; set; }

    // @property (nonatomic, strong) SentryDsn * _Nullable parsedDsn;
    [NullAllowed, Export ("parsedDsn", ArgumentSemantic.Strong)]
    SentryDsn ParsedDsn { get; set; }

    // @property (assign, nonatomic) BOOL debug;
    [Export ("debug")]
    bool Debug { get; set; }

    // @property (assign, nonatomic) SentryLevel diagnosticLevel;
    [Export ("diagnosticLevel", ArgumentSemantic.Assign)]
    SentryLevel DiagnosticLevel { get; set; }

    // @property (copy, nonatomic) NSString * _Nullable releaseName;
    [NullAllowed, Export ("releaseName")]
    string ReleaseName { get; set; }

    // @property (copy, nonatomic) NSString * _Nullable dist;
    [NullAllowed, Export ("dist")]
    string Dist { get; set; }

    // @property (copy, nonatomic) NSString * _Nullable environment;
    [NullAllowed, Export ("environment")]
    string Environment { get; set; }

    // @property (assign, nonatomic) BOOL enabled;
    [Export ("enabled")]
    bool Enabled { get; set; }

	// @property (assign, nonatomic) BOOL enableCrashHandler;
	[Export ("enableCrashHandler")]
	bool EnableCrashHandler { get; set; }

    // @property (assign, nonatomic) NSUInteger maxBreadcrumbs;
    [Export ("maxBreadcrumbs")]
    nuint MaxBreadcrumbs { get; set; }

    // @property (assign, nonatomic) BOOL enableNetworkBreadcrumbs;
    [Export ("enableNetworkBreadcrumbs")]
    bool EnableNetworkBreadcrumbs { get; set; }

    // @property (assign, nonatomic) NSUInteger maxCacheItems;
    [Export ("maxCacheItems")]
    nuint MaxCacheItems { get; set; }

    // @property (copy, nonatomic) SentryBeforeSendEventCallback _Nullable beforeSend;
    [NullAllowed, Export ("beforeSend", ArgumentSemantic.Copy)]
    Func<SentryEvent?, SentryEvent> BeforeSend { get; set; }

    // @property (copy, nonatomic) SentryBeforeBreadcrumbCallback _Nullable beforeBreadcrumb;
    [NullAllowed, Export ("beforeBreadcrumb", ArgumentSemantic.Copy)]
    Func<SentryBreadcrumb, SentryBreadcrumb?> BeforeBreadcrumb { get; set; }

    // @property (copy, nonatomic) SentryOnCrashedLastRunCallback _Nullable onCrashedLastRun;
    [NullAllowed, Export ("onCrashedLastRun", ArgumentSemantic.Copy)]
    Action<SentryEvent> OnCrashedLastRun { get; set; }

    // @property (copy, nonatomic) NSArray<NSString *> * _Nullable integrations;
    [NullAllowed, Export ("integrations", ArgumentSemantic.Copy)]
    string[] Integrations { get; set; }

    // +(NSArray<NSString *> * _Nonnull)defaultIntegrations;
    [Static]
    [Export ("defaultIntegrations")]
    string[] DefaultIntegrations { get; }

    // @property (copy, nonatomic) NSNumber * _Nullable sampleRate;
    [NullAllowed, Export ("sampleRate", ArgumentSemantic.Copy)]
    NSNumber SampleRate { get; set; }

    // @property (assign, nonatomic) BOOL enableAutoSessionTracking;
    [Export ("enableAutoSessionTracking")]
    bool EnableAutoSessionTracking { get; set; }

    // @property (assign, nonatomic) BOOL enableOutOfMemoryTracking;
    [Export ("enableOutOfMemoryTracking")]
    bool EnableOutOfMemoryTracking { get; set; }

    // @property (assign, nonatomic) NSUInteger sessionTrackingIntervalMillis;
    [Export ("sessionTrackingIntervalMillis")]
    nuint SessionTrackingIntervalMillis { get; set; }

    // @property (assign, nonatomic) BOOL attachStacktrace;
    [Export ("attachStacktrace")]
    bool AttachStacktrace { get; set; }

    // @property (assign, nonatomic) BOOL stitchAsyncCode;
    [Export ("stitchAsyncCode")]
    bool StitchAsyncCode { get; set; }

    // @property (readonly, nonatomic, strong) DEPRECATED_MSG_ATTRIBUTE("This property will be removed in a future version of the SDK") SentrySdkInfo * sdkInfo __attribute__((deprecated("This property will be removed in a future version of the SDK")));
    [Export ("sdkInfo", ArgumentSemantic.Strong)]
    SentrySdkInfo SdkInfo { get; }

    // @property (assign, nonatomic) NSUInteger maxAttachmentSize;
    [Export ("maxAttachmentSize")]
    nuint MaxAttachmentSize { get; set; }

    // @property (assign, nonatomic) BOOL sendDefaultPii;
    [Export ("sendDefaultPii")]
    bool SendDefaultPii { get; set; }

    // @property (assign, nonatomic) BOOL enableAutoPerformanceTracking;
    [Export ("enableAutoPerformanceTracking")]
    bool EnableAutoPerformanceTracking { get; set; }

    // @property (assign, nonatomic) BOOL enableUIViewControllerTracking;
    [Export ("enableUIViewControllerTracking")]
    bool EnableUIViewControllerTracking { get; set; }

    // @property (assign, nonatomic) BOOL attachScreenshot;
    [Export ("attachScreenshot")]
    bool AttachScreenshot { get; set; }

	// @property (assign, nonatomic) BOOL attachViewHierarchy;
	[Export ("attachViewHierarchy")]
	bool AttachViewHierarchy { get; set; }

    // @property (assign, nonatomic) BOOL enableUserInteractionTracing;
    [Export ("enableUserInteractionTracing")]
    bool EnableUserInteractionTracing { get; set; }

    // @property (assign, nonatomic) NSTimeInterval idleTimeout;
    [Export ("idleTimeout")]
    double IdleTimeout { get; set; }

    // @property (assign, nonatomic) BOOL enableNetworkTracking;
    [Export ("enableNetworkTracking")]
    bool EnableNetworkTracking { get; set; }

    // @property (assign, nonatomic) BOOL enableFileIOTracking;
    [Export ("enableFileIOTracking")]
    bool EnableFileIOTracking { get; set; }

    // @property (nonatomic, strong) NSNumber * _Nullable tracesSampleRate;
    [NullAllowed, Export ("tracesSampleRate", ArgumentSemantic.Strong)]
    NSNumber TracesSampleRate { get; set; }

    // @property (nonatomic) SentryTracesSamplerCallback _Nullable tracesSampler;
    [NullAllowed, Export ("tracesSampler", ArgumentSemantic.Assign)]
    Func<SentrySamplingContext, NSNumber?> TracesSampler { get; set; }

    // @property (readonly, assign, nonatomic) BOOL isTracingEnabled;
    [Export ("isTracingEnabled")]
    bool IsTracingEnabled { get; }

    // @property (readonly, copy, nonatomic) NSArray<NSString *> * _Nonnull inAppIncludes;
    [Export ("inAppIncludes", ArgumentSemantic.Copy)]
    string[] InAppIncludes { get; }

    // -(void)addInAppInclude:(NSString * _Nonnull)inAppInclude;
    [Export ("addInAppInclude:")]
    void AddInAppInclude (string inAppInclude);

    // @property (readonly, copy, nonatomic) NSArray<NSString *> * _Nonnull inAppExcludes;
    [Export ("inAppExcludes", ArgumentSemantic.Copy)]
    string[] InAppExcludes { get; }

    // -(void)addInAppExclude:(NSString * _Nonnull)inAppExclude;
    [Export ("addInAppExclude:")]
    void AddInAppExclude (string inAppExclude);

    [Wrap ("WeakUrlSessionDelegate")]
    [NullAllowed]
    NSUrlSessionDelegate UrlSessionDelegate { get; set; }

    // @property (nonatomic, weak) id<NSURLSessionDelegate> _Nullable urlSessionDelegate;
    [NullAllowed, Export ("urlSessionDelegate", ArgumentSemantic.Weak)]
    NSObject WeakUrlSessionDelegate { get; set; }

    // @property (assign, nonatomic) BOOL enableSwizzling;
    [Export ("enableSwizzling")]
    bool EnableSwizzling { get; set; }

    // @property (assign, nonatomic) BOOL enableCoreDataTracking;
    [Export ("enableCoreDataTracking")]
    bool EnableCoreDataTracking { get; set; }

	// @property (nonatomic, strong) NSNumber * _Nullable profilesSampleRate;
	[NullAllowed, Export ("profilesSampleRate", ArgumentSemantic.Strong)]
	NSNumber ProfilesSampleRate { get; set; }

	// @property (nonatomic) SentryTracesSamplerCallback _Nullable profilesSampler;
	[NullAllowed, Export ("profilesSampler", ArgumentSemantic.Assign)]
	Func<SentrySamplingContext, NSNumber?> ProfilesSampler { get; set; }

	// @property (readonly, assign, nonatomic) BOOL isProfilingEnabled;
	[Export ("isProfilingEnabled")]
	bool IsProfilingEnabled { get; }

	// @property (assign, nonatomic) BOOL enableProfiling __attribute__((deprecated("Use profilesSampleRate or profilesSampler instead. This property will be removed in a future version of the SDK")));
    [Export ("enableProfiling")]
    bool EnableProfiling { get; set; }

    // @property (assign, nonatomic) BOOL sendClientReports;
    [Export ("sendClientReports")]
    bool SendClientReports { get; set; }

    // @property (assign, nonatomic) BOOL enableAppHangTracking;
    [Export ("enableAppHangTracking")]
    bool EnableAppHangTracking { get; set; }

    // @property (assign, nonatomic) NSTimeInterval appHangTimeoutInterval;
    [Export ("appHangTimeoutInterval")]
    double AppHangTimeoutInterval { get; set; }

    // @property (assign, nonatomic) BOOL enableAutoBreadcrumbTracking;
    [Export ("enableAutoBreadcrumbTracking")]
    bool EnableAutoBreadcrumbTracking { get; set; }
}

// @protocol SentryIntegrationProtocol <NSObject>
[Protocol]
[BaseType (typeof(NSObject))]
[Internal]
interface SentryIntegrationProtocol
{
	// @required -(BOOL)installWithOptions:(SentryOptions * _Nonnull)options;
    [Abstract]
    [Export ("installWithOptions:")]
	bool InstallWithOptions (SentryOptions options);

    // @optional -(void)uninstall;
    [Export ("uninstall")]
    void Uninstall ();
}

// @interface SentrySpanContext : NSObject <SentrySerializable>
[BaseType (typeof(NSObject))]
[DisableDefaultCtor]
[Internal]
interface SentrySpanContext : SentrySerializable
{
    // @property (readonly, nonatomic) SentryId * _Nonnull traceId;
    [Export ("traceId")]
    SentryId TraceId { get; }

    // @property (readonly, nonatomic) SentrySpanId * _Nonnull spanId;
    [Export ("spanId")]
    SentrySpanId SpanId { get; }

    // @property (readonly, nonatomic) SentrySpanId * _Nullable parentSpanId;
    [NullAllowed, Export ("parentSpanId")]
    SentrySpanId ParentSpanId { get; }

    // @property (nonatomic) SentrySampleDecision sampled;
    [Export ("sampled", ArgumentSemantic.Assign)]
    SentrySampleDecision Sampled { get; set; }

    // @property (copy, nonatomic) NSString * _Nonnull operation;
    [Export ("operation")]
    string Operation { get; set; }

    // @property (copy, nonatomic) NSString * _Nullable spanDescription;
    [NullAllowed, Export ("spanDescription")]
    string SpanDescription { get; set; }

    // @property (nonatomic) SentrySpanStatus status;
    [Export ("status", ArgumentSemantic.Assign)]
    SentrySpanStatus Status { get; set; }

    // @property (readonly, nonatomic) NSDictionary<NSString *,NSString *> * _Nonnull tags;
    [Export ("tags")]
    NSDictionary<NSString, NSString> Tags { get; }

    // -(instancetype _Nonnull)initWithOperation:(NSString * _Nonnull)operation;
    [Export ("initWithOperation:")]
    NativeHandle Constructor (string operation);

    // -(instancetype _Nonnull)initWithOperation:(NSString * _Nonnull)operation sampled:(SentrySampleDecision)sampled;
    [Export ("initWithOperation:sampled:")]
    NativeHandle Constructor (string operation, SentrySampleDecision sampled);

    // -(instancetype _Nonnull)initWithTraceId:(SentryId * _Nonnull)traceId spanId:(SentrySpanId * _Nonnull)spanId parentId:(SentrySpanId * _Nullable)parentId operation:(NSString * _Nonnull)operation sampled:(SentrySampleDecision)sampled;
    [Export ("initWithTraceId:spanId:parentId:operation:sampled:")]
    NativeHandle Constructor (SentryId traceId, SentrySpanId spanId, [NullAllowed] SentrySpanId parentId, string operation, SentrySampleDecision sampled);

    // -(void)setTagValue:(NSString * _Nonnull)value forKey:(NSString * _Nonnull)key __attribute__((swift_name("setTag(value:key:)")));
    [Export ("setTagValue:forKey:")]
    void SetTagValue (string value, string key);

    // -(void)removeTagForKey:(NSString * _Nonnull)key __attribute__((swift_name("removeTag(key:)")));
    [Export ("removeTagForKey:")]
    void RemoveTagForKey (string key);

    // @property (readonly, copy, nonatomic, class) NSString * _Nonnull type;
    [Static]
    [Export ("type")]
    string Type { get; }
}

// @protocol SentrySpan <SentrySerializable>
[Protocol, Model]
[BaseType (typeof(NSObject))]
[Internal]
interface SentrySpan : SentrySerializable
{
    // @required @property (readonly, nonatomic) SentrySpanContext * _Nonnull context;
    [Abstract]
    [Export ("context")]
    SentrySpanContext Context { get; }

    // @required @property (nonatomic, strong) NSDate * _Nullable timestamp;
    [Abstract]
    [NullAllowed, Export ("timestamp", ArgumentSemantic.Strong)]
    NSDate Timestamp { get; set; }

    // @required @property (nonatomic, strong) NSDate * _Nullable startTimestamp;
    [Abstract]
    [NullAllowed, Export ("startTimestamp", ArgumentSemantic.Strong)]
    NSDate StartTimestamp { get; set; }

    // @required @property (readonly) NSDictionary<NSString *,id> * _Nullable data;
    [Abstract]
    [NullAllowed, Export ("data")]
    NSDictionary<NSString, NSObject> Data { get; }

    // @required @property (readonly) NSDictionary<NSString *,NSString *> * _Nonnull tags;
    [Abstract]
    [Export ("tags")]
    NSDictionary<NSString, NSString> Tags { get; }

    // @required @property (readonly) BOOL isFinished;
    [Abstract]
    [Export ("isFinished")]
    bool IsFinished { get; }

    // @required -(id<SentrySpan> _Nonnull)startChildWithOperation:(NSString * _Nonnull)operation __attribute__((swift_name("startChild(operation:)")));
    [Abstract]
    [Export ("startChildWithOperation:")]
    SentrySpan StartChildWithOperation (string operation);

    // @required -(id<SentrySpan> _Nonnull)startChildWithOperation:(NSString * _Nonnull)operation description:(NSString * _Nullable)description __attribute__((swift_name("startChild(operation:description:)")));
    [Abstract]
    [Export ("startChildWithOperation:description:")]
    SentrySpan StartChildWithOperation (string operation, [NullAllowed] string description);

    // @required -(void)setDataValue:(id _Nullable)value forKey:(NSString * _Nonnull)key __attribute__((swift_name("setData(value:key:)")));
    [Abstract]
    [Export ("setDataValue:forKey:")]
    void SetDataValue ([NullAllowed] NSObject value, string key);

    // @required -(void)setExtraValue:(id _Nullable)value forKey:(NSString * _Nonnull)key __attribute__((swift_name("setExtra(value:key:)")));
    [Abstract]
    [Export ("setExtraValue:forKey:")]
    void SetExtraValue ([NullAllowed] NSObject value, string key);

    // @required -(void)removeDataForKey:(NSString * _Nonnull)key __attribute__((swift_name("removeData(key:)")));
    [Abstract]
    [Export ("removeDataForKey:")]
    void RemoveDataForKey (string key);

    // @required -(void)setTagValue:(NSString * _Nonnull)value forKey:(NSString * _Nonnull)key __attribute__((swift_name("setTag(value:key:)")));
    [Abstract]
    [Export ("setTagValue:forKey:")]
    void SetTagValue (string value, string key);

    // @required -(void)removeTagForKey:(NSString * _Nonnull)key __attribute__((swift_name("removeTag(key:)")));
    [Abstract]
    [Export ("removeTagForKey:")]
    void RemoveTagForKey (string key);

    // @required -(void)finish;
    [Abstract]
    [Export ("finish")]
    void Finish ();

    // @required -(void)finishWithStatus:(SentrySpanStatus)status __attribute__((swift_name("finish(status:)")));
    [Abstract]
    [Export ("finishWithStatus:")]
    void FinishWithStatus (SentrySpanStatus status);

    // @required -(SentryTraceHeader * _Nonnull)toTraceHeader;
    [Abstract]
    [Export ("toTraceHeader")]
    SentryTraceHeader ToTraceHeader();
}

// @interface SentryHub : NSObject
[BaseType (typeof(NSObject))]
[DisableDefaultCtor]
[Internal]
interface SentryHub
{
    // -(instancetype _Nonnull)initWithClient:(SentryClient * _Nullable)client andScope:(SentryScope * _Nullable)scope;
    [Export ("initWithClient:andScope:")]
    NativeHandle Constructor ([NullAllowed] SentryClient client, [NullAllowed] SentryScope scope);

    // @property (readonly, nonatomic, strong) SentrySession * _Nullable session;
    [NullAllowed, Export ("session", ArgumentSemantic.Strong)]
    SentrySession Session { get; }

    // -(void)startSession;
    [Export ("startSession")]
    void StartSession ();

    // -(void)endSession;
    [Export ("endSession")]
    void EndSession ();

    // -(void)endSessionWithTimestamp:(NSDate * _Nonnull)timestamp;
    [Export ("endSessionWithTimestamp:")]
    void EndSessionWithTimestamp (NSDate timestamp);

    // -(SentryId * _Nonnull)captureEvent:(SentryEvent * _Nonnull)event __attribute__((swift_name("capture(event:)")));
    [Export ("captureEvent:")]
    SentryId CaptureEvent (SentryEvent @event);

    // -(SentryId * _Nonnull)captureEvent:(SentryEvent * _Nonnull)event withScope:(SentryScope * _Nonnull)scope __attribute__((swift_name("capture(event:scope:)")));
    [Export ("captureEvent:withScope:")]
    SentryId CaptureEvent (SentryEvent @event, SentryScope scope);

    // -(id<SentrySpan> _Nonnull)startTransactionWithName:(NSString * _Nonnull)name operation:(NSString * _Nonnull)operation __attribute__((swift_name("startTransaction(name:operation:)")));
    [Export ("startTransactionWithName:operation:")]
    SentrySpan StartTransactionWithName (string name, string operation);

    // -(id<SentrySpan> _Nonnull)startTransactionWithName:(NSString * _Nonnull)name operation:(NSString * _Nonnull)operation bindToScope:(BOOL)bindToScope __attribute__((swift_name("startTransaction(name:operation:bindToScope:)")));
    [Export ("startTransactionWithName:operation:bindToScope:")]
    SentrySpan StartTransactionWithName (string name, string operation, bool bindToScope);

    // -(id<SentrySpan> _Nonnull)startTransactionWithContext:(SentryTransactionContext * _Nonnull)transactionContext __attribute__((swift_name("startTransaction(transactionContext:)")));
    [Export ("startTransactionWithContext:")]
    SentrySpan StartTransactionWithContext (SentryTransactionContext transactionContext);

    // -(id<SentrySpan> _Nonnull)startTransactionWithContext:(SentryTransactionContext * _Nonnull)transactionContext bindToScope:(BOOL)bindToScope __attribute__((swift_name("startTransaction(transactionContext:bindToScope:)")));
    [Export ("startTransactionWithContext:bindToScope:")]
    SentrySpan StartTransactionWithContext (SentryTransactionContext transactionContext, bool bindToScope);

    // -(id<SentrySpan> _Nonnull)startTransactionWithContext:(SentryTransactionContext * _Nonnull)transactionContext bindToScope:(BOOL)bindToScope customSamplingContext:(NSDictionary<NSString *,id> * _Nonnull)customSamplingContext __attribute__((swift_name("startTransaction(transactionContext:bindToScope:customSamplingContext:)")));
    [Export ("startTransactionWithContext:bindToScope:customSamplingContext:")]
    SentrySpan StartTransactionWithContext (SentryTransactionContext transactionContext, bool bindToScope, NSDictionary<NSString, NSObject> customSamplingContext);

    // -(id<SentrySpan> _Nonnull)startTransactionWithContext:(SentryTransactionContext * _Nonnull)transactionContext customSamplingContext:(NSDictionary<NSString *,id> * _Nonnull)customSamplingContext __attribute__((swift_name("startTransaction(transactionContext:customSamplingContext:)")));
    [Export ("startTransactionWithContext:customSamplingContext:")]
    SentrySpan StartTransactionWithContext (SentryTransactionContext transactionContext, NSDictionary<NSString, NSObject> customSamplingContext);

    // -(SentryId * _Nonnull)captureError:(NSError * _Nonnull)error __attribute__((swift_name("capture(error:)")));
    [Export ("captureError:")]
    SentryId CaptureError (NSError error);

    // -(SentryId * _Nonnull)captureError:(NSError * _Nonnull)error withScope:(SentryScope * _Nonnull)scope __attribute__((swift_name("capture(error:scope:)")));
    [Export ("captureError:withScope:")]
    SentryId CaptureError (NSError error, SentryScope scope);

    // -(SentryId * _Nonnull)captureException:(NSException * _Nonnull)exception __attribute__((swift_name("capture(exception:)")));
    [Export ("captureException:")]
    SentryId CaptureException (NSException exception);

    // -(SentryId * _Nonnull)captureException:(NSException * _Nonnull)exception withScope:(SentryScope * _Nonnull)scope __attribute__((swift_name("capture(exception:scope:)")));
    [Export ("captureException:withScope:")]
    SentryId CaptureException (NSException exception, SentryScope scope);

    // -(SentryId * _Nonnull)captureMessage:(NSString * _Nonnull)message __attribute__((swift_name("capture(message:)")));
    [Export ("captureMessage:")]
    SentryId CaptureMessage (string message);

    // -(SentryId * _Nonnull)captureMessage:(NSString * _Nonnull)message withScope:(SentryScope * _Nonnull)scope __attribute__((swift_name("capture(message:scope:)")));
    [Export ("captureMessage:withScope:")]
    SentryId CaptureMessage (string message, SentryScope scope);

    // -(void)captureUserFeedback:(SentryUserFeedback * _Nonnull)userFeedback __attribute__((swift_name("capture(userFeedback:)")));
    [Export ("captureUserFeedback:")]
    void CaptureUserFeedback (SentryUserFeedback userFeedback);

    // -(void)configureScope:(void (^ _Nonnull)(SentryScope * _Nonnull))callback;
    [Export ("configureScope:")]
    void ConfigureScope (Action<SentryScope> callback);

    // -(void)addBreadcrumb:(SentryBreadcrumb * _Nonnull)crumb;
    [Export ("addBreadcrumb:")]
    void AddBreadcrumb (SentryBreadcrumb crumb);

    // -(SentryClient * _Nullable)getClient;
    [NullAllowed, Export ("getClient")]
    SentryClient Client { get; }

    // @property (readonly, nonatomic, strong) SentryScope * _Nonnull scope;
    [Export ("scope", ArgumentSemantic.Strong)]
    SentryScope Scope { get; }

    // -(void)bindClient:(SentryClient * _Nullable)client;
    [Export ("bindClient:")]
    void BindClient ([NullAllowed] SentryClient client);

	// -(BOOL)hasIntegration:(NSString * _Nonnull)integrationName;
	[Export ("hasIntegration:")]
	bool HasIntegration (string integrationName);

    // -(BOOL)isIntegrationInstalled:(Class _Nonnull)integrationClass;
    [Export ("isIntegrationInstalled:")]
    bool IsIntegrationInstalled (Class integrationClass);

    // -(void)setUser:(SentryUser * _Nullable)user;
    [Export ("setUser:")]
    void SetUser ([NullAllowed] SentryUser user);

    // -(void)captureEnvelope:(SentryEnvelope * _Nonnull)envelope __attribute__((swift_name("capture(envelope:)")));
    [Export ("captureEnvelope:")]
    void CaptureEnvelope (SentryEnvelope envelope);
}

// @interface SentryId : NSObject
[BaseType (typeof(NSObject))]
[Internal]
interface SentryId
{
    // -(instancetype _Nonnull)initWithUUID:(NSUUID * _Nonnull)uuid;
    [Export ("initWithUUID:")]
    NativeHandle Constructor (NSUuid uuid);

    // -(instancetype _Nonnull)initWithUUIDString:(NSString * _Nonnull)string;
    [Export ("initWithUUIDString:")]
    NativeHandle Constructor (string @string);

    // @property (readonly, copy) NSString * _Nonnull sentryIdString;
    [Export ("sentryIdString")]
    string SentryIdString { get; }

    // @property (readonly, nonatomic, strong, class) SentryId * _Nonnull empty;
    [Static]
    [Export ("empty", ArgumentSemantic.Strong)]
    SentryId Empty { get; }
}

// @interface SentryMechanism : NSObject <SentrySerializable>
[BaseType (typeof(NSObject))]
[DisableDefaultCtor]
[Internal]
interface SentryMechanism : SentrySerializable
{
    // @property (copy, nonatomic) NSString * _Nonnull type;
    [Export ("type")]
    string Type { get; set; }

    // @property (copy, nonatomic) NSString * _Nullable desc;
    [NullAllowed, Export ("desc")]
    string Desc { get; set; }

    // @property (nonatomic, strong) NSDictionary<NSString *,id> * _Nullable data;
    [NullAllowed, Export ("data", ArgumentSemantic.Strong)]
    NSDictionary<NSString, NSObject> Data { get; set; }

    // @property (copy, nonatomic) NSNumber * _Nullable handled;
    [NullAllowed, Export ("handled", ArgumentSemantic.Copy)]
    NSNumber Handled { get; set; }

    // @property (copy, nonatomic) NSString * _Nullable helpLink;
    [NullAllowed, Export ("helpLink")]
    string HelpLink { get; set; }

    // @property (nonatomic, strong) SentryMechanismMeta * _Nullable meta;
    [NullAllowed, Export ("meta", ArgumentSemantic.Strong)]
    SentryMechanismMeta Meta { get; set; }

    // -(instancetype _Nonnull)initWithType:(NSString * _Nonnull)type;
    [Export ("initWithType:")]
    NativeHandle Constructor (string type);
}

// @interface SentryMechanismMeta : NSObject <SentrySerializable>
[BaseType (typeof(NSObject))]
[Internal]
interface SentryMechanismMeta : SentrySerializable
{
    // @property (nonatomic, strong) NSDictionary<NSString *,id> * _Nullable signal;
    [NullAllowed, Export ("signal", ArgumentSemantic.Strong)]
    NSDictionary<NSString, NSObject> Signal { get; set; }

    // @property (nonatomic, strong) NSDictionary<NSString *,id> * _Nullable machException;
    [NullAllowed, Export ("machException", ArgumentSemantic.Strong)]
    NSDictionary<NSString, NSObject> MachException { get; set; }

    // @property (nonatomic, strong) SentryNSError * _Nullable error;
    [NullAllowed, Export ("error", ArgumentSemantic.Strong)]
    SentryNSError Error { get; set; }
}

// @interface SentryMessage : NSObject <SentrySerializable>
[BaseType (typeof(NSObject))]
[DisableDefaultCtor]
[Internal]
interface SentryMessage : SentrySerializable
{
    // -(instancetype _Nonnull)initWithFormatted:(NSString * _Nonnull)formatted;
    [Export ("initWithFormatted:")]
    NativeHandle Constructor (string formatted);

    // @property (readonly, copy, nonatomic) NSString * _Nonnull formatted;
    [Export ("formatted")]
    string Formatted { get; }

    // @property (copy, nonatomic) NSString * _Nullable message;
    [NullAllowed, Export ("message")]
    string Message { get; set; }

    // @property (nonatomic, strong) NSArray<NSString *> * _Nullable params;
    [NullAllowed, Export ("params", ArgumentSemantic.Strong)]
    string[] Params { get; set; }
}

// @interface SentryNSError : NSObject <SentrySerializable>
[BaseType (typeof(NSObject))]
[DisableDefaultCtor]
[Internal]
interface SentryNSError : SentrySerializable
{
    // @property (copy, nonatomic) NSString * _Nonnull domain;
    [Export ("domain")]
    string Domain { get; set; }

    // @property (assign, nonatomic) NSInteger code;
    [Export ("code")]
    nint Code { get; set; }

    // -(instancetype _Nonnull)initWithDomain:(NSString * _Nonnull)domain code:(NSInteger)code;
    [Export ("initWithDomain:code:")]
    NativeHandle Constructor (string domain, nint code);
}

// @interface SentrySDK : NSObject
[BaseType (typeof(NSObject), Name="SentrySDK")]
[DisableDefaultCtor]
[Internal]
interface SentrySdk
{
    // @property (readonly, nonatomic, class) id<SentrySpan> _Nullable span;
    [Static]
    [NullAllowed, Export ("span")]
    SentrySpan Span { get; }

    // @property (readonly, nonatomic, class) BOOL isEnabled;
    [Static]
    [Export ("isEnabled")]
    bool IsEnabled { get; }

    // +(void)startWithOptions:(NSDictionary<NSString *,id> * _Nonnull)optionsDict __attribute__((swift_name("start(options:)")));
    [Static]
    [Export ("startWithOptions:")]
    void StartWithOptions (NSDictionary<NSString, NSObject> optionsDict);

    // +(void)startWithOptionsObject:(SentryOptions * _Nonnull)options __attribute__((swift_name("start(options:)")));
    [Static]
    [Export ("startWithOptionsObject:")]
    void StartWithOptionsObject (SentryOptions options);

    // +(void)startWithConfigureOptions:(void (^ _Nonnull)(SentryOptions * _Nonnull))configureOptions __attribute__((swift_name("start(configureOptions:)")));
    [Static]
    [Export ("startWithConfigureOptions:")]
    void StartWithConfigureOptions (Action<SentryOptions> configureOptions);

    // +(SentryId * _Nonnull)captureEvent:(SentryEvent * _Nonnull)event __attribute__((swift_name("capture(event:)")));
    [Static]
    [Export ("captureEvent:")]
    SentryId CaptureEvent (SentryEvent @event);

    // +(SentryId * _Nonnull)captureEvent:(SentryEvent * _Nonnull)event withScope:(SentryScope * _Nonnull)scope __attribute__((swift_name("capture(event:scope:)")));
    [Static]
    [Export ("captureEvent:withScope:")]
    SentryId CaptureEvent (SentryEvent @event, SentryScope scope);

    // +(SentryId * _Nonnull)captureEvent:(SentryEvent * _Nonnull)event withScopeBlock:(void (^ _Nonnull)(SentryScope * _Nonnull))block __attribute__((swift_name("capture(event:block:)")));
    [Static]
    [Export ("captureEvent:withScopeBlock:")]
    SentryId CaptureEvent (SentryEvent @event, Action<SentryScope> block);

    // +(id<SentrySpan> _Nonnull)startTransactionWithName:(NSString * _Nonnull)name operation:(NSString * _Nonnull)operation __attribute__((swift_name("startTransaction(name:operation:)")));
    [Static]
    [Export ("startTransactionWithName:operation:")]
    SentrySpan StartTransactionWithName (string name, string operation);

    // +(id<SentrySpan> _Nonnull)startTransactionWithName:(NSString * _Nonnull)name operation:(NSString * _Nonnull)operation bindToScope:(BOOL)bindToScope __attribute__((swift_name("startTransaction(name:operation:bindToScope:)")));
    [Static]
    [Export ("startTransactionWithName:operation:bindToScope:")]
    SentrySpan StartTransactionWithName (string name, string operation, bool bindToScope);

    // +(id<SentrySpan> _Nonnull)startTransactionWithContext:(SentryTransactionContext * _Nonnull)transactionContext __attribute__((swift_name("startTransaction(transactionContext:)")));
    [Static]
    [Export ("startTransactionWithContext:")]
    SentrySpan StartTransactionWithContext (SentryTransactionContext transactionContext);

    // +(id<SentrySpan> _Nonnull)startTransactionWithContext:(SentryTransactionContext * _Nonnull)transactionContext bindToScope:(BOOL)bindToScope __attribute__((swift_name("startTransaction(transactionContext:bindToScope:)")));
    [Static]
    [Export ("startTransactionWithContext:bindToScope:")]
    SentrySpan StartTransactionWithContext (SentryTransactionContext transactionContext, bool bindToScope);

    // +(id<SentrySpan> _Nonnull)startTransactionWithContext:(SentryTransactionContext * _Nonnull)transactionContext bindToScope:(BOOL)bindToScope customSamplingContext:(NSDictionary<NSString *,id> * _Nonnull)customSamplingContext __attribute__((swift_name("startTransaction(transactionContext:bindToScope:customSamplingContext:)")));
    [Static]
    [Export ("startTransactionWithContext:bindToScope:customSamplingContext:")]
    SentrySpan StartTransactionWithContext (SentryTransactionContext transactionContext, bool bindToScope, NSDictionary<NSString, NSObject> customSamplingContext);

    // +(id<SentrySpan> _Nonnull)startTransactionWithContext:(SentryTransactionContext * _Nonnull)transactionContext customSamplingContext:(NSDictionary<NSString *,id> * _Nonnull)customSamplingContext __attribute__((swift_name("startTransaction(transactionContext:customSamplingContext:)")));
    [Static]
    [Export ("startTransactionWithContext:customSamplingContext:")]
    SentrySpan StartTransactionWithContext (SentryTransactionContext transactionContext, NSDictionary<NSString, NSObject> customSamplingContext);

    // +(SentryId * _Nonnull)captureError:(NSError * _Nonnull)error __attribute__((swift_name("capture(error:)")));
    [Static]
    [Export ("captureError:")]
    SentryId CaptureError (NSError error);

    // +(SentryId * _Nonnull)captureError:(NSError * _Nonnull)error withScope:(SentryScope * _Nonnull)scope __attribute__((swift_name("capture(error:scope:)")));
    [Static]
    [Export ("captureError:withScope:")]
    SentryId CaptureError (NSError error, SentryScope scope);

    // +(SentryId * _Nonnull)captureError:(NSError * _Nonnull)error withScopeBlock:(void (^ _Nonnull)(SentryScope * _Nonnull))block __attribute__((swift_name("capture(error:block:)")));
    [Static]
    [Export ("captureError:withScopeBlock:")]
    SentryId CaptureError (NSError error, Action<SentryScope> block);

    // +(SentryId * _Nonnull)captureException:(NSException * _Nonnull)exception __attribute__((swift_name("capture(exception:)")));
    [Static]
    [Export ("captureException:")]
    SentryId CaptureException (NSException exception);

    // +(SentryId * _Nonnull)captureException:(NSException * _Nonnull)exception withScope:(SentryScope * _Nonnull)scope __attribute__((swift_name("capture(exception:scope:)")));
    [Static]
    [Export ("captureException:withScope:")]
    SentryId CaptureException (NSException exception, SentryScope scope);

    // +(SentryId * _Nonnull)captureException:(NSException * _Nonnull)exception withScopeBlock:(void (^ _Nonnull)(SentryScope * _Nonnull))block __attribute__((swift_name("capture(exception:block:)")));
    [Static]
    [Export ("captureException:withScopeBlock:")]
    SentryId CaptureException (NSException exception, Action<SentryScope> block);

    // +(SentryId * _Nonnull)captureMessage:(NSString * _Nonnull)message __attribute__((swift_name("capture(message:)")));
    [Static]
    [Export ("captureMessage:")]
    SentryId CaptureMessage (string message);

    // +(SentryId * _Nonnull)captureMessage:(NSString * _Nonnull)message withScope:(SentryScope * _Nonnull)scope __attribute__((swift_name("capture(message:scope:)")));
    [Static]
    [Export ("captureMessage:withScope:")]
    SentryId CaptureMessage (string message, SentryScope scope);

    // +(SentryId * _Nonnull)captureMessage:(NSString * _Nonnull)message withScopeBlock:(void (^ _Nonnull)(SentryScope * _Nonnull))block __attribute__((swift_name("capture(message:block:)")));
    [Static]
    [Export ("captureMessage:withScopeBlock:")]
    SentryId CaptureMessage (string message, Action<SentryScope> block);

    // +(void)captureUserFeedback:(SentryUserFeedback * _Nonnull)userFeedback __attribute__((swift_name("capture(userFeedback:)")));
    [Static]
    [Export ("captureUserFeedback:")]
    void CaptureUserFeedback (SentryUserFeedback userFeedback);

    // +(void)addBreadcrumb:(SentryBreadcrumb * _Nonnull)crumb __attribute__((swift_name("addBreadcrumb(crumb:)")));
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

    // +(void)close;
    [Static]
    [Export ("close")]
    void Close ();
}

// @interface SentrySamplingContext : NSObject
[BaseType (typeof(NSObject))]
[Internal]
interface SentrySamplingContext
{
    // @property (readonly, nonatomic) SentryTransactionContext * _Nonnull transactionContext;
    [Export ("transactionContext")]
    SentryTransactionContext TransactionContext { get; }

    // @property (readonly, nonatomic) NSDictionary<NSString *,id> * _Nullable customSamplingContext;
    [NullAllowed, Export ("customSamplingContext")]
    NSDictionary<NSString, NSObject> CustomSamplingContext { get; }

    // -(instancetype _Nonnull)initWithTransactionContext:(SentryTransactionContext * _Nonnull)transactionContext;
    [Export ("initWithTransactionContext:")]
    NativeHandle Constructor (SentryTransactionContext transactionContext);

    // -(instancetype _Nonnull)initWithTransactionContext:(SentryTransactionContext * _Nonnull)transactionContext customSamplingContext:(NSDictionary<NSString *,id> * _Nonnull)customSamplingContext;
    [Export ("initWithTransactionContext:customSamplingContext:")]
    NativeHandle Constructor (SentryTransactionContext transactionContext, NSDictionary<NSString, NSObject> customSamplingContext);
}

// @interface SentryScope : NSObject <SentrySerializable>
[BaseType (typeof(NSObject))]
[Internal]
interface SentryScope : SentrySerializable
{
    // @property (nonatomic, strong) id<SentrySpan> _Nullable span;
    [NullAllowed, Export ("span", ArgumentSemantic.Strong)]
    SentrySpan Span { get; set; }

    // -(instancetype _Nonnull)initWithMaxBreadcrumbs:(NSInteger)maxBreadcrumbs __attribute__((objc_designated_initializer));
    [Export ("initWithMaxBreadcrumbs:")]
    [DesignatedInitializer]
    NativeHandle Constructor (nint maxBreadcrumbs);

    // -(instancetype _Nonnull)initWithScope:(SentryScope * _Nonnull)scope;
    [Export ("initWithScope:")]
    NativeHandle Constructor (SentryScope scope);

    // -(void)setUser:(SentryUser * _Nullable)user;
    [Export ("setUser:")]
    void SetUser ([NullAllowed] SentryUser user);

    // -(void)setTagValue:(NSString * _Nonnull)value forKey:(NSString * _Nonnull)key __attribute__((swift_name("setTag(value:key:)")));
    [Export ("setTagValue:forKey:")]
    void SetTagValue (string value, string key);

    // -(void)removeTagForKey:(NSString * _Nonnull)key __attribute__((swift_name("removeTag(key:)")));
    [Export ("removeTagForKey:")]
    void RemoveTagForKey (string key);

    // -(void)setTags:(NSDictionary<NSString *,NSString *> * _Nullable)tags;
    [Export ("setTags:")]
    void SetTags ([NullAllowed] NSDictionary<NSString, NSString> tags);

    // -(void)setExtras:(NSDictionary<NSString *,id> * _Nullable)extras;
    [Export ("setExtras:")]
    void SetExtras ([NullAllowed] NSDictionary<NSString, NSObject> extras);

    // -(void)setExtraValue:(id _Nullable)value forKey:(NSString * _Nonnull)key __attribute__((swift_name("setExtra(value:key:)")));
    [Export ("setExtraValue:forKey:")]
    void SetExtraValue ([NullAllowed] NSObject value, string key);

    // -(void)removeExtraForKey:(NSString * _Nonnull)key __attribute__((swift_name("removeExtra(key:)")));
    [Export ("removeExtraForKey:")]
    void RemoveExtraForKey (string key);

    // -(void)setDist:(NSString * _Nullable)dist;
    [Export ("setDist:")]
    void SetDist ([NullAllowed] string dist);

    // -(void)setEnvironment:(NSString * _Nullable)environment;
    [Export ("setEnvironment:")]
    void SetEnvironment ([NullAllowed] string environment);

    // -(void)setFingerprint:(NSArray<NSString *> * _Nullable)fingerprint;
    [Export ("setFingerprint:")]
    void SetFingerprint ([NullAllowed] string[] fingerprint);

    // -(void)setLevel:(enum SentryLevel)level;
    [Export ("setLevel:")]
    void SetLevel (SentryLevel level);

    // -(void)addBreadcrumb:(SentryBreadcrumb * _Nonnull)crumb;
    [Export ("addBreadcrumb:")]
    void AddBreadcrumb (SentryBreadcrumb crumb);

    // -(void)clearBreadcrumbs;
    [Export ("clearBreadcrumbs")]
    void ClearBreadcrumbs ();

    // -(NSDictionary<NSString *,id> * _Nonnull)serialize;
    [Export ("serialize")]
    NSDictionary<NSString, NSObject> Serialize();

    // -(SentryEvent * _Nullable)applyToEvent:(SentryEvent * _Nonnull)event maxBreadcrumb:(NSUInteger)maxBreadcrumbs;
    [Export ("applyToEvent:maxBreadcrumb:")]
    [return: NullAllowed]
    SentryEvent ApplyToEvent (SentryEvent @event, nuint maxBreadcrumbs);

    // -(void)applyToSession:(SentrySession * _Nonnull)session;
    [Export ("applyToSession:")]
    void ApplyToSession (SentrySession session);

    // -(void)setContextValue:(NSDictionary<NSString *,id> * _Nonnull)value forKey:(NSString * _Nonnull)key __attribute__((swift_name("setContext(value:key:)")));
    [Export ("setContextValue:forKey:")]
    void SetContextValue (NSDictionary<NSString, NSObject> value, string key);

    // -(void)removeContextForKey:(NSString * _Nonnull)key __attribute__((swift_name("removeContext(key:)")));
    [Export ("removeContextForKey:")]
    void RemoveContextForKey (string key);

    // -(void)addAttachment:(SentryAttachment * _Nonnull)attachment;
    [Export ("addAttachment:")]
    void AddAttachment (SentryAttachment attachment);

    // -(void)clearAttachments;
    [Export ("clearAttachments")]
    void ClearAttachments ();

    // -(void)clear;
    [Export ("clear")]
    void Clear ();

    // -(void)useSpan:(SentrySpanCallback _Nonnull)callback;
    [Export ("useSpan:")]
    void UseSpan (Action<SentrySpan?> callback);
}

// @interface SentryScreenFrames : NSObject
[BaseType (typeof(NSObject))]
[DisableDefaultCtor]
[Internal]
interface SentryScreenFrames
{
    // -(instancetype _Nonnull)initWithTotal:(NSUInteger)total frozen:(NSUInteger)frozen slow:(NSUInteger)slow;
    [Export ("initWithTotal:frozen:slow:")]
    NativeHandle Constructor (nuint total, nuint frozen, nuint slow);

    // -(instancetype _Nonnull)initWithTotal:(NSUInteger)total frozen:(NSUInteger)frozen slow:(NSUInteger)slow frameTimestamps:(SentryFrameInfoTimeSeries * _Nonnull)frameTimestamps frameRateTimestamps:(SentryFrameInfoTimeSeries * _Nonnull)frameRateTimestamps;
    [Export ("initWithTotal:frozen:slow:frameTimestamps:frameRateTimestamps:")]
    NativeHandle Constructor (nuint total, nuint frozen, nuint slow, NSDictionary<NSString, NSNumber>[] frameTimestamps, NSDictionary<NSString, NSNumber>[] frameRateTimestamps);

    // @property (readonly, assign, nonatomic) NSUInteger total;
    [Export ("total")]
    nuint Total { get; }

    // @property (readonly, assign, nonatomic) NSUInteger frozen;
    [Export ("frozen")]
    nuint Frozen { get; }

    // @property (readonly, assign, nonatomic) NSUInteger slow;
    [Export ("slow")]
    nuint Slow { get; }

    // @property (readonly, copy, nonatomic) SentryFrameInfoTimeSeries * _Nonnull frameTimestamps;
    [Export ("frameTimestamps", ArgumentSemantic.Copy)]
    NSDictionary<NSString, NSNumber>[] FrameTimestamps { get; }

    // @property (readonly, copy, nonatomic) SentryFrameInfoTimeSeries * _Nonnull frameRateTimestamps;
    [Export ("frameRateTimestamps", ArgumentSemantic.Copy)]
    NSDictionary<NSString, NSNumber>[] FrameRateTimestamps { get; }
}

// @interface SentrySdkInfo : NSObject <SentrySerializable>
[BaseType (typeof(NSObject))]
[DisableDefaultCtor]
[Internal]
interface SentrySdkInfo : SentrySerializable
{
    // @property (readonly, copy, nonatomic) NSString * _Nonnull name;
    [Export ("name")]
    string Name { get; }

    // @property (readonly, copy, nonatomic) NSString * _Nonnull version;
    [Export ("version")]
    string Version { get; }

    // -(instancetype _Nonnull)initWithName:(NSString * _Nonnull)name andVersion:(NSString * _Nonnull)version __attribute__((objc_designated_initializer));
    [Export ("initWithName:andVersion:")]
    [DesignatedInitializer]
    NativeHandle Constructor (string name, string version);

    // -(instancetype _Nonnull)initWithDict:(NSDictionary * _Nonnull)dict;
    [Export ("initWithDict:")]
    NativeHandle Constructor (NSDictionary dict);

    // -(instancetype _Nonnull)initWithDict:(NSDictionary * _Nonnull)dict orDefaults:(SentrySdkInfo * _Nonnull)info;
    [Export ("initWithDict:orDefaults:")]
    NativeHandle Constructor (NSDictionary dict, SentrySdkInfo info);
}

// @interface SentrySession : NSObject <SentrySerializable, NSCopying>
[BaseType (typeof(NSObject))]
[DisableDefaultCtor]
[Internal]
interface SentrySession : SentrySerializable //, INSCopying
{
    // -(instancetype _Nonnull)initWithReleaseName:(NSString * _Nonnull)releaseName;
    [Export ("initWithReleaseName:")]
    NativeHandle Constructor (string releaseName);

    // -(instancetype _Nullable)initWithJSONObject:(NSDictionary * _Nonnull)jsonObject;
    [Export ("initWithJSONObject:")]
    NativeHandle Constructor (NSDictionary jsonObject);

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

    // @property (readonly, nonatomic, strong) NSUUID * _Nonnull sessionId;
    [Export ("sessionId", ArgumentSemantic.Strong)]
    NSUuid SessionId { get; }

    // @property (readonly, nonatomic, strong) NSDate * _Nonnull started;
    [Export ("started", ArgumentSemantic.Strong)]
    NSDate Started { get; }

    // @property (readonly, nonatomic) enum SentrySessionStatus status;
    [Export ("status")]
    SentrySessionStatus Status { get; }

    // @property (readonly, nonatomic) NSUInteger errors;
    [Export ("errors")]
    nuint Errors { get; }

    // @property (readonly, nonatomic) NSUInteger sequence;
    [Export ("sequence")]
    nuint Sequence { get; }

    // @property (readonly, nonatomic, strong) NSString * _Nonnull distinctId;
    [Export ("distinctId", ArgumentSemantic.Strong)]
    string DistinctId { get; }

    // @property (readonly, copy, nonatomic) NSNumber * _Nullable flagInit;
    [NullAllowed, Export ("flagInit", ArgumentSemantic.Copy)]
    NSNumber FlagInit { get; }

    // @property (readonly, nonatomic, strong) NSDate * _Nullable timestamp;
    [NullAllowed, Export ("timestamp", ArgumentSemantic.Strong)]
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

    // @property (copy, nonatomic) SentryUser * _Nullable user;
    [NullAllowed, Export ("user", ArgumentSemantic.Copy)]
    SentryUser User { get; set; }

    // -(NSDictionary<NSString *,id> * _Nonnull)serialize;
    [Export ("serialize")]
    NSDictionary<NSString, NSObject> Serialize();
}

// @interface SentrySpanId : NSObject <NSCopying>
[BaseType (typeof(NSObject))]
[Internal]
interface SentrySpanId // : INSCopying
{
    // -(instancetype _Nonnull)initWithUUID:(NSUUID * _Nonnull)uuid;
    [Export ("initWithUUID:")]
    NativeHandle Constructor (NSUuid uuid);

    // -(instancetype _Nonnull)initWithValue:(NSString * _Nonnull)value;
    [Export ("initWithValue:")]
    NativeHandle Constructor (string value);

    // @property (readonly, copy) NSString * _Nonnull sentrySpanIdString;
    [Export ("sentrySpanIdString")]
    string SentrySpanIdString { get; }

    // @property (readonly, nonatomic, strong, class) SentrySpanId * _Nonnull empty;
    [Static]
    [Export ("empty", ArgumentSemantic.Strong)]
    SentrySpanId Empty { get; }
}

// @interface SentryStacktrace : NSObject <SentrySerializable>
[BaseType (typeof(NSObject))]
[DisableDefaultCtor]
[Internal]
interface SentryStacktrace : SentrySerializable
{
    // @property (nonatomic, strong) NSArray<SentryFrame *> * _Nonnull frames;
    [Export ("frames", ArgumentSemantic.Strong)]
    SentryFrame[] Frames { get; set; }

    // @property (nonatomic, strong) NSDictionary<NSString *,NSString *> * _Nonnull registers;
    [Export ("registers", ArgumentSemantic.Strong)]
    NSDictionary<NSString, NSString> Registers { get; set; }

    // -(instancetype _Nonnull)initWithFrames:(NSArray<SentryFrame *> * _Nonnull)frames registers:(NSDictionary<NSString *,NSString *> * _Nonnull)registers;
    [Export ("initWithFrames:registers:")]
    NativeHandle Constructor (SentryFrame[] frames, NSDictionary<NSString, NSString> registers);

    // -(void)fixDuplicateFrames;
    [Export ("fixDuplicateFrames")]
    void FixDuplicateFrames ();
}

// @interface SentryThread : NSObject <SentrySerializable>
[BaseType (typeof(NSObject))]
[DisableDefaultCtor]
[Internal]
interface SentryThread : SentrySerializable
{
    // @property (copy, nonatomic) NSNumber * _Nonnull threadId;
    [Export ("threadId", ArgumentSemantic.Copy)]
    NSNumber ThreadId { get; set; }

    // @property (copy, nonatomic) NSString * _Nullable name;
    [NullAllowed, Export ("name")]
    string Name { get; set; }

    // @property (nonatomic, strong) SentryStacktrace * _Nullable stacktrace;
    [NullAllowed, Export ("stacktrace", ArgumentSemantic.Strong)]
    SentryStacktrace Stacktrace { get; set; }

    // @property (copy, nonatomic) NSNumber * _Nullable crashed;
    [NullAllowed, Export ("crashed", ArgumentSemantic.Copy)]
    NSNumber Crashed { get; set; }

    // @property (copy, nonatomic) NSNumber * _Nullable current;
    [NullAllowed, Export ("current", ArgumentSemantic.Copy)]
    NSNumber Current { get; set; }

    // -(instancetype _Nonnull)initWithThreadId:(NSNumber * _Nonnull)threadId;
    [Export ("initWithThreadId:")]
    NativeHandle Constructor (NSNumber threadId);
}

// @interface SentryTraceHeader : NSObject
[BaseType (typeof(NSObject))]
[DisableDefaultCtor]
[Internal]
interface SentryTraceHeader
{
    // @property (readonly, nonatomic) SentryId * _Nonnull traceId;
    [Export ("traceId")]
    SentryId TraceId { get; }

    // @property (readonly, nonatomic) SentrySpanId * _Nonnull spanId;
    [Export ("spanId")]
    SentrySpanId SpanId { get; }

    // @property (readonly, nonatomic) SentrySampleDecision sampled;
    [Export ("sampled")]
    SentrySampleDecision Sampled { get; }

    // -(instancetype _Nonnull)initWithTraceId:(SentryId * _Nonnull)traceId spanId:(SentrySpanId * _Nonnull)spanId sampled:(SentrySampleDecision)sampled;
    [Export ("initWithTraceId:spanId:sampled:")]
    NativeHandle Constructor (SentryId traceId, SentrySpanId spanId, SentrySampleDecision sampled);

    // -(NSString * _Nonnull)value;
    [Export ("value")]
    string Value { get; }
}

// @interface SentryTransactionContext : SentrySpanContext
[BaseType (typeof(SentrySpanContext))]
[DisableDefaultCtor]
[Internal]
interface SentryTransactionContext
{
    // @property (readonly, nonatomic) NSString * _Nonnull name;
    [Export ("name")]
    string Name { get; }

	// @property (readonly, nonatomic) SentryTransactionNameSource nameSource;
	[Export ("nameSource")]
	SentryTransactionNameSource NameSource { get; }

    // @property (nonatomic) SentrySampleDecision parentSampled;
    [Export ("parentSampled", ArgumentSemantic.Assign)]
    SentrySampleDecision ParentSampled { get; set; }

    // @property (nonatomic, strong) NSNumber * _Nullable sampleRate;
    [NullAllowed, Export ("sampleRate", ArgumentSemantic.Strong)]
    NSNumber SampleRate { get; set; }

    // -(instancetype _Nonnull)initWithName:(NSString * _Nonnull)name operation:(NSString * _Nonnull)operation;
    [Export ("initWithName:operation:")]
    NativeHandle Constructor (string name, string operation);

    // -(instancetype _Nonnull)initWithName:(NSString * _Nonnull)name operation:(NSString * _Nonnull)operation sampled:(SentrySampleDecision)sampled;
    [Export ("initWithName:operation:sampled:")]
    NativeHandle Constructor (string name, string operation, SentrySampleDecision sampled);

    // -(instancetype _Nonnull)initWithName:(NSString * _Nonnull)name operation:(NSString * _Nonnull)operation traceId:(SentryId * _Nonnull)traceId spanId:(SentrySpanId * _Nonnull)spanId parentSpanId:(SentrySpanId * _Nullable)parentSpanId parentSampled:(SentrySampleDecision)parentSampled;
    [Export ("initWithName:operation:traceId:spanId:parentSpanId:parentSampled:")]
    NativeHandle Constructor (string name, string operation, SentryId traceId, SentrySpanId spanId, [NullAllowed] SentrySpanId parentSpanId, SentrySampleDecision parentSampled);
}

// @interface SentryUser : NSObject <SentrySerializable, NSCopying>
[BaseType (typeof(NSObject))]
[Internal]
interface SentryUser : SentrySerializable //, INSCopying
{
	// @property (copy, atomic) NSString * _Nullable userId;
	[NullAllowed, Export ("userId")]
    string UserId { get; set; }

    // @property (copy, atomic) NSString * _Nullable email;
    [NullAllowed, Export ("email")]
    string Email { get; set; }

    // @property (copy, atomic) NSString * _Nullable username;
    [NullAllowed, Export ("username")]
    string Username { get; set; }

    // @property (copy, atomic) NSString * _Nullable ipAddress;
    [NullAllowed, Export ("ipAddress")]
    string IpAddress { get; set; }

    // @property (atomic, strong) NSDictionary<NSString *,id> * _Nullable data;
    [NullAllowed, Export ("data", ArgumentSemantic.Strong)]
    NSDictionary<NSString, NSObject> Data { get; set; }

    // -(instancetype _Nonnull)initWithUserId:(NSString * _Nonnull)userId;
    [Export ("initWithUserId:")]
    NativeHandle Constructor (string userId);

    // // -(BOOL)isEqual:(id _Nullable)other;
    // [Export ("isEqual:")]
    // bool IsEqual ([NullAllowed] NSObject other);

    // -(BOOL)isEqualToUser:(SentryUser * _Nonnull)user;
    [Export ("isEqualToUser:")]
    bool IsEqualToUser (SentryUser user);

    // -(NSUInteger)hash;
    [Export ("hash")]
    nuint Hash { get; }
}

// @interface SentryUserFeedback : NSObject <SentrySerializable>
[BaseType (typeof(NSObject))]
[DisableDefaultCtor]
[Internal]
interface SentryUserFeedback : SentrySerializable
{
    // -(instancetype _Nonnull)initWithEventId:(SentryId * _Nonnull)eventId;
    [Export ("initWithEventId:")]
    NativeHandle Constructor (SentryId eventId);

    // @property (readonly, nonatomic, strong) SentryId * _Nonnull eventId;
    [Export ("eventId", ArgumentSemantic.Strong)]
    SentryId EventId { get; }

    // @property (copy, nonatomic) NSString * _Nonnull name;
    [Export ("name")]
    string Name { get; set; }

    // @property (copy, nonatomic) NSString * _Nonnull email;
    [Export ("email")]
    string Email { get; set; }

    // @property (copy, nonatomic) NSString * _Nonnull comments;
    [Export ("comments")]
    string Comments { get; set; }
}
