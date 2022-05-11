using System.Reflection;
using Microsoft.Extensions.Hosting;

namespace Sentry.Extensions.Logging.Internal;

internal static class SentryAspNetCorePipelineHook
{
    // This class is used to hook into the ASP.NET Core pipeline via reflection.
    // It's required because we may have called a IHostBuilder.UseSentry extension method that
    // flows through Sentry.Extensions.Logging, even when we're in an ASP.NET Core application.
    // This can happen when hosting ASP.NET Core in a .NET Generic Host and calling UseSentry
    // without options that are specifically part of SentryAspNetCoreOptions.

    // However, if we ARE using a IHostBuilder.UseSentry extension method that
    // flows from Sentry.AspNetCore, then we can bypass this approach.

    private const string SentryAspNetCoreAssemblyFullName = "Sentry.AspNetCore, PublicKey=002400000480000094000000060200000024000052534131000400000100010059964a931488bcdbd14657f1ee0df32df61b57b3d14d7290c262c2cc9ddaad6ec984044f761f778e1823049d2cb996a4f58c8ea5b46c37891414cb34b4036b1c178d7b582289d2eef3c0f1e9b692c229a306831ee3d371d9e883f0eb0f74aeac6c6ab8c85fd1ec04b267e15a31532c4b4e2191f5980459db4dce0081f1050fb8";

    // This constant is used as a property on the builder to bypass reflection when we don't need it.
    internal const string WillRegisterSentryAspNetCoreStartupFilter = "WillRegisterSentryAspNetCoreStartupFilterDirectly";

    internal static bool TryRegisterAspNetCoreStartupFilterIfNeeded(this IHostBuilder builder)
    {
        if (builder.Properties.TryGetValue(WillRegisterSentryAspNetCoreStartupFilter, out var value) && value is true)
        {
            // We came from a UseSentry method in Sentry.AspNetCore.  We don't need to register the filter ourselves.
            // Still return true to indicate that the filter will indeed be loaded.
            return true;
        }

        var assembly = TryGetSentryAspNetCoreAssembly();
        if (assembly == null)
        {
            // We're not using Sentry.AspNetCore in the application.
            return false;
        }

        // Get the startup filter and its interface through reflection.
        var implementationType = assembly.GetType("Sentry.AspNetCore.SentryStartupFilter");
        var serviceType = implementationType?.GetInterfaces().FirstOrDefault(x => x.Name == "IStartupFilter");
        if (serviceType != null && implementationType != null)
        {
            // Register the startup filter with DI.
            builder.ConfigureServices(services =>
                services.TryAddExactTransient(serviceType, implementationType));
        }

        return true;
    }

    private static Assembly? TryGetSentryAspNetCoreAssembly()
    {
        try
        {
            // We can't guarantee the assembly is loaded yet, so we have to attempt to load it ourselves.
            // If the assembly is already loaded, it will just return it.
            return AppDomain.CurrentDomain.Load(SentryAspNetCoreAssemblyFullName);
        }
        catch
        {
            // If not found, we're not using Sentry.AspNetCore in the application.
            return null;
        }
    }
}
