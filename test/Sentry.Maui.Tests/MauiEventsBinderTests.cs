using NSubstitute;
using Sentry.Maui.CommunityToolkitMvvm;
using Sentry.Maui.Internal;

namespace Sentry.Maui.Tests;

public partial class MauiEventsBinderTests
{
    private class Fixture
    {
        public IHub Hub { get; }

        public MauiEventsBinder Binder { get; }

        public Scope Scope { get; } = new();

        public SentryMauiOptions Options { get; } = new();

        public Fixture()
        {
            Hub = Substitute.For<IHub>();
            Hub.When(h => h.ConfigureScope(Arg.Any<Action<Scope>>()))
                .Do(c => c.Arg<Action<Scope>>()(Scope));

            Hub.StartSpan(default, default).ReturnsForAnyArgs(args =>
            {
                var tracer = Substitute.For<ITransactionTracer>();
                return tracer;
            });


            Options.Debug = true;
            var logger = Substitute.For<IDiagnosticLogger>();
            logger.IsEnabled(Arg.Any<SentryLevel>()).Returns(true);
            Options.DiagnosticLogger = logger;
            var options = Microsoft.Extensions.Options.Options.Create(Options);
            Binder = new MauiEventsBinder(
                Hub,
                options,
                [
                    new MauiButtonEventsBinder(),
                    new MauiImageButtonEventsBinder(),
                    new CtMvvmMauiElementEventBinder(Hub)
                ]
            );
        }
    }

    private readonly Fixture _fixture = new();

    // Tests are in partial class files for better organization
}
