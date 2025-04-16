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
