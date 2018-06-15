using System.ComponentModel;
using Sentry.AspNetCore;

// ReSharper disable once CheckNamespace -- Discoverability
namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Extension methods for <see cref="IApplicationBuilder"/>
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class ApplicationBuilderExtensions
    {
        /// <summary>
        /// Uses the Sentry's middleware to capture exceptions thrown in the pipeline
        /// </summary>
        /// <param name="app">The application.</param>
        /// <returns></returns>
        public static IApplicationBuilder UseSentry(this IApplicationBuilder app)
            => app.UseMiddleware<SentryMiddleware>();
    }
}
