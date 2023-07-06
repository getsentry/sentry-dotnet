using Microsoft.Extensions.DependencyInjection;

namespace Sentry.AspNetCore.TestUtils;

public static class DependencyContainerExtensions
{
    public static void EnableValidation(this ServiceProviderOptions options, bool enable = true)
    {
        options.ValidateScopes = enable;

#if NETCOREAPP3_1_OR_GREATER
        options.ValidateOnBuild = enable;
#endif
    }
}
