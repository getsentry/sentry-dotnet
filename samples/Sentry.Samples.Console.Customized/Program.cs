using System;
using System.Reflection;
using System.Threading.Tasks;
using Sentry;
using Sentry.Extensibility;
using Sentry.Protocol;
using Sentry.Samples.Console.Customized;

// One of the ways to set your DSN is via an attribute:
// It could be set via AssemblyInfo.cs and patched via CI.
// Other ways are via environment variable, configuration files and explictly via parameter to Init
[assembly: Dsn(Program.DefaultDsn)]
// Tracks the release which sent the event and enables more features: https://docs.sentry.io/learn/releases/
// Much like the DSN above, this is only one of the ways to define the release.
// If not set here, it can also be defined via appsettings.json, environment variable 'SENTRY_RELEASE' and AssemblyVersion
// STANDARD_CI_SOURCE_REVISION_ID -> TeamCity: %build.vcs.number%, VSTS: BUILD_SOURCEVERSION, Travis-CI: TRAVIS_COMMIT, AppVeyor: APPVEYOR_REPO_COMMIT, CircleCI: CIRCLE_SHA1
[assembly: AssemblyInformationalVersion("e386dfd")]

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
            await SentrySdk.ConfigureScopeAsync(async scope =>
            {
                // Never executed:
                // This could be any async I/O operation, like a DB query
                await Task.Yield();
                scope.SetExtra("Key", "Value");
            });

            // Enable the SDK
            using (SentrySdk.Init(o =>
            {
                o.AddEventProcessor(new SomeEventProcessor());
                o.AddExceptionProcessor(new ArgumentExceptionProcessor());

                // Modifications to event before it goes out. Could replace the event altogether
                o.BeforeSend = @event =>
                {
                    // Drop an event altogether:
                    if (@event.Tags.ContainsKey("SomeTag"))
                    {
                        return null;
                    }

                    return @event;
                };

                // Configure the background worker which sends events to sentry:
                o.Worker(w =>
                {
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
                await SentrySdk.ConfigureScopeAsync(async scope =>
                {
                    // This could be any async I/O operation, like a DB query
                    await Task.Yield();
                    scope.SetExtra("SomeExtraInfo",
                        new
                        {
                            Data = "Value fetched asynchronously",
                            ManaLevel = 199
                        });
                });

                SentrySdk.CaptureMessage("Some warning!", SentryLevel.Warning);

                // -------------------------

                // A custom made client, that could be registered with DI,
                // would get disposed by the container on app shutdown

                SentrySdk.CaptureMessage("Starting new client");
                // Using a different DSN:
                var adminDsn = new Dsn(AdminDsn);
                using (var adminClient = new SentryClient(new SentryOptions { Dsn = adminDsn }))
                {
                    // Make believe web framework middleware
                    var middleware = new AdminPartMiddleware(adminClient, null);
                    var request = new { Path = "/admin" }; // made up request
                    middleware.Invoke(request);

                } // Dispose the client which flushes any queued events

                SentrySdk.CaptureException(
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
                using (SentrySdk.PushScope())
                {
                    SentrySdk.AddBreadcrumb(request.Path, "request-path");

                    // Change the SentryClient in case the request is to the admin part:
                    if (request.Path.StartsWith("/admin"))
                    {
                        // Within this scope, the _adminClient will be used instead of whatever
                        // client was defined before this point:
                        SentrySdk.BindClient(_adminClient);
                    }

                    SentrySdk.CaptureException(new Exception("Error at the admin section"));
                    // Else it uses the default client

                    _middleware?.Invoke(request);

                } // Scope is disposed.
            }
        }

        private class SomeEventProcessor : ISentryEventProcessor
        {
            public void Process(SentryEvent @event)
            {
                // Here you can modify the event as you need
                if (@event.Level > SentryLevel.Info)
                {
                    @event.AddBreadcrumb("Processed by " + nameof(SomeEventProcessor));

                    @event.User = new User
                    {
                        Username = Environment.UserName
                    };

                    @event.ServerName = Environment.MachineName;
                }
            }
        }

        private class ArgumentExceptionProcessor : SentryEventExceptionProcessor<ArgumentException>
        {
            protected override void ProcessException(ArgumentException exception, SentryEvent sentryEvent)
            {
                // Handle specific types of exceptions and add more data to the event
                sentryEvent.SetTag("parameter-name", exception.ParamName);
            }
        }
    }
}
