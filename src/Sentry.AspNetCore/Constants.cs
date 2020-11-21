namespace Sentry.AspNetCore
{
    internal static class Constants
    {
        // See: https://github.com/getsentry/sentry-release-registry
        public const string SdkName = "sentry.dotnet.aspnetcore";

        public static string ASPNETCoreProductionEnvironmentName =>
#if NETSTANDARD2_0
             "Production";
#else
             Microsoft.Extensions.Hosting.Environments.Production;
#endif

        public static string ASPNETCoreDevelopmentEnvironmentName =>
#if NETSTANDARD2_0
             "Development";
#else
             Microsoft.Extensions.Hosting.Environments.Development;
#endif
    }
}
