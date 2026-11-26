using Microsoft.Extensions.DependencyInjection;

namespace Sentry.AspNetCore.TestUtils;

public static class DependencyContainerExtensions
{
    public static void EnableValidation(this ServiceProviderOptions options, bool enable = true)
    {
        options.ValidateScopes = enable;

        options.ValidateOnBuild = enable;
    }
}
