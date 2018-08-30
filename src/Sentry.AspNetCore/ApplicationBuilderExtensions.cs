using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Sentry;
using Sentry.AspNetCore;
using Sentry.Extensibility;

// ReSharper disable once CheckNamespace -- Discoverability
namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Extension methods for <see cref="IApplicationBuilder"/>
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static class ApplicationBuilderExtensions
    {
        /// <summary>
        /// Use Sentry integration
        /// </summary>
        /// <param name="app">The application.</param>
        /// <returns></returns>
        public static IApplicationBuilder UseSentry(this IApplicationBuilder app)
        {
            UseServiceProviderProcessors(app.ApplicationServices);

            return app.UseMiddleware<SentryMiddleware>();
        }

        private static void UseServiceProviderProcessors(IServiceProvider provider)
        {
            var hub = provider.GetService<IHub>();
            if (hub != null)
            {
                if (provider.GetService<IEnumerable<ISentryEventProcessor>>().Any())
                {
                    hub.ConfigureScope(s => s.AddEventProcessorProvider(provider.GetServices<ISentryEventProcessor>));
                }

                if (provider.GetService<IEnumerable<ISentryEventExceptionProcessor>>().Any())
                {
                    hub.ConfigureScope(s => s.AddExceptionProcessorProvider(provider.GetServices<ISentryEventExceptionProcessor>));
                }
            }
        }
    }
}
