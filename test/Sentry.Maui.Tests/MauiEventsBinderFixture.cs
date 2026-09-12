using Sentry.Internal;
using Sentry.Maui.Internal;

namespace Sentry.Maui.Tests;

internal class MauiEventsBinderFixture
{
    public IHubInternal Hub { get; }

    public MauiEventsBinder Binder { get; }

    public Scope Scope { get; } = new();

    public SentryMauiOptions Options { get; } = new();

    public MauiEventsBinderFixture(params IEnumerable<IMauiElementEventBinder> elementEventBinders)
    {
        Hub = Substitute.For<IHubInternal>();
        Hub.SubstituteConfigureScope(Scope);

        Options.Debug = true;
        var logger = Substitute.For<IDiagnosticLogger>();
        logger.IsEnabled(Arg.Any<SentryLevel>()).Returns(true);
        Options.DiagnosticLogger = logger;
        var options = Microsoft.Extensions.Options.Options.Create(Options);
        Binder = new MauiEventsBinder(
            Hub,
            options,
            elementEventBinders
        );
    }
}
