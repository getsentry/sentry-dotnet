using System;
using System.Reflection;
using System.Web;
using Sentry.Internal;

namespace Sentry.AspNet.Internal
{
    internal static class SystemWebVersionLocator
    {
        internal static string? GetCurrent()
        {
            if (ReleaseLocator.GetCurrent() is string release)
            {
                return release;
            }
            else if (HttpContext.Current?.ApplicationInstance?.GetType() is Type type)
            {
                while (type != null && type.Namespace == "ASP")
                {
                    type = type.BaseType;
                }
                if (type?.Assembly is Assembly assembly)
                {
                    return ApplicationVersionLocator.GetCurrent(assembly);
                }
            }
            return null;
        }
    }
}
