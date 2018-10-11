using System;
using System.Reflection;
using System.Threading.Tasks;
using Sentry;
using Sentry.Extensibility;
using Sentry.Protocol;

// One of the ways to set your DSN is via an attribute:
// It could be set via AssemblyInfo.cs and patched via CI.
// Other ways are via environment variable, configuration files and explictly via parameter to Init
[assembly: Dsn(Program.DefaultDsn)]
// Tracks the release which sent the event and enables more features: https://docs.sentry.io/learn/releases/
// Much like the DSN above, this is only one of the ways to define the release.
// If not set here, it can also be defined via appsettings.json, environment variable 'SENTRY_RELEASE' and AssemblyVersion
// STANDARD_CI_SOURCE_REVISION_ID -> TeamCity: %build.vcs.number%, VSTS: BUILD_SOURCEVERSION, Travis-CI: TRAVIS_COMMIT, AppVeyor: APPVEYOR_REPO_COMMIT, CircleCI: CIRCLE_SHA1
[assembly: AssemblyInformationalVersion("e386dfd")]

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
            // Send stack trace for events that were not created from an exception
            // e.g: CaptureMessage, log.LogDebug, log.LogInformation ...
            o.AttachStacktrace = true;

            // Sentry won't consider code from namespace LibraryX.* as part of the app code and will hide it from the stacktrace by default
            // To see the lines from non `AppCode`, select `Full`. That'll include non App code like System.*, Microsoft.* and LibraryX.*
            o.AddInAppExclude("LibraryX.");

            // Send personal identifiable information like the username logged on to the computer and machine name
            o.SendDefaultPii = true;

            // To enable event sampling, uncomment:
            // o.SampleRate = 0.5f; // Randomly drop (don't send to Sentry) half of events

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

            // Allows inspecting and modifying, returning a new or simply rejecting (returning null)
            o.BeforeBreadcrumb = crumb =>
            {
                // Don't add breadcrumbs with message containing:
                if (crumb.Message?.Contains("bad breadcrumb") == true)
                {
                    return null;
                }

                return crumb;
            };

            // Configure the background worker which sends events to sentry:
            // Wait up to 5 seconds before shutdown while there are events to send.
            o.ShutdownTimeout = TimeSpan.FromSeconds(5);

            // Enable SDK logging with Debug level
            o.Debug = true;
            // To change the verbosity, use:
            // o.DiagnosticsLevel = SentryLevel.Info;
            // To use a custom logger:
            // o.DiagnosticLogger = ...

            // Using a proxy:
            o.HttpProxy = null; //new WebProxy("https://localhost:3128");

            // Example customizing the HttpClientHandlers created
            o.ConfigureHandler = (handler, dsn) =>
            {
                handler.ServerCertificateCustomValidationCallback =
                    // A custom certificate validation
                    (sender, certificate, chain, sslPolicyErrors) => !certificate.Archived;
            };

            // Access to the HttpClient created to serve the SentryClint
            o.ConfigureClient = (client, dsn) =>
            {
                client.DefaultRequestHeaders.TryAddWithoutValidation("CustomHeader", new[] { "my value" });
            };
        }))
        {
            SentrySdk.AddBreadcrumb(
                "A 'bad breadcrumb' that will be rejected because of 'BeforeBreadcrumb callback above.'");

            // Data added to the root scope (no PushScope called up to this point)
            // The modifications done here will affect all events sent and will propagate to child scopes.
            await SentrySdk.ConfigureScopeAsync(async scope =>
            {
                scope.AddEventProcessor(new SomeEventProcessor());
                scope.AddExceptionProcessor(new ArgumentExceptionProcessor());

                // This could be any async I/O operation, like a DB query
                await Task.Yield();
                scope.SetExtra("SomeExtraInfo",
                    new
                    {
                        Data = "Value fetched asynchronously",
                        ManaLevel = 199
                    });
            });

            // Configures a scope which is only valid within the callback
            SentrySdk.WithScope(s =>
            {
                s.Level = SentryLevel.Fatal;
                s.Transaction = "main";
                s.Environment = "SpecialEnvironment";

                SentrySdk.CaptureMessage("Fatal message!");
            });

            SentrySdk.CaptureMessage("Some warning!", SentryLevel.Warning);

            var error = new Exception("Attempting to send this multiple times");

            // Only the first capture will be sent to Sentry
            for (var i = 0; i < 3; i++)
            {
                // The SDK is able to detect duplicate events:
                // This is useful, for example, when multiple loggers log the same exception. Or exception is re-thrown and recaptured.
                SentrySdk.CaptureException(error);
            }

            // -------------------------

            // A custom made client, that could be registered with DI,
            // would get disposed by the container on app shutdown

            var evt = new SentryEvent();
            evt.Message ="Starting new client";
            evt.AddBreadcrumb("Breadcrumb directly to the event");
            evt.User.Username = "some@user";
            // Group all events with the following fingerprint:
            evt.SetFingerprint(new [] { "NewClientDebug"});
            evt.Level = SentryLevel.Debug;
            SentrySdk.CaptureEvent(evt);

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
        public SentryEvent Process(SentryEvent @event)
        {
            // Here you can modify the event as you need
            if (@event.Level > SentryLevel.Info)
            {
                @event.AddBreadcrumb("Processed by " + nameof(SomeEventProcessor));
            }

            return @event;
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
