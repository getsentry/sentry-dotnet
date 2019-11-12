#if NETSTANDARD2_0
using IWebHostEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;
#else
using Microsoft.AspNetCore.Hosting;
#endif

namespace Sentry.AspNetCore
{
    internal static class HostingEnvironmentExtensions
    {
        public static string RootPath(this IWebHostEnvironment env) => env.
#if NETSTANDARD2_0
            WebRootPath;
#else
            ContentRootPath;
#endif
    }
}
