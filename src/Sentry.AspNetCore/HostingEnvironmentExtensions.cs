#if NETSTANDARD2_0
using IHostingEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;
#else
using IHostingEnvironment = Microsoft.AspNetCore.Hosting.IWebHostEnvironment;
#endif

namespace Sentry.AspNetCore
{
    internal static class HostingEnvironmentExtensions
    {
        public static string RootPath(this IHostingEnvironment env) => env.WebRootPath;
    }
}
