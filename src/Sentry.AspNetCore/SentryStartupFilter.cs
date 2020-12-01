using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Sentry.AspNetCore
{
    /// <inheritdoc />
    internal class SentryStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => e =>
        {
            e.UseSentry();

            next(e);
        };
    }
}
