using System;
using System.Net;
using System.Threading.Tasks;
using Sentry;

// One of the ways to set your DSN is via an attribute:
// It could be set via AssemblyInfo.cs and patched via CI
[assembly: Dsn("https://key@sentry.io/id")]

namespace Sentry.Samples.Console.Customized
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            // When the SDK is disabled, no callback is executed:
            await SentryCore.ConfigureScopeAsync(async scope =>
            {
                // Never executed:
                // This could be any async I/O operation, like a DB query
                await Task.Yield();
                scope.SetExtra("Key", "Value");
            });

            // Enable the SDK
            using (SentryCore.Init(o =>
            {
                // Modifications to event before it goes out. Could replace the event altogether
                o.BeforeSend = @event =>
                {
                    // Drop an event altogether:
                    if (@event.Tags.ContainsKey("SomeTag"))
                    {
                        return null;
                    }

                    // Create a totally new event or modify the current one:
                    @event.ServerName = null; // Make sure no ServerName is sent out
                    return @event;
                };

                // Configure the background worker which sends events to sentry:
                o.Worker(w =>
                {
                    // Poll for events every 100ms
                    w.EmptyQueueDelay = TimeSpan.FromMilliseconds(100);
                    // Wait up to 5 seconds before shutdown while there are events to send.
                    w.ShutdownTimeout = TimeSpan.FromSeconds(5);
                });

                o.Http(h =>
                {
                    // Using a proxy:
                    h.Proxy = new WebProxy("https://localhost:3128");
                });
            }))
            {
                await SentryCore.ConfigureScopeAsync(async scope =>
                {
                    // This could be any async I/O operation, like a DB query
                    await Task.Yield();
                    scope.SetExtra("Key", "Value");
                });

                SentryCore.CaptureException(new Exception("Something went wrong."));

                // -------------------------

                // A custom made client, that can be registered with DI,
                // would get disposed by the container on app shutdown
                var adminDsn = new Dsn("https://key@sentry.io/admin-project");
                using (var adminClient = new SentryClient(new SentryOptions { Dsn = adminDsn }))
                {
                    // Make believe web framework middleware
                    var middleware = new AdminPartMiddleware(adminClient, null);
                    var request = new { Path = "/bla" }; // made up request
                    middleware.Invoke(request);

                } // A client created by hand has its lifetime managed by the creator
            }  // On Dispose: SDK closed, events queued are flushed
        }

        private class AdminPartMiddleware
        {
            private readonly ISentryClient _adminClient;
            private readonly dynamic _middleware;

            public AdminPartMiddleware(ISentryClient adminClient, dynamic middleware)
            {
                _adminClient = adminClient;
                _middleware = middleware;
            }

            public void Invoke(dynamic request)
            {
                using (SentryCore.PushScope())
                {
                    SentryCore.AddBreadcrumb(request.Path, "request-path");

                    // Change the SentryClient in case the request is to the admin part:
                    if (request.Path.StartsWith("/admin"))
                    {
                        // Within this scope, the _adminClient will be used instead of whatever
                        // client was defined before this point:
                        SentryCore.BindClient(_adminClient);
                    }
                    // Else it uses the default client

                    _middleware?.Invoke(request);

                } // Scope is disposed.
            }
        }
    }
}
