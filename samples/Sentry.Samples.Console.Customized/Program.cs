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

                o.Worker(w =>
                {
                    w.EmptyQueueDelay = TimeSpan.FromMilliseconds(500); // Poll for events every 500ms
                    w.FullQueueBlockTimeout = TimeSpan.FromMilliseconds(100);
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
                var adminDsn = new Dsn("https://key@sentry.io/admin-project-id");
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
