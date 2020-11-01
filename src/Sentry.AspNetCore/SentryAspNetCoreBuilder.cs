using System;
using Microsoft.Extensions.DependencyInjection;

namespace Sentry.AspNetCore
{
    /// <summary>
    /// Allows for further customization of Sentry ASP.NET Core integration
    /// </summary>
    internal class SentryAspNetCoreBuilder : ISentryBuilder
    {
        public IServiceCollection Services { get; }

        public SentryAspNetCoreBuilder(IServiceCollection services)
        {
            Services = services ?? throw new ArgumentNullException(nameof(services));
        }
    }
}
