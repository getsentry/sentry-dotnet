using Microsoft.Extensions.Logging;
using Sentry.Protocol;

namespace Sentry.Extensions.Logging
{
    internal static class LogLevelExtensions
    {
        public static BreadcrumbLevel ToBreadcrumbLevel(this LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Trace:
                    return BreadcrumbLevel.Debug;
                case LogLevel.Debug:
                    return BreadcrumbLevel.Debug;
                case LogLevel.Information:
                    return BreadcrumbLevel.Info;
                case LogLevel.Warning:
                    return BreadcrumbLevel.Warning;
                case LogLevel.Error:
                    return BreadcrumbLevel.Error;
                case LogLevel.Critical:
                    return BreadcrumbLevel.Critical;
                case LogLevel.None:
                default:
                    return (BreadcrumbLevel)level;
            }
        }
    }
}
