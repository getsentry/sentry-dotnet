using Sentry.Maui.CommunityToolkit.Mvvm;

namespace Sentry.Maui;

/// <summary>
/// Methods to hook into MAUI and CommunityToolkit.Mvvm
/// </summary>
public static class SentryMauiAppBuilderExtensions
{
    /// <summary>
    /// Automatically create traces for CommunityToolkit.Mvvm commands
    /// </summary>
    public static SentryMauiAppBuilder AddCommunityToolkitIntegration(this SentryMauiAppBuilder builder)
        => builder.AddMauiElementBinder<MauiCommunityToolkitMvvmEventsBinder>();
}
