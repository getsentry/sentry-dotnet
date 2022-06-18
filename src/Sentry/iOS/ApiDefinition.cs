using System;
using Foundation;
using ObjCRuntime;

namespace Sentry.iOS
{
    //[Static]
    //[Verify(ConstantsInterfaceAssociation)]
    //partial interface Constants
    //{
    //	// extern double SentryVersionNumber;
    //	[Field("SentryVersionNumber", "__Internal")]
    //	double SentryVersionNumber { get; }

    //	// extern const unsigned char [] SentryVersionString;
    //	[Field("SentryVersionString", "__Internal")]
    //	byte[] SentryVersionString { get; }
    //}

    // typedef void (^SentryRequestFinished)(NSError * _Nullable);
    [Internal]
    internal delegate void SentryRequestFinished([NullAllowed] NSError arg0);

    // typedef void (^SentryRequestOperationFinished)(NSHTTPURLResponse * _Nullable, NSError * _Nullable);
    [Internal]
    internal delegate void SentryRequestOperationFinished([NullAllowed] NSHttpUrlResponse arg0, [NullAllowed] NSError arg1);

    // typedef SentryBreadcrumb * _Nullable (^SentryBeforeBreadcrumbCallback)(SentryBreadcrumb * _Nonnull);
    [Internal]
    internal delegate SentryBreadcrumb SentryBeforeBreadcrumbCallback(SentryBreadcrumb arg0);

    // typedef SentryEvent * _Nullable (^SentryBeforeSendEventCallback)(SentryEvent * _Nonnull);
    [Internal]
    internal delegate SentryEvent SentryBeforeSendEventCallback(SentryEvent arg0);

    // typedef BOOL (^SentryShouldQueueEvent)(NSHTTPURLResponse * _Nullable, NSError * _Nullable);
    [Internal]
    internal delegate bool SentryShouldQueueEvent([NullAllowed] NSHttpUrlResponse arg0, [NullAllowed] NSError arg1);

    // @protocol SentrySerializable <NSObject>
    /*
      Check whether adding [Model] to this declaration is appropriate.
      [Model] is used to generate a C# class that implements this protocol,
      and might be useful for protocols that consumers are supposed to implement,
      since consumers can subclass the generated class instead of implementing
      the generated interface. If consumers are not supposed to implement this
      protocol, then [Model] is redundant and will generate code that will never
      be used.
    */
    [Internal]
    [Protocol]
    [BaseType(typeof(NSObject))]
    [DisableDefaultCtor]
    interface SentrySerializable
    {
        // @required -(NSDictionary<NSString *,id> * _Nonnull)serialize;
        [Abstract]
        [Export("serialize")]
        //[Verify(MethodToProperty)]
        NSDictionary<NSString, NSObject> Serialize { get; }
    }

    [Internal]
    interface ISentrySerializable
    {

    }
    // @interface SentryBreadcrumb : NSObject <SentrySerializable>
    [Internal]
    [BaseType(typeof(NSObject))]
    interface SentryBreadcrumb : ISentrySerializable
    {
        // @property (nonatomic) enum SentryLevel level;
        [Export("level", ArgumentSemantic.Assign)]
        SentryLevel Level { get; set; }

        // @property (copy, nonatomic) NSString * _Nonnull category;
        [Export("category")]
        string Category { get; set; }

        // @property (nonatomic, strong) NSDate * _Nullable timestamp;
        [NullAllowed, Export("timestamp", ArgumentSemantic.Strong)]
        NSDate Timestamp { get; set; }

        // @property (copy, nonatomic) NSString * _Nullable type;
        [NullAllowed, Export("type")]
        string Type { get; set; }

        // @property (copy, nonatomic) NSString * _Nullable message;
        [NullAllowed, Export("message")]
        string Message { get; set; }

        // @property (nonatomic, strong) NSDictionary<NSString *,id> * _Nullable data;
        [NullAllowed, Export("data", ArgumentSemantic.Strong)]
        NSDictionary<NSString, NSObject> Data { get; set; }

        // -(instancetype _Nonnull)initWithLevel:(enum SentryLevel)level category:(NSString * _Nonnull)category;
        [Export("initWithLevel:category:")]
        IntPtr Constructor(SentryLevel level, string category);

        // -(NSDictionary<NSString *,id> * _Nonnull)serialize;
        [Export("serialize")]
        //[Verify(MethodToProperty)]
        NSDictionary<NSString, NSObject> Serialize { get; }

        // error CS0114: 'SentryBreadcrumb.IsEqual(NSObject?)' hides inherited member 'NSObject.IsEqual(NSObject?)'.
        // // -(BOOL)isEqual:(id _Nullable)other;
        // [Export("isEqual:")]
        // bool IsEqual([NullAllowed] NSObject other);

        // -(BOOL)isEqualToBreadcrumb:(SentryBreadcrumb * _Nonnull)breadcrumb;
        [Export("isEqualToBreadcrumb:")]
        bool IsEqualToBreadcrumb(SentryBreadcrumb breadcrumb);

        // -(NSUInteger)hash;
        [Export("hash")]
        //[Verify(MethodToProperty)]
        nuint Hash { get; }
        // new NSObject IsEqual(NSObject? obj);
    }

    // @interface SentryClient : NSObject
    [Internal]
    [BaseType(typeof(NSObject))]
    [DisableDefaultCtor]
    interface SentryClient
    {
        // @property (nonatomic, strong) SentryOptions * _Nonnull options;
        [Export("options", ArgumentSemantic.Strong)]
        SentryOptions Options { get; set; }

        // -(instancetype _Nullable)initWithOptions:(SentryOptions * _Nonnull)options;
        [Export("initWithOptions:")]
        IntPtr Constructor(SentryOptions options);

        // -(SentryId * _Nonnull)captureEvent:(SentryEvent * _Nonnull)event __attribute__((swift_name("capture(event:)")));
        [Export("captureEvent:")]
        SentryId CaptureEvent(SentryEvent @event);

        // -(SentryId * _Nonnull)captureEvent:(SentryEvent * _Nonnull)event withScope:(SentryScope * _Nonnull)scope __attribute__((swift_name("capture(event:scope:)")));
        [Export("captureEvent:withScope:")]
        SentryId CaptureEvent(SentryEvent @event, SentryScope scope);

        // -(SentryId * _Nonnull)captureError:(NSError * _Nonnull)error __attribute__((swift_name("capture(error:)")));
        [Export("captureError:")]
        SentryId CaptureError(NSError error);

        // -(SentryId * _Nonnull)captureError:(NSError * _Nonnull)error withScope:(SentryScope * _Nonnull)scope __attribute__((swift_name("capture(error:scope:)")));
        [Export("captureError:withScope:")]
        SentryId CaptureError(NSError error, SentryScope scope);

        // -(SentryId * _Nonnull)captureException:(NSException * _Nonnull)exception __attribute__((swift_name("capture(exception:)")));
        [Export("captureException:")]
        SentryId CaptureException(NSException exception);

        // -(SentryId * _Nonnull)captureException:(NSException * _Nonnull)exception withScope:(SentryScope * _Nonnull)scope __attribute__((swift_name("capture(exception:scope:)")));
        [Export("captureException:withScope:")]
        SentryId CaptureException(NSException exception, SentryScope scope);

        // -(SentryId * _Nonnull)captureMessage:(NSString * _Nonnull)message __attribute__((swift_name("capture(message:)")));
        [Export("captureMessage:")]
        SentryId CaptureMessage(string message);

        // -(SentryId * _Nonnull)captureMessage:(NSString * _Nonnull)message withScope:(SentryScope * _Nonnull)scope __attribute__((swift_name("capture(message:scope:)")));
        [Export("captureMessage:withScope:")]
        SentryId CaptureMessage(string message, SentryScope scope);

        // -(void)captureSession:(SentrySession * _Nonnull)session __attribute__((swift_name("capture(session:)")));
        [Export("captureSession:")]
        void CaptureSession(SentrySession session);

        // -(SentryFileManager * _Nonnull)fileManager;
        //[Export("fileManager")]
        //[Verify(MethodToProperty)]
        //SentryFileManager FileManager { get; }
    }

    // @interface SentryCrashExceptionApplication : NSObject
    [Internal]
    [BaseType(typeof(NSObject))]
    interface SentryCrashExceptionApplication
    {
    }

    // @interface SentryDebugMeta : NSObject <SentrySerializable>
    [Internal]
    [BaseType(typeof(NSObject))]
    interface SentryDebugMeta : ISentrySerializable
    {
        // @property (copy, nonatomic) NSString * _Nullable uuid;
        [NullAllowed, Export("uuid")]
        string Uuid { get; set; }

        // @property (copy, nonatomic) NSString * _Nullable type;
        [NullAllowed, Export("type")]
        string Type { get; set; }

        // @property (copy, nonatomic) NSString * _Nullable name;
        [NullAllowed, Export("name")]
        string Name { get; set; }

        // @property (copy, nonatomic) NSNumber * _Nullable imageSize;
        [NullAllowed, Export("imageSize", ArgumentSemantic.Copy)]
        NSNumber ImageSize { get; set; }

        // @property (copy, nonatomic) NSString * _Nullable imageAddress;
        [NullAllowed, Export("imageAddress")]
        string ImageAddress { get; set; }

        // @property (copy, nonatomic) NSString * _Nullable imageVmAddress;
        [NullAllowed, Export("imageVmAddress")]
        string ImageVmAddress { get; set; }
    }

    // @interface SentryDsn : NSObject
    [Internal]
    [BaseType(typeof(NSObject))]
    interface SentryDsn
    {
        // @property (readonly, nonatomic, strong) NSURL * _Nonnull url;
        [Export("url", ArgumentSemantic.Strong)]
        NSUrl Url { get; }

        // -(instancetype _Nullable)initWithString:(NSString * _Nonnull)dsnString didFailWithError:(NSError * _Nullable * _Nullable)error;
        [Export("initWithString:didFailWithError:")]
        IntPtr Constructor(string dsnString, [NullAllowed] out NSError error);

        // -(NSString * _Nonnull)getHash;
        //[Export("getHash")]
        //[Verify(MethodToProperty)]
        //string Hash { get; }

        // -(NSURL * _Nonnull)getStoreEndpoint;
        [Export("getStoreEndpoint")]
        //[Verify(MethodToProperty)]
        NSUrl StoreEndpoint { get; }

        // -(NSURL * _Nonnull)getEnvelopeEndpoint;
        [Export("getEnvelopeEndpoint")]
        //[Verify(MethodToProperty)]
        NSUrl EnvelopeEndpoint { get; }
    }

    // @interface SentryEnvelopeHeader : NSObject
    [Internal]
    [BaseType(typeof(NSObject))]
    [DisableDefaultCtor]
    interface SentryEnvelopeHeader
    {
        // -(instancetype _Nonnull)initWithId:(SentryId * _Nullable)eventId;
        [Export("initWithId:")]
        IntPtr Constructor([NullAllowed] SentryId eventId);

        // -(instancetype _Nonnull)initWithId:(SentryId * _Nullable)eventId andSdkInfo:(SentrySdkInfo * _Nullable)sdkInfo __attribute__((objc_designated_initializer));
        //[Export("initWithId:andSdkInfo:")]
        //[DesignatedInitializer]
        //IntPtr Constructor([NullAllowed] SentryId eventId, [NullAllowed] SentrySdkInfo sdkInfo);

        // @property (readonly, copy, nonatomic) SentryId * _Nullable eventId;
        [NullAllowed, Export("eventId", ArgumentSemantic.Copy)]
        SentryId EventId { get; }

        //// @property (readonly, copy, nonatomic) SentrySdkInfo * _Nullable sdkInfo;
        //[NullAllowed, Export("sdkInfo", ArgumentSemantic.Copy)]
        //SentrySdkInfo SdkInfo { get; }
    }

    // @interface SentryEnvelopeItemHeader : NSObject
    [Internal]
    [BaseType(typeof(NSObject))]
    [DisableDefaultCtor]
    interface SentryEnvelopeItemHeader
    {
        // -(instancetype _Nonnull)initWithType:(NSString * _Nonnull)type length:(NSUInteger)length __attribute__((objc_designated_initializer));
        [Export("initWithType:length:")]
        [DesignatedInitializer]
        IntPtr Constructor(string type, nuint length);

        // @property (readonly, copy, nonatomic) NSString * _Nonnull type;
        [Export("type")]
        string Type { get; }

        // @property (readonly, nonatomic) NSUInteger length;
        [Export("length")]
        nuint Length { get; }
    }

    // @interface SentryEnvelopeItem : NSObject
    [Internal]
    [BaseType(typeof(NSObject))]
    [DisableDefaultCtor]
    interface SentryEnvelopeItem
    {
        // -(instancetype _Nonnull)initWithEvent:(SentryEvent * _Nonnull)event;
        [Export("initWithEvent:")]
        IntPtr Constructor(SentryEvent @event);

        // -(instancetype _Nonnull)initWithSession:(SentrySession * _Nonnull)session;
        [Export("initWithSession:")]
        IntPtr Constructor(SentrySession session);

        // -(instancetype _Nonnull)initWithHeader:(SentryEnvelopeItemHeader * _Nonnull)header data:(NSData * _Nonnull)data __attribute__((objc_designated_initializer));
        [Export("initWithHeader:data:")]
        [DesignatedInitializer]
        IntPtr Constructor(SentryEnvelopeItemHeader header, NSData data);

        // @property (readonly, nonatomic, strong) SentryEnvelopeItemHeader * _Nonnull header;
        [Export("header", ArgumentSemantic.Strong)]
        SentryEnvelopeItemHeader Header { get; }

        // @property (readonly, nonatomic, strong) NSData * _Nonnull data;
        [Export("data", ArgumentSemantic.Strong)]
        NSData Data { get; }
    }

    // @interface SentryEnvelope : NSObject
    [Internal]
    [BaseType(typeof(NSObject))]
    [DisableDefaultCtor]
    interface SentryEnvelope
    {
        // -(instancetype _Nonnull)initWithId:(SentryId * _Nullable)id singleItem:(SentryEnvelopeItem * _Nonnull)item;
        [Export("initWithId:singleItem:")]
        IntPtr Constructor([NullAllowed] SentryId id, SentryEnvelopeItem item);

        // -(instancetype _Nonnull)initWithHeader:(SentryEnvelopeHeader * _Nonnull)header singleItem:(SentryEnvelopeItem * _Nonnull)item;
        [Export("initWithHeader:singleItem:")]
        IntPtr Constructor(SentryEnvelopeHeader header, SentryEnvelopeItem item);

        // -(instancetype _Nonnull)initWithId:(SentryId * _Nullable)id items:(NSArray<SentryEnvelopeItem *> * _Nonnull)items;
        [Export("initWithId:items:")]
        IntPtr Constructor([NullAllowed] SentryId id, SentryEnvelopeItem[] items);

        // -(instancetype _Nonnull)initWithSession:(SentrySession * _Nonnull)session;
        [Export("initWithSession:")]
        IntPtr Constructor(SentrySession session);

        // -(instancetype _Nonnull)initWithSessions:(NSArray<SentrySession *> * _Nonnull)sessions;
        [Export("initWithSessions:")]
        IntPtr Constructor(SentrySession[] sessions);

        // -(instancetype _Nonnull)initWithHeader:(SentryEnvelopeHeader * _Nonnull)header items:(NSArray<SentryEnvelopeItem *> * _Nonnull)items __attribute__((objc_designated_initializer));
        [Export("initWithHeader:items:")]
        [DesignatedInitializer]
        IntPtr Constructor(SentryEnvelopeHeader header, SentryEnvelopeItem[] items);

        // -(instancetype _Nonnull)initWithEvent:(SentryEvent * _Nonnull)event;
        [Export("initWithEvent:")]
        IntPtr Constructor(SentryEvent @event);

        // @property (readonly, nonatomic, strong) SentryEnvelopeHeader * _Nonnull header;
        [Export("header", ArgumentSemantic.Strong)]
        SentryEnvelopeHeader Header { get; }

        // @property (readonly, nonatomic, strong) NSArray<SentryEnvelopeItem *> * _Nonnull items;
        [Export("items", ArgumentSemantic.Strong)]
        SentryEnvelopeItem[] Items { get; }
    }

    [Internal]
    [Static]
    //[Verify(ConstantsInterfaceAssociation)]
    partial interface Constants
    {
        // extern NSString *const _Nonnull SentryErrorDomain __attribute__((visibility("default")));
        [Field("SentryErrorDomain", "__Internal")]
        NSString SentryErrorDomain { get; }
    }

    // @interface SentryEvent : NSObject <SentrySerializable>
    [Internal]
    [BaseType(typeof(NSObject))]
    interface SentryEvent : ISentrySerializable
    {
        // @property (nonatomic, strong) SentryId * _Nonnull eventId;
        [Export("eventId", ArgumentSemantic.Strong)]
        SentryId EventId { get; set; }

        // @property (nonatomic, strong) SentryMessage * _Nonnull message;
        [Export("message", ArgumentSemantic.Strong)]
        SentryMessage Message { get; set; }

        // @property (nonatomic, strong) NSDate * _Nonnull timestamp;
        [Export("timestamp", ArgumentSemantic.Strong)]
        NSDate Timestamp { get; set; }

        // @property (nonatomic, strong) NSDate * _Nullable startTimestamp;
        [NullAllowed, Export("startTimestamp", ArgumentSemantic.Strong)]
        NSDate StartTimestamp { get; set; }

        // @property (nonatomic) enum SentryLevel level;
        [Export("level", ArgumentSemantic.Assign)]
        SentryLevel Level { get; set; }

        // @property (copy, nonatomic) NSString * _Nonnull platform;
        [Export("platform")]
        string Platform { get; set; }

        // @property (copy, nonatomic) NSString * _Nullable logger;
        [NullAllowed, Export("logger")]
        string Logger { get; set; }

        // @property (copy, nonatomic) NSString * _Nullable serverName;
        [NullAllowed, Export("serverName")]
        string ServerName { get; set; }

        // @property (copy, nonatomic) NSString * _Nullable releaseName;
        [NullAllowed, Export("releaseName")]
        string ReleaseName { get; set; }

        // @property (copy, nonatomic) NSString * _Nullable dist;
        [NullAllowed, Export("dist")]
        string Dist { get; set; }

        // @property (copy, nonatomic) NSString * _Nullable environment;
        [NullAllowed, Export("environment")]
        string Environment { get; set; }

        // @property (copy, nonatomic) NSString * _Nullable transaction;
        [NullAllowed, Export("transaction")]
        string Transaction { get; set; }

        // @property (copy, nonatomic) NSString * _Nullable type;
        [NullAllowed, Export("type")]
        string Type { get; set; }

        // @property (nonatomic, strong) NSDictionary<NSString *,NSString *> * _Nullable tags;
        [NullAllowed, Export("tags", ArgumentSemantic.Strong)]
        NSDictionary<NSString, NSString> Tags { get; set; }

        // @property (nonatomic, strong) NSDictionary<NSString *,id> * _Nullable extra;
        [NullAllowed, Export("extra", ArgumentSemantic.Strong)]
        NSDictionary<NSString, NSObject> Extra { get; set; }

        // @property (nonatomic, strong) NSDictionary<NSString *,id> * _Nullable sdk;
        [NullAllowed, Export("sdk", ArgumentSemantic.Strong)]
        NSDictionary<NSString, NSObject> Sdk { get; set; }

        // @property (nonatomic, strong) NSDictionary<NSString *,NSString *> * _Nullable modules;
        [NullAllowed, Export("modules", ArgumentSemantic.Strong)]
        NSDictionary<NSString, NSString> Modules { get; set; }

        // @property (nonatomic, strong) NSArray<NSString *> * _Nullable fingerprint;
        [NullAllowed, Export("fingerprint", ArgumentSemantic.Strong)]
        string[] Fingerprint { get; set; }

        // @property (nonatomic, strong) SentryUser * _Nullable user;
        [NullAllowed, Export("user", ArgumentSemantic.Strong)]
        SentryUser User { get; set; }

        // @property (nonatomic, strong) NSDictionary<NSString *,NSDictionary<NSString *,id> *> * _Nullable context;
        [NullAllowed, Export("context", ArgumentSemantic.Strong)]
        NSDictionary<NSString, NSDictionary<NSString, NSObject>> Context { get; set; }

        // @property (nonatomic, strong) NSArray<SentryThread *> * _Nullable threads;
        [NullAllowed, Export("threads", ArgumentSemantic.Strong)]
        SentryThread[] Threads { get; set; }

        // @property (nonatomic, strong) NSArray<SentryException *> * _Nullable exceptions;
        [NullAllowed, Export("exceptions", ArgumentSemantic.Strong)]
        SentryException[] Exceptions { get; set; }

        // @property (nonatomic, strong) SentryStacktrace * _Nullable stacktrace;
        [NullAllowed, Export("stacktrace", ArgumentSemantic.Strong)]
        SentryStacktrace Stacktrace { get; set; }

        // @property (nonatomic, strong) NSArray<SentryDebugMeta *> * _Nullable debugMeta;
        [NullAllowed, Export("debugMeta", ArgumentSemantic.Strong)]
        SentryDebugMeta[] DebugMeta { get; set; }

        // @property (nonatomic, strong) NSArray<SentryBreadcrumb *> * _Nullable breadcrumbs;
        [NullAllowed, Export("breadcrumbs", ArgumentSemantic.Strong)]
        SentryBreadcrumb[] Breadcrumbs { get; set; }

        // @property (nonatomic, strong) NSData * _Nonnull json;
        [Export("json", ArgumentSemantic.Strong)]
        NSData Json { get; set; }

        // -(instancetype _Nonnull)initWithLevel:(enum SentryLevel)level __attribute__((objc_designated_initializer));
        [Export("initWithLevel:")]
        [DesignatedInitializer]
        IntPtr Constructor(SentryLevel level);

        // -(instancetype _Nonnull)initWithJSON:(NSData * _Nonnull)json;
        [Export("initWithJSON:")]
        IntPtr Constructor(NSData json);
    }

    // @interface SentryException : NSObject <SentrySerializable>
    [Internal]
    [BaseType(typeof(NSObject))]
    [DisableDefaultCtor]
    interface SentryException : ISentrySerializable
    {
        // @property (copy, nonatomic) NSString * _Nonnull value;
        [Export("value")]
        string Value { get; set; }

        // @property (copy, nonatomic) NSString * _Nonnull type;
        [Export("type")]
        string Type { get; set; }

        // @property (nonatomic, strong) SentryMechanism * _Nullable mechanism;
        [NullAllowed, Export("mechanism", ArgumentSemantic.Strong)]
        SentryMechanism Mechanism { get; set; }

        // @property (copy, nonatomic) NSString * _Nullable module;
        [NullAllowed, Export("module")]
        string Module { get; set; }

        // @property (copy, nonatomic) NSNumber * _Nullable userReported;
        [NullAllowed, Export("userReported", ArgumentSemantic.Copy)]
        NSNumber UserReported { get; set; }

        // @property (nonatomic, strong) SentryThread * _Nullable thread;
        [NullAllowed, Export("thread", ArgumentSemantic.Strong)]
        SentryThread Thread { get; set; }

        // -(instancetype _Nonnull)initWithValue:(NSString * _Nonnull)value type:(NSString * _Nonnull)type;
        [Export("initWithValue:type:")]
        IntPtr Constructor(string value, string type);
    }

    // @interface SentryFrame : NSObject <SentrySerializable>
    [Internal]
    [BaseType(typeof(NSObject))]
    interface SentryFrame : ISentrySerializable
    {
        // @property (copy, nonatomic) NSString * _Nullable symbolAddress;
        [NullAllowed, Export("symbolAddress")]
        string SymbolAddress { get; set; }

        // @property (copy, nonatomic) NSString * _Nullable fileName;
        [NullAllowed, Export("fileName")]
        string FileName { get; set; }

        // @property (copy, nonatomic) NSString * _Nullable function;
        [NullAllowed, Export("function")]
        string Function { get; set; }

        // @property (copy, nonatomic) NSString * _Nullable module;
        [NullAllowed, Export("module")]
        string Module { get; set; }

        // @property (copy, nonatomic) NSString * _Nullable package;
        [NullAllowed, Export("package")]
        string Package { get; set; }

        // @property (copy, nonatomic) NSString * _Nullable imageAddress;
        [NullAllowed, Export("imageAddress")]
        string ImageAddress { get; set; }

        // @property (copy, nonatomic) NSString * _Nullable platform;
        [NullAllowed, Export("platform")]
        string Platform { get; set; }

        // @property (copy, nonatomic) NSString * _Nullable instructionAddress;
        [NullAllowed, Export("instructionAddress")]
        string InstructionAddress { get; set; }

        // @property (copy, nonatomic) NSNumber * _Nullable lineNumber;
        [NullAllowed, Export("lineNumber", ArgumentSemantic.Copy)]
        NSNumber LineNumber { get; set; }

        // @property (copy, nonatomic) NSNumber * _Nullable columnNumber;
        [NullAllowed, Export("columnNumber", ArgumentSemantic.Copy)]
        NSNumber ColumnNumber { get; set; }

        // @property (copy, nonatomic) NSNumber * _Nullable inApp;
        [NullAllowed, Export("inApp", ArgumentSemantic.Copy)]
        NSNumber InApp { get; set; }
    }

    // @interface SentryOptions : NSObject
    [Internal]
    [BaseType(typeof(NSObject))]
    interface SentryOptions
    {
        // -(instancetype _Nullable)initWithDict:(NSDictionary<NSString *,id> * _Nonnull)options didFailWithError:(NSError * _Nullable * _Nullable)error;
        [Export("initWithDict:didFailWithError:")]
        IntPtr Constructor(NSDictionary<NSString, NSObject> options, [NullAllowed] out NSError error);

        // @property (nonatomic, strong) NSString * _Nullable dsn;
        [NullAllowed, Export("dsn", ArgumentSemantic.Strong)]
        string Dsn { get; set; }

        // @property (nonatomic, strong) SentryDsn * _Nullable parsedDsn;
        [NullAllowed, Export("parsedDsn", ArgumentSemantic.Strong)]
        SentryDsn ParsedDsn { get; set; }

        // @property (assign, nonatomic) BOOL debug;
        [Export("debug")]
        bool Debug { get; set; }

        // @property (assign, nonatomic) SentryLogLevel logLevel;
        [Export("logLevel", ArgumentSemantic.Assign)]
        SentryLogLevel LogLevel { get; set; }

        // @property (copy, nonatomic) NSString * _Nullable releaseName;
        [NullAllowed, Export("releaseName")]
        string ReleaseName { get; set; }

        // @property (copy, nonatomic) NSString * _Nullable dist;
        [NullAllowed, Export("dist")]
        string Dist { get; set; }

        // @property (copy, nonatomic) NSString * _Nullable environment;
        [NullAllowed, Export("environment")]
        string Environment { get; set; }

        // @property (assign, nonatomic) NSUInteger maxBreadcrumbs;
        [Export("maxBreadcrumbs")]
        nuint MaxBreadcrumbs { get; set; }

        // @property (copy, nonatomic) SentryBeforeSendEventCallback _Nullable beforeSend;
        [NullAllowed, Export("beforeSend", ArgumentSemantic.Copy)]
        SentryBeforeSendEventCallback BeforeSend { get; set; }

        // @property (copy, nonatomic) SentryBeforeBreadcrumbCallback _Nullable beforeBreadcrumb;
        [NullAllowed, Export("beforeBreadcrumb", ArgumentSemantic.Copy)]
        SentryBeforeBreadcrumbCallback BeforeBreadcrumb { get; set; }

        // @property (copy, nonatomic) NSArray<NSString *> * _Nullable integrations;
        [NullAllowed, Export("integrations", ArgumentSemantic.Copy)]
        string[] Integrations { get; set; }

        // +(NSArray<NSString *> * _Nonnull)defaultIntegrations;
        //[Static]
        //[Export("defaultIntegrations")]
        //[Verify(MethodToProperty)]
        //string[] DefaultIntegrations { get; }

        // @property (copy, nonatomic) NSNumber * _Nullable sampleRate;
        [NullAllowed, Export("sampleRate", ArgumentSemantic.Copy)]
        NSNumber SampleRate { get; set; }

        // @property (assign, nonatomic) BOOL enableAutoSessionTracking;
        [Export("enableAutoSessionTracking")]
        bool EnableAutoSessionTracking { get; set; }

        // @property (assign, nonatomic) NSUInteger sessionTrackingIntervalMillis;
        [Export("sessionTrackingIntervalMillis")]
        nuint SessionTrackingIntervalMillis { get; set; }

        // @property (assign, nonatomic) BOOL attachStacktrace;
        [Export("attachStacktrace")]
        bool AttachStacktrace { get; set; }
    }

    // @protocol SentryIntegrationProtocol <NSObject>
    /*
      Check whether adding [Model] to this declaration is appropriate.
      [Model] is used to generate a C# class that implements this protocol,
      and might be useful for protocols that consumers are supposed to implement,
      since consumers can subclass the generated class instead of implementing
      the generated interface. If consumers are not supposed to implement this
      protocol, then [Model] is redundant and will generate code that will never
      be used.
    */
    [Internal]
    [Protocol]
    [BaseType(typeof(NSObject))]
    interface SentryIntegrationProtocol
    {
        // @required -(void)installWithOptions:(SentryOptions * _Nonnull)options;
        [Abstract]
        [Export("installWithOptions:")]
        void InstallWithOptions(SentryOptions options);
    }

    // @interface SentryHub : NSObject
    [Internal]
    [BaseType(typeof(NSObject))]
    [DisableDefaultCtor]
    interface SentryHub
    {
        // -(instancetype _Nonnull)initWithClient:(SentryClient * _Nullable)client andScope:(SentryScope * _Nullable)scope;
        [Export("initWithClient:andScope:")]
        IntPtr Constructor([NullAllowed] SentryClient client, [NullAllowed] SentryScope scope);

        // @property (readonly, nonatomic, strong) SentrySession * _Nullable session;
        [NullAllowed, Export("session", ArgumentSemantic.Strong)]
        SentrySession Session { get; }

        // -(void)startSession;
        [Export("startSession")]
        void StartSession();

        // -(void)endSessionWithTimestamp:(NSDate * _Nonnull)timestamp;
        [Export("endSessionWithTimestamp:")]
        void EndSessionWithTimestamp(NSDate timestamp);

        // -(void)closeCachedSessionWithTimestamp:(NSDate * _Nullable)timestamp;
        [Export("closeCachedSessionWithTimestamp:")]
        void CloseCachedSessionWithTimestamp([NullAllowed] NSDate timestamp);

        //// @property (nonatomic, strong) NSMutableArray<NSObject<SentryIntegrationProtocol> *> * _Nonnull installedIntegrations;
        //[Export("installedIntegrations", ArgumentSemantic.Strong)]
        //NSMutableArray<SentryIntegrationProtocol> InstalledIntegrations { get; set; }

        // -(SentryId * _Nonnull)captureEvent:(SentryEvent * _Nonnull)event __attribute__((swift_name("capture(event:)")));
        [Export("captureEvent:")]
        SentryId CaptureEvent(SentryEvent @event);

        // -(SentryId * _Nonnull)captureEvent:(SentryEvent * _Nonnull)event withScope:(SentryScope * _Nonnull)scope __attribute__((swift_name("capture(event:scope:)")));
        [Export("captureEvent:withScope:")]
        SentryId CaptureEvent(SentryEvent @event, SentryScope scope);

        // -(SentryId * _Nonnull)captureError:(NSError * _Nonnull)error __attribute__((swift_name("capture(error:)")));
        [Export("captureError:")]
        SentryId CaptureError(NSError error);

        // -(SentryId * _Nonnull)captureError:(NSError * _Nonnull)error withScope:(SentryScope * _Nonnull)scope __attribute__((swift_name("capture(error:scope:)")));
        [Export("captureError:withScope:")]
        SentryId CaptureError(NSError error, SentryScope scope);

        // -(SentryId * _Nonnull)captureException:(NSException * _Nonnull)exception __attribute__((swift_name("capture(exception:)")));
        [Export("captureException:")]
        SentryId CaptureException(NSException exception);

        // -(SentryId * _Nonnull)captureException:(NSException * _Nonnull)exception withScope:(SentryScope * _Nonnull)scope __attribute__((swift_name("capture(exception:scope:)")));
        [Export("captureException:withScope:")]
        SentryId CaptureException(NSException exception, SentryScope scope);

        // -(SentryId * _Nonnull)captureMessage:(NSString * _Nonnull)message __attribute__((swift_name("capture(message:)")));
        [Export("captureMessage:")]
        SentryId CaptureMessage(string message);

        // -(SentryId * _Nonnull)captureMessage:(NSString * _Nonnull)message withScope:(SentryScope * _Nonnull)scope __attribute__((swift_name("capture(message:scope:)")));
        [Export("captureMessage:withScope:")]
        SentryId CaptureMessage(string message, SentryScope scope);

        // -(void)configureScope:(void (^ _Nonnull)(SentryScope * _Nonnull))callback;
        [Export("configureScope:")]
        void ConfigureScope(Action<SentryScope> callback);

        // -(void)addBreadcrumb:(SentryBreadcrumb * _Nonnull)crumb;
        [Export("addBreadcrumb:")]
        void AddBreadcrumb(SentryBreadcrumb crumb);

        // -(SentryClient * _Nullable)getClient;
        [NullAllowed, Export("getClient")]
        //[Verify(MethodToProperty)]
        SentryClient Client { get; }

        // -(SentryScope * _Nonnull)getScope;
        [Export("getScope")]
        //[Verify(MethodToProperty)]
        SentryScope Scope { get; }

        // -(void)bindClient:(SentryClient * _Nullable)client;
        [Export("bindClient:")]
        void BindClient([NullAllowed] SentryClient client);

        // -(id _Nullable)getIntegration:(NSString * _Nonnull)integrationName;
        [Export("getIntegration:")]
        [return: NullAllowed]
        NSObject GetIntegration(string integrationName);

        // -(BOOL)isIntegrationInstalled:(Class _Nonnull)integrationClass;
        [Export("isIntegrationInstalled:")]
        bool IsIntegrationInstalled(Class integrationClass);

        // -(void)setUser:(SentryUser * _Nullable)user;
        [Export("setUser:")]
        void SetUser([NullAllowed] SentryUser user);
    }

    // @interface SentryId : NSObject
    [Internal]
    [BaseType(typeof(NSObject))]
    interface SentryId
    {
        // -(instancetype _Nonnull)initWithUUID:(NSUUID * _Nonnull)uuid;
        [Export("initWithUUID:")]
        IntPtr Constructor(NSUuid uuid);

        // -(instancetype _Nonnull)initWithUUIDString:(NSString * _Nonnull)string;
        [Export("initWithUUIDString:")]
        IntPtr Constructor(string @string);

        // @property (readonly, copy) NSString * _Nonnull sentryIdString;
        [Export("sentryIdString")]
        string SentryIdString { get; }

        // @property (readonly, nonatomic, strong, class) SentryId * _Nonnull empty;
        [Static]
        [Export("empty", ArgumentSemantic.Strong)]
        SentryId Empty { get; }
    }

    // @interface SentryMechanism : NSObject <SentrySerializable>
    [BaseType(typeof(NSObject))]
    [DisableDefaultCtor]
    interface SentryMechanism : ISentrySerializable
    {
        // @property (copy, nonatomic) NSString * _Nonnull type;
        [Export("type")]
        string Type { get; set; }

        // @property (copy, nonatomic) NSString * _Nullable desc;
        [NullAllowed, Export("desc")]
        string Desc { get; set; }

        // @property (nonatomic, strong) NSDictionary<NSString *,id> * _Nullable data;
        [NullAllowed, Export("data", ArgumentSemantic.Strong)]
        NSDictionary<NSString, NSObject> Data { get; set; }

        // @property (copy, nonatomic) NSNumber * _Nullable handled;
        [NullAllowed, Export("handled", ArgumentSemantic.Copy)]
        NSNumber Handled { get; set; }

        // @property (copy, nonatomic) NSString * _Nullable helpLink;
        [NullAllowed, Export("helpLink")]
        string HelpLink { get; set; }

        // @property (nonatomic, strong) NSDictionary<NSString *,NSString *> * _Nullable meta;
        [NullAllowed, Export("meta", ArgumentSemantic.Strong)]
        NSDictionary<NSString, NSString> Meta { get; set; }

        // -(instancetype _Nonnull)initWithType:(NSString * _Nonnull)type;
        [Export("initWithType:")]
        IntPtr Constructor(string type);
    }

    // @interface SentryMessage : NSObject <SentrySerializable>
    [Internal]
    [BaseType(typeof(NSObject))]
    [DisableDefaultCtor]
    interface SentryMessage : ISentrySerializable
    {
        // -(instancetype _Nonnull)initWithFormatted:(NSString * _Nonnull)formatted;
        [Export("initWithFormatted:")]
        IntPtr Constructor(string formatted);

        // @property (readonly, copy, nonatomic) NSString * _Nonnull formatted;
        [Export("formatted")]
        string Formatted { get; }

        // @property (copy, nonatomic) NSString * _Nullable message;
        [NullAllowed, Export("message")]
        string Message { get; set; }

        // @property (nonatomic, strong) NSArray<NSString *> * _Nullable params;
        [NullAllowed, Export("params", ArgumentSemantic.Strong)]
        string[] Params { get; set; }
    }

    // @interface SentrySDK : NSObject
    [Internal]
    [BaseType(typeof(NSObject))]
    [DisableDefaultCtor]
    interface SentrySDK
    {
        // +(SentryHub * _Nonnull)currentHub;
        // +(void)setCurrentHub:(SentryHub * _Nonnull)hub;
        [Static]
        [Export("currentHub")]
        //[Verify(MethodToProperty)]
        SentryHub CurrentHub { get; set; }

        // +(void)crash;
        [Static]
        [Export("crash")]
        void Crash();

        // +(void)startWithOptions:(NSDictionary<NSString *,id> * _Nonnull)optionsDict __attribute__((swift_name("start(options:)")));
        [Static]
        [Export("startWithOptions:")]
        void StartWithOptions(NSDictionary<NSString, NSObject> optionsDict);

        // +(void)startWithOptionsObject:(SentryOptions * _Nonnull)options __attribute__((swift_name("start(options:)")));
        [Static]
        [Export("startWithOptionsObject:")]
        void StartWithOptionsObject(SentryOptions options);

        // +(void)startWithConfigureOptions:(void (^ _Nonnull)(SentryOptions * _Nonnull))configureOptions __attribute__((swift_name("start(configureOptions:)")));
        [Static]
        [Export("startWithConfigureOptions:")]
        void StartWithConfigureOptions(Action<SentryOptions> configureOptions);

        // +(SentryId * _Nonnull)captureEvent:(SentryEvent * _Nonnull)event __attribute__((swift_name("capture(event:)")));
        [Static]
        [Export("captureEvent:")]
        SentryId CaptureEvent(SentryEvent @event);

        // +(SentryId * _Nonnull)captureEvent:(SentryEvent * _Nonnull)event withScope:(SentryScope * _Nonnull)scope __attribute__((swift_name("capture(event:scope:)")));
        [Static]
        [Export("captureEvent:withScope:")]
        SentryId CaptureEvent(SentryEvent @event, SentryScope scope);

        // +(SentryId * _Nonnull)captureEvent:(SentryEvent * _Nonnull)event withScopeBlock:(void (^ _Nonnull)(SentryScope * _Nonnull))block __attribute__((swift_name("capture(event:block:)")));
        [Static]
        [Export("captureEvent:withScopeBlock:")]
        SentryId CaptureEvent(SentryEvent @event, Action<SentryScope> block);

        // +(SentryId * _Nonnull)captureError:(NSError * _Nonnull)error __attribute__((swift_name("capture(error:)")));
        [Static]
        [Export("captureError:")]
        SentryId CaptureError(NSError error);

        // +(SentryId * _Nonnull)captureError:(NSError * _Nonnull)error withScope:(SentryScope * _Nonnull)scope __attribute__((swift_name("capture(error:scope:)")));
        [Static]
        [Export("captureError:withScope:")]
        SentryId CaptureError(NSError error, SentryScope scope);

        // +(SentryId * _Nonnull)captureError:(NSError * _Nonnull)error withScopeBlock:(void (^ _Nonnull)(SentryScope * _Nonnull))block __attribute__((swift_name("capture(error:block:)")));
        [Static]
        [Export("captureError:withScopeBlock:")]
        SentryId CaptureError(NSError error, Action<SentryScope> block);

        // +(SentryId * _Nonnull)captureException:(NSException * _Nonnull)exception __attribute__((swift_name("capture(exception:)")));
        [Static]
        [Export("captureException:")]
        SentryId CaptureException(NSException exception);

        // +(SentryId * _Nonnull)captureException:(NSException * _Nonnull)exception withScope:(SentryScope * _Nonnull)scope __attribute__((swift_name("capture(exception:scope:)")));
        [Static]
        [Export("captureException:withScope:")]
        SentryId CaptureException(NSException exception, SentryScope scope);

        // +(SentryId * _Nonnull)captureException:(NSException * _Nonnull)exception withScopeBlock:(void (^ _Nonnull)(SentryScope * _Nonnull))block __attribute__((swift_name("capture(exception:block:)")));
        [Static]
        [Export("captureException:withScopeBlock:")]
        SentryId CaptureException(NSException exception, Action<SentryScope> block);

        // +(SentryId * _Nonnull)captureMessage:(NSString * _Nonnull)message __attribute__((swift_name("capture(message:)")));
        [Static]
        [Export("captureMessage:")]
        SentryId CaptureMessage(string message);

        // +(SentryId * _Nonnull)captureMessage:(NSString * _Nonnull)message withScope:(SentryScope * _Nonnull)scope __attribute__((swift_name("capture(message:scope:)")));
        [Static]
        [Export("captureMessage:withScope:")]
        SentryId CaptureMessage(string message, SentryScope scope);

        // +(SentryId * _Nonnull)captureMessage:(NSString * _Nonnull)message withScopeBlock:(void (^ _Nonnull)(SentryScope * _Nonnull))block __attribute__((swift_name("capture(message:block:)")));
        [Static]
        [Export("captureMessage:withScopeBlock:")]
        SentryId CaptureMessage(string message, Action<SentryScope> block);

        // +(void)addBreadcrumb:(SentryBreadcrumb * _Nonnull)crumb __attribute__((swift_name("addBreadcrumb(crumb:)")));
        [Static]
        [Export("addBreadcrumb:")]
        void AddBreadcrumb(SentryBreadcrumb crumb);

        // +(void)configureScope:(void (^ _Nonnull)(SentryScope * _Nonnull))callback;
        [Static]
        [Export("configureScope:")]
        void ConfigureScope(Action<SentryScope> callback);

        // @property (nonatomic, class) SentryLogLevel logLevel;
        [Static]
        [Export("logLevel", ArgumentSemantic.Assign)]
        SentryLogLevel LogLevel { get; set; }

        // @property (readonly, nonatomic, class) BOOL crashedLastRun;
        [Static]
        [Export("crashedLastRun")]
        bool CrashedLastRun { get; }

        // +(void)setUser:(SentryUser * _Nullable)user;
        [Static]
        [Export("setUser:")]
        void SetUser([NullAllowed] SentryUser user);
    }

    // @interface SentryScope : NSObject <SentrySerializable>
    [Internal]
    [BaseType(typeof(NSObject))]
    interface SentryScope : ISentrySerializable
    {
        // -(instancetype _Nonnull)initWithMaxBreadcrumbs:(NSInteger)maxBreadcrumbs __attribute__((objc_designated_initializer));
        [Export("initWithMaxBreadcrumbs:")]
        [DesignatedInitializer]
        IntPtr Constructor(nint maxBreadcrumbs);

        // -(instancetype _Nonnull)initWithScope:(SentryScope * _Nonnull)scope;
        [Export("initWithScope:")]
        IntPtr Constructor(SentryScope scope);

        // -(void)setUser:(SentryUser * _Nullable)user;
        [Export("setUser:")]
        void SetUser([NullAllowed] SentryUser user);

        // -(void)setTagValue:(NSString * _Nonnull)value forKey:(NSString * _Nonnull)key __attribute__((swift_name("setTag(value:key:)")));
        [Export("setTagValue:forKey:")]
        void SetTagValue(string value, string key);

        // -(void)removeTagForKey:(NSString * _Nonnull)key __attribute__((swift_name("removeTag(key:)")));
        [Export("removeTagForKey:")]
        void RemoveTagForKey(string key);

        // -(void)setTags:(NSDictionary<NSString *,NSString *> * _Nullable)tags;
        [Export("setTags:")]
        void SetTags([NullAllowed] NSDictionary<NSString, NSString> tags);

        // -(void)setExtras:(NSDictionary<NSString *,id> * _Nullable)extras;
        [Export("setExtras:")]
        void SetExtras([NullAllowed] NSDictionary<NSString, NSObject> extras);

        // -(void)setExtraValue:(id _Nullable)value forKey:(NSString * _Nonnull)key __attribute__((swift_name("setExtra(value:key:)")));
        [Export("setExtraValue:forKey:")]
        void SetExtraValue([NullAllowed] NSObject value, string key);

        // -(void)removeExtraForKey:(NSString * _Nonnull)key __attribute__((swift_name("removeExtra(key:)")));
        [Export("removeExtraForKey:")]
        void RemoveExtraForKey(string key);

        // -(void)setDist:(NSString * _Nullable)dist;
        [Export("setDist:")]
        void SetDist([NullAllowed] string dist);

        // -(void)setEnvironment:(NSString * _Nullable)environment;
        [Export("setEnvironment:")]
        void SetEnvironment([NullAllowed] string environment);

        // -(void)setFingerprint:(NSArray<NSString *> * _Nullable)fingerprint;
        [Export("setFingerprint:")]
        void SetFingerprint([NullAllowed] string[] fingerprint);

        // -(void)setLevel:(enum SentryLevel)level;
        [Export("setLevel:")]
        void SetLevel(SentryLevel level);

        // -(void)addBreadcrumb:(SentryBreadcrumb * _Nonnull)crumb;
        [Export("addBreadcrumb:")]
        void AddBreadcrumb(SentryBreadcrumb crumb);

        // -(void)clearBreadcrumbs;
        [Export("clearBreadcrumbs")]
        void ClearBreadcrumbs();

        // -(NSDictionary<NSString *,id> * _Nonnull)serialize;
        [Export("serialize")]
        //[Verify(MethodToProperty)]
        NSDictionary<NSString, NSObject> Serialize { get; }

        // -(SentryEvent * _Nullable)applyToEvent:(SentryEvent * _Nonnull)event maxBreadcrumb:(NSUInteger)maxBreadcrumbs;
        [Export("applyToEvent:maxBreadcrumb:")]
        [return: NullAllowed]
        SentryEvent ApplyToEvent(SentryEvent @event, nuint maxBreadcrumbs);

        // -(void)applyToSession:(SentrySession * _Nonnull)session;
        [Export("applyToSession:")]
        void ApplyToSession(SentrySession session);

        // -(void)setContextValue:(NSDictionary<NSString *,id> * _Nonnull)value forKey:(NSString * _Nonnull)key __attribute__((swift_name("setContext(value:key:)")));
        [Export("setContextValue:forKey:")]
        void SetContextValue(NSDictionary<NSString, NSObject> value, string key);

        // -(void)removeContextForKey:(NSString * _Nonnull)key __attribute__((swift_name("removeContext(key:)")));
        [Export("removeContextForKey:")]
        void RemoveContextForKey(string key);

        // -(void)clear;
        [Export("clear")]
        void Clear();

        //// -(BOOL)isEqual:(id _Nullable)other;
        //[Export("isEqual:")]
        //bool IsEqual([NullAllowed] NSObject other);

        //// -(BOOL)isEqualToScope:(SentryScope * _Nonnull)scope;
        //[Export("isEqualToScope:")]
        //bool IsEqualToScope(SentryScope scope);

        //// -(NSUInteger)hash;
        //[Export("hash")]
        //[Verify(MethodToProperty)]
        //nuint Hash { get; }
    }

    // @interface SentrySession : NSObject <NSCopying>
    [Internal]
    [BaseType(typeof(NSObject))]
    [DisableDefaultCtor]
    interface SentrySession
    //: INSCopying // error CS8767: Nullability of reference types in type of parameter 'zone' of 'NSObject SentrySession.Copy(NSZone zone)'
    {
        // -(instancetype _Nonnull)initWithReleaseName:(NSString * _Nonnull)releaseName;
        [Export("initWithReleaseName:")]
        IntPtr Constructor(string releaseName);

        // -(instancetype _Nonnull)initWithJSONObject:(NSDictionary * _Nonnull)jsonObject;
        [Export("initWithJSONObject:")]
        IntPtr Constructor(NSDictionary jsonObject);

        // -(void)endSessionExitedWithTimestamp:(NSDate * _Nonnull)timestamp;
        [Export("endSessionExitedWithTimestamp:")]
        void EndSessionExitedWithTimestamp(NSDate timestamp);

        // -(void)endSessionCrashedWithTimestamp:(NSDate * _Nonnull)timestamp;
        [Export("endSessionCrashedWithTimestamp:")]
        void EndSessionCrashedWithTimestamp(NSDate timestamp);

        // -(void)endSessionAbnormalWithTimestamp:(NSDate * _Nonnull)timestamp;
        [Export("endSessionAbnormalWithTimestamp:")]
        void EndSessionAbnormalWithTimestamp(NSDate timestamp);

        // -(void)incrementErrors;
        [Export("incrementErrors")]
        void IncrementErrors();

        // @property (readonly, nonatomic, strong) NSUUID * _Nonnull sessionId;
        [Export("sessionId", ArgumentSemantic.Strong)]
        NSUuid SessionId { get; }

        // @property (readonly, nonatomic, strong) NSDate * _Nonnull started;
        [Export("started", ArgumentSemantic.Strong)]
        NSDate Started { get; }

        // @property (readonly, nonatomic) enum SentrySessionStatus status;
        [Export("status")]
        SentrySessionStatus Status { get; }

        // @property (readonly, nonatomic) NSUInteger errors;
        [Export("errors")]
        nuint Errors { get; }

        // @property (readonly, nonatomic) NSUInteger sequence;
        [Export("sequence")]
        nuint Sequence { get; }

        // @property (readonly, nonatomic, strong) NSString * _Nonnull distinctId;
        [Export("distinctId", ArgumentSemantic.Strong)]
        string DistinctId { get; }

        // @property (readonly, copy, nonatomic) NSNumber * _Nullable flagInit;
        [NullAllowed, Export("flagInit", ArgumentSemantic.Copy)]
        NSNumber FlagInit { get; }

        // @property (readonly, nonatomic, strong) NSDate * _Nullable timestamp;
        [NullAllowed, Export("timestamp", ArgumentSemantic.Strong)]
        NSDate Timestamp { get; }

        // @property (readonly, nonatomic, strong) NSNumber * _Nullable duration;
        [NullAllowed, Export("duration", ArgumentSemantic.Strong)]
        NSNumber Duration { get; }

        // @property (readonly, copy, nonatomic) NSString * _Nullable releaseName;
        [NullAllowed, Export("releaseName")]
        string ReleaseName { get; }

        // @property (copy, nonatomic) NSString * _Nullable environment;
        [NullAllowed, Export("environment")]
        string Environment { get; set; }

        // @property (copy, nonatomic) SentryUser * _Nullable user;
        [NullAllowed, Export("user", ArgumentSemantic.Copy)]
        SentryUser User { get; set; }

        // -(NSDictionary<NSString *,id> * _Nonnull)serialize;
        [Export("serialize")]
        //[Verify(MethodToProperty)]
        NSDictionary<NSString, NSObject> Serialize { get; }
    }

    // @interface SentryStacktrace : NSObject <SentrySerializable>
    [Internal]
    [BaseType(typeof(NSObject))]
    [DisableDefaultCtor]
    interface SentryStacktrace : ISentrySerializable
    {
        // @property (nonatomic, strong) NSArray<SentryFrame *> * _Nonnull frames;
        [Export("frames", ArgumentSemantic.Strong)]
        SentryFrame[] Frames { get; set; }

        // @property (nonatomic, strong) NSDictionary<NSString *,NSString *> * _Nonnull registers;
        [Export("registers", ArgumentSemantic.Strong)]
        NSDictionary<NSString, NSString> Registers { get; set; }

        // -(instancetype _Nonnull)initWithFrames:(NSArray<SentryFrame *> * _Nonnull)frames registers:(NSDictionary<NSString *,NSString *> * _Nonnull)registers;
        [Export("initWithFrames:registers:")]
        IntPtr Constructor(SentryFrame[] frames, NSDictionary<NSString, NSString> registers);

        // -(void)fixDuplicateFrames;
        [Export("fixDuplicateFrames")]
        void FixDuplicateFrames();
    }

    // @interface SentryThread : NSObject <SentrySerializable>
    [Internal]
    [BaseType(typeof(NSObject))]
    [DisableDefaultCtor]
    interface SentryThread : ISentrySerializable
    {
        // @property (copy, nonatomic) NSNumber * _Nonnull threadId;
        [Export("threadId", ArgumentSemantic.Copy)]
        NSNumber ThreadId { get; set; }

        // @property (copy, nonatomic) NSString * _Nullable name;
        [NullAllowed, Export("name")]
        string Name { get; set; }

        // @property (nonatomic, strong) SentryStacktrace * _Nullable stacktrace;
        [NullAllowed, Export("stacktrace", ArgumentSemantic.Strong)]
        SentryStacktrace Stacktrace { get; set; }

        // @property (copy, nonatomic) NSNumber * _Nullable crashed;
        [NullAllowed, Export("crashed", ArgumentSemantic.Copy)]
        NSNumber Crashed { get; set; }

        // @property (copy, nonatomic) NSNumber * _Nullable current;
        [NullAllowed, Export("current", ArgumentSemantic.Copy)]
        NSNumber Current { get; set; }

        // -(instancetype _Nonnull)initWithThreadId:(NSNumber * _Nonnull)threadId;
        [Export("initWithThreadId:")]
        IntPtr Constructor(NSNumber threadId);
    }

    // @interface SentryUser : NSObject <SentrySerializable, NSCopying>
    [Internal]
    [BaseType(typeof(NSObject))]
    interface SentryUser : ISentrySerializable
    //, INSCopying // error CS8767: Nullability of reference types in type of parameter 'zone' of 'NSObject SentryUser.Copy(NSZone zone)'
    {
        // @property (copy, nonatomic) NSString * _Nonnull userId;
        [Export("userId")]
        string UserId { get; set; }

        // @property (copy, nonatomic) NSString * _Nullable email;
        [NullAllowed, Export("email")]
        string Email { get; set; }

        // @property (copy, nonatomic) NSString * _Nullable username;
        [NullAllowed, Export("username")]
        string Username { get; set; }

        // @property (copy, nonatomic) NSString * _Nullable ipAddress;
        [NullAllowed, Export("ipAddress")]
        string IpAddress { get; set; }

        // @property (nonatomic, strong) NSDictionary<NSString *,id> * _Nullable data;
        [NullAllowed, Export("data", ArgumentSemantic.Strong)]
        NSDictionary<NSString, NSObject> Data { get; set; }

        // -(instancetype _Nonnull)initWithUserId:(NSString * _Nonnull)userId;
        [Export("initWithUserId:")]
        IntPtr Constructor(string userId);

        //// -(BOOL)isEqual:(id _Nullable)other;
        //[Export("isEqual:")]
        //bool IsEqual([NullAllowed] NSObject other);

        //// -(BOOL)isEqualToUser:(SentryUser * _Nonnull)user;
        //[Export("isEqualToUser:")]
        //bool IsEqualToUser(SentryUser user);

        //// -(NSUInteger)hash;
        //[Export("hash")]
        //[Verify(MethodToProperty)]
        //nuint Hash { get; }
    }

    //[Static]
    //[Verify(ConstantsInterfaceAssociation)]
    //partial interface Constants
    //{
    //	// extern double SentryVersionNumber;
    //	[Field("SentryVersionNumber", "__Internal")]
    //	double SentryVersionNumber { get; }

    //	// extern const unsigned char [] SentryVersionString;
    //	[Field("SentryVersionString", "__Internal")]
    //	byte[] SentryVersionString { get; }
    //}
}
