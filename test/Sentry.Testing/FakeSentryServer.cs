using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;

namespace Sentry.Testing
{
    public static class FakeSentryServer
    {
        public static TestServer CreateServer(IReadOnlyCollection<RequestHandler> handlers)
        {
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    _ = app.Use(async (context, next) =>
                        {
                            var handler = handlers.FirstOrDefault(p => p.Path == context.Request.Path);

                            await (handler?.Handler(context) ?? next());
                        });
                });

            return new TestServer(builder);
        }

        public static TestServer CreateServer()
        {
            return CreateServer(new[]
            {
                new RequestHandler
                {
                    Path = "/store",
                    Handler = c => c.Response.WriteAsync(SentryResponses.SentryOkResponseBody)
                }
            });
        }
    }
}
