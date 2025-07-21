using Sentry.Maui.CommunityToolkit.Mvvm;

namespace Sentry.Maui;

/// <summary>
/// Methods to hook into MAUI and CommunityToolkit.Mvvm
/// </summary>
public static class SentryOptionsExtensions
{
    /// <summary>
    /// Automatically create traces for CommunityToolkit.Mvvm commands
    /// </summary>
    public static SentryMauiOptions AddCommunityToolkitIntegration(this SentryMauiOptions options)
    {
        options.InjectEventBinder<MauiCommunityToolkitMvvmEventsBinder>();
        return options;
    }
}
