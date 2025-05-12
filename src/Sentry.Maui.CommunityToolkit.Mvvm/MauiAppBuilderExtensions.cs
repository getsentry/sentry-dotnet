using Sentry.Maui.CommunityToolkit.Mvvm;

namespace Sentry.Maui;

/// <summary>
/// Methods to hook into MAUI and CommunityToolkit.Mvvm
/// </summary>
public static class MauiAppBuilderExtensions
{
    /// <summary>
    /// Installs necessary services to auto-instrument CommunityToolkit.Mvvm commands
    /// </summary>
    /// <param name="builder">The MauiAppBuilder</param>
    /// <returns>The MauiAppBuilder</returns>
    public static MauiAppBuilder UseSentryCommunityToolkitIntegration(this MauiAppBuilder builder)
    {
        builder.Services.UseSentryCommunityToolkitIntegration();
        return builder;
    }


    /// <summary>
    /// Installs necessary services to auto-instrument CommunityToolkit.Mvvm commands
    /// </summary>
    /// <param name="services">The Service Collection</param>
    /// <returns>The Service Collection</returns>
    public static IServiceCollection UseSentryCommunityToolkitIntegration(this IServiceCollection services)
    {
        services.AddSingleton<IMauiElementEventBinder, CtMvvmMauiElementEventBinder>();
        return services;
    }
}
