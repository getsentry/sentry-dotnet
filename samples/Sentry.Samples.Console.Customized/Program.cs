using System;
using System.Threading.Tasks;
using Sentry;
using Sentry.Extensibility;

// One of the ways to set your DSN is via an attribute:
// It could be set via AssemblyInfo.cs and patched via CI
[assembly: Dsn("https://key@sentry.io/id")]

namespace Sentry.Samples.Console.Customized
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            // With the SDK disabled so the callback is never invoked
            await SentryCore.ConfigureScopeAsync(async scope =>
            {
                // This could be any async I/O operation, like a DB query
                await Task.Yield();
                scope.SetExtra("Key", "Value");
            });

            // Enable the SDK
            SentryCore.Init(o =>
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

                o.Worker(w =>
                {
                    w.EmptyQueueDelay = TimeSpan.FromMilliseconds(500); // Poll for events every 500ms
                    w.FullQueueBlockTimeout = TimeSpan.FromMilliseconds(100); 
                });
            });

            await SentryCore.ConfigureScopeAsync(async scope =>
            {
                // This could be any async I/O operation, like a DB query
                await Task.Yield(); 
                scope.SetExtra("Key", "Value");
            });

            SentryCore.CaptureException(new Exception("Something went wrong."));
            SentryCore.CloseAndFlush();

            // -------------------------

            // A custom made client, registered with DI which gets disposed by
            // the container on app shutdown
            var adminClient = new SentryClient(new SentryOptions
            {
                Dsn = new Dsn("admin-project-dsn")
            });

            // Make believe web framework middleware
            var middleware = new AdminPartMiddleware(adminClient, null);

            var request = new Request { Path = "/bla" }; // made up request
            middleware.Invoke(request);
        }

        private class Request
        {
            public string Path { get; set; }
        }

        private interface IMiddleware
        {
            void Invoke(Request request);
        }

        private class AdminPartMiddleware
        {
            private readonly ISentryClient _adminClient;
            private readonly IMiddleware _middleware;

            public AdminPartMiddleware(ISentryClient adminClient, IMiddleware middleware)
            {
                _adminClient = adminClient;
                _middleware = middleware;
            }

            public void Invoke(Request request)
            {
                using (SentryCore.PushScope())
                {
                    SentryCore.AddBreadcrumb(request.Path, "request-path");

                    if (request.Path.StartsWith("/admin"))
                    {
                        // Within this scope, the _adminClient will be used instead of whatever
                        // client was defined before this point:
                        SentryCore.BindClient(_adminClient);

                        _middleware.Invoke(request);
                    }

                }
            }
        }
    }
}
