namespace Sentry.PlatformAbstractions
{
    internal static class FrameworkInstallationExtensions
    {
        internal static string? GetVersionNumber(this FrameworkInstallation frameworkInstall)
        => frameworkInstall?.ShortName != null ? frameworkInstall.ShortName :
           frameworkInstall?.Version != null ? $"v{frameworkInstall.Version}" :
           null;
    }
}
