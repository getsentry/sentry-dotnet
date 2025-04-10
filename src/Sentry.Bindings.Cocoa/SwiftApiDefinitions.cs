/*
 * This file defines Xamarin iOS API contracts for the members we need from Sentry-Swift.h
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

    [NullAllowed, Export("configureForm", ArgumentSemantic.Copy)]
    Action<SentryUserFeedbackFormConfiguration> ConfigureForm { get; set; }

    [Export("formConfig", ArgumentSemantic.Strong)]
    SentryUserFeedbackFormConfiguration FormConfig { get; set; }

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

    [NullAllowed, Export("configureTheme", ArgumentSemantic.Copy)]
    Action<SentryUserFeedbackThemeConfiguration> ConfigureTheme { get; set; }

    [Export("theme", ArgumentSemantic.Strong)]
    SentryUserFeedbackThemeConfiguration Theme { get; set; }

    [NullAllowed, Export("configureDarkTheme", ArgumentSemantic.Copy)]
    Action<SentryUserFeedbackThemeConfiguration> ConfigureDarkTheme { get; set; }

    [Export("darkTheme", ArgumentSemantic.Strong)]
    SentryUserFeedbackThemeConfiguration DarkTheme { get; set; }

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

[BaseType(typeof(NSObject), Name = "_TtC6Sentry37SentryUserFeedbackThemeConfiguration")]
[DisableDefaultCtor]
[Internal]
interface SentryUserFeedbackThemeConfiguration
{
    [Export("backgroundColor", ArgumentSemantic.Strong)]
    UIColor BackgroundColor { get; set; }

    [Export("textColor", ArgumentSemantic.Strong)]
    UIColor TextColor { get; set; }

    [Export("buttonColor", ArgumentSemantic.Strong)]
    UIColor ButtonColor { get; set; }

    [Export("buttonTextColor", ArgumentSemantic.Strong)]
    UIColor ButtonTextColor { get; set; }

    [Export("init")]
    [DesignatedInitializer]
    IntPtr Constructor();
}

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


[BaseType(typeof(NSObject), Name = "_TtC6Sentry37SentryUserFeedbackFormConfiguration")]
[DisableDefaultCtor]
[Internal]
interface SentryUserFeedbackFormConfiguration
{
    [Export("title", ArgumentSemantic.Copy)]
    string Title { get; set; }

    [Export("subtitle", ArgumentSemantic.Copy)]
    string Subtitle { get; set; }

    [Export("submitButtonTitle", ArgumentSemantic.Copy)]
    string SubmitButtonTitle { get; set; }

    [Export("cancelButtonTitle", ArgumentSemantic.Copy)]
    string CancelButtonTitle { get; set; }

    [Export("thankYouMessage", ArgumentSemantic.Copy)]
    string ThankYouMessage { get; set; }

    [Export("init")]
    [DesignatedInitializer]
    IntPtr Constructor();
}
