using System;
using System.Threading.Tasks;
using Sentry;
using Sentry.Samples.Console.Customized;

// One of the ways to set your DSN is via an attribute:
// It could be set via AssemblyInfo.cs and patched via CI.
// Other ways are via environment variable, configuration files and explictly via parameter
[assembly: Dsn(Program.DefaultDsn)]

namespace Sentry.Samples.Console.Customized
{
    internal static class Program
    {
        public const string DefaultDsn = "https://5fd7a6cda8444965bade9ccfd3df9882@sentry.io/1188141";
        // A different DSN for a section of the app (i.e: admin)
        public const string AdminDsn = "https://f670c444cca14cf2bb4bfc403525b6a3@sentry.io/259314";

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
                    h.Proxy = null; //new WebProxy("https://localhost:3128");
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

                // A custom made client, that could be registered with DI,
                // would get disposed by the container on app shutdown

                // Using a different DSN:
                var adminDsn = new Dsn(AdminDsn);
                using (var adminClient = new SentryClient(new SentryOptions { Dsn = adminDsn }))
                {
                    // Make believe web framework middleware
                    var middleware = new AdminPartMiddleware(adminClient, null);
                    var request = new { Path = "/admin" }; // made up request
                    middleware.Invoke(request);

                } // Dispose the client which flushes any queued events

                SentryCore.CaptureException(
                    new Exception("Error outside of the admin section: Goes to the default DSN"));

            }  // On Dispose: SDK closed, events queued are flushed/sent to Sentry
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

                    SentryCore.CaptureException(new Exception("Error at the admin section"));
                    // Else it uses the default client

                    _middleware?.Invoke(request);

                } // Scope is disposed.
            }
        }
    }
}
