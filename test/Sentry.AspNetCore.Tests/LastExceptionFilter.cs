using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Sentry.AspNetCore.Tests
{
    public class LastExceptionFilter : IStartupFilter
    {
        public Exception LastException { get; set; }

        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
            =>
                e =>
                {
                    _ = e.Use(async (_, n) =>
                    {
                        try
                        {
                            await n();
                        }
                        catch (Exception ex)
                        {
                            LastException = ex;
                        }
                    });

                    next(e);
                };
    }
}
