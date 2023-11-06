using System.Reflection;
using System.Xml.Xsl;
using Sentry;
using Sentry.Extensibility;

internal static class Program
{
    private static async Task Main()
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
            // A Sentry Data Source Name (DSN) is required.
            // See https://docs.sentry.io/product/sentry-basics/dsn-explainer/
            // You can set it in the SENTRY_DSN environment variable, or you can set it in code here.
            // o.Dsn = "... Your DSN ...";

            // Send stack trace for events that were not created from an exception
            // e.g: CaptureMessage, log.LogDebug, log.LogInformation ...
            o.AttachStacktrace = true;

            // Sentry won't consider code from namespace LibraryX.* as part of the app code and will hide it from the stacktrace by default
            // To see the lines from non `AppCode`, select `Full`. Will include non App code like System.*, Microsoft.* and LibraryX.*
            o.AddInAppExclude("LibraryX.");

            // Before excluding all prefixed 'LibraryX.', any stack trace from a type namespaced 'LibraryX.Core' will be considered InApp.
            o.AddInAppInclude("LibraryX.Core");

            // Send personal identifiable information like the username logged on to the computer and machine name
            o.SendDefaultPii = true;

            // To enable event sampling, uncomment:
            // o.SampleRate = 0.5f; // Randomly drop (don't send to Sentry) half of events

            // Modifications to event before it goes out. Could replace the event altogether
            o.SetBeforeSend((@event, _) =>
                {
                    // Drop an event altogether:
                    if (@event.Tags.ContainsKey("SomeTag"))
                    {
                        return null;
                    }

                    return @event;
                }
            );

            // Allows inspecting and modifying, returning a new or simply rejecting (returning null)
            o.SetBeforeBreadcrumb((crumb, hint) =>
            {
                // Don't add breadcrumbs with message containing:
                if (crumb.Message?.Contains("bad breadcrumb") == true)
                {
                    return null;
                }

                // Replace breadcrumbs entirely incase of a drastic hint
                const string replaceBreadcrumb = "don't trust this breadcrumb";
                if (hint.Items.TryGetValue(replaceBreadcrumb, out var replacementMessage))
                {
                    return new Breadcrumb((string)replacementMessage, null, null, null, BreadcrumbLevel.Critical);
                }

                return crumb;
            });

            // Ignore exception by its type:
            o.AddExceptionFilterForType<XsltCompileException>();

            // Configure the background worker which sends events to sentry:
            // Wait up to 5 seconds before shutdown while there are events to send.
            o.ShutdownTimeout = TimeSpan.FromSeconds(5);

            // Enable SDK logging with Debug level
            o.Debug = true;
            // To change the verbosity, use:
            // o.DiagnosticLevel = SentryLevel.Info;
            // To use a custom logger:
            // o.DiagnosticLogger = ...

            // Using a proxy:
            o.HttpProxy = null; //new WebProxy("https://localhost:3128");

            // Example customizing the HttpMessageHandlers created
            o.CreateHttpMessageHandler = () => new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (_, certificate, _, _) =>
                    !certificate.Archived
            };

            // Access to the HttpClient created to serve the SentryClint
            o.ConfigureClient = client => client.DefaultRequestHeaders.TryAddWithoutValidation("CustomHeader", new[] { "my value" });

            // Control/override how to apply the State object into the scope
            o.SentryScopeStateProcessor = new MyCustomerScopeStateProcessor();
        }))
        {
            // Ignored by its type due to the setting above
            SentrySdk.CaptureException(new XsltCompileException());

            SentrySdk.AddBreadcrumb(
                "A 'bad breadcrumb' that will be rejected because of 'BeforeBreadcrumb callback above.'");

            SentrySdk.AddBreadcrumb(
                new Breadcrumb("A breadcrumb that will be replaced by the 'BeforeBreadcrumb callback because of the hint", null),
                new Hint("don't trust this breadcrumb", "trust this instead")
                );

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
            SentrySdk.CaptureMessage("Fatal message!", s =>
            {
                s.Level = SentryLevel.Fatal;
                s.TransactionName = "main";
                s.Environment = "SpecialEnvironment";

                // Add a file attachment for upload
                s.AddAttachment(typeof(Program).Assembly.Location);
            });

            var eventId = SentrySdk.CaptureMessage("Some warning!", SentryLevel.Warning);

            // Send an user feedback linked to the warning.
            var timestamp = DateTime.Now.Ticks;
            var user = $"user{timestamp}";
            var email = $"user{timestamp}@user{timestamp}.com";

            SentrySdk.CaptureUserFeedback(new UserFeedback(eventId, user, email, "this is a sample user feedback"));

            var error = new Exception("Attempting to send this multiple times");

            // Only the first capture will be sent to Sentry
            for (var i = 0; i < 3; i++)
            {
                // The SDK is able to detect duplicate events:
                // This is useful, for example, when multiple loggers log the same exception. Or exception is re-thrown and recaptured.
                SentrySdk.CaptureException(error);
            }

            var count = 10;
            for (var i = 0; i < count; i++)
            {
                const string msg = "{0} of {1} items we'll wait to flush to Sentry!";
                SentrySdk.CaptureEvent(new SentryEvent
                {
                    Message = new SentryMessage
                    {
                        Message = msg,
                        Formatted = string.Format(msg, i, count)
                    },
                    Level = SentryLevel.Debug
                });
            }
            // Console output will show queue being flushed.
            await SentrySdk.FlushAsync();

            // -------------------------

            // A custom made client, that could be registered with DI,
            // would get disposed by the container on app shutdown

            var evt = new SentryEvent
            {
                Message = "Starting new client"
            };
            evt.AddBreadcrumb("Breadcrumb directly to the event");
            evt.User.Username = "some@user";
            // Group all events with the following fingerprint:
            evt.SetFingerprint("NewClientDebug");
            evt.Level = SentryLevel.Debug;
            SentrySdk.CaptureEvent(evt);

            // Using a different DSN for a section of the app (i.e: admin)
            const string AdminDsn = "https://f670c444cca14cf2bb4bfc403525b6a3@sentry.io/259314";
            using (var adminClient = new SentryClient(new SentryOptions { Dsn = AdminDsn }))
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
            using (SentrySdk.PushScope(new SpecialContextObject()))
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

    private class MyCustomerScopeStateProcessor : ISentryScopeStateProcessor
    {
        private readonly ISentryScopeStateProcessor _fallback = new DefaultSentryScopeStateProcessor();

        public void Apply(Scope scope, object state)
        {
            if (state is SpecialContextObject specialState)
            {
                scope.Tags["SpecialContextObject"] = specialState.A + specialState.B;
            }
            else
            {
                _fallback.Apply(scope, state);
            }
        }
    }

    private class SpecialContextObject
    {
        public string A { get; } = "hello";
        public string B { get; } = "world";
    }
}
