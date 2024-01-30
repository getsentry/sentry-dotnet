using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Sentry.AspNetCore;

internal class SentryTracingStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return builder =>
        {
            var wrappedBuilder = new SentryTracingBuilder(builder);
            next(wrappedBuilder);
        };
    }
}
