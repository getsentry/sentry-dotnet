using System.Reflection;

namespace Sentry.Maui;

internal static class Constants
{
    // See: https://github.com/getsentry/sentry-release-registry
    public const string SdkName = "sentry.dotnet.maui";

    public static string SdkVersion = typeof(SentryMauiOptions).Assembly
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;
}
