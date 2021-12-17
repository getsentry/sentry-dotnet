using Microsoft.Extensions.DependencyInjection;

namespace Sentry.Serilog.Tests.Utils.Extensions;

internal static class DependencyContainerExtensions
{
    public static void EnableValidation(this ServiceProviderOptions options, bool enable = true)
    {
        options.ValidateScopes = enable;

#if !NET461 && !NETCOREAPP2_1
        options.ValidateOnBuild = enable;
#endif
    }
}
