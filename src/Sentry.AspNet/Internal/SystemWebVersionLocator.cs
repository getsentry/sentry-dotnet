using System;
using System.Reflection;
using System.Web;
using Sentry.Internal;

namespace Sentry.AspNet.Internal
{
    internal static class SystemWebVersionLocator
    {
        public static string? GetCurrent() => GetCurrent(ReleaseLocator.GetCurrent());
        internal static string? GetCurrent(string? release)

        {
            if (release != null)
            {
                return release;
            }
            else if (HttpContext.Current?.ApplicationInstance?.GetType() is { } type)
            {
                //Usually the type is ASP.global_asax and the BaseType is the Web Application.
                while (type != null && type.Namespace == "ASP")
                {
                    type = type.BaseType;
                }
                if (type?.Assembly is { } assembly)
                {
                    return ApplicationVersionLocator.GetCurrent(assembly);
                }
            }
            return null;
        }
    }
}
