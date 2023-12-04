using Microsoft.Extensions.Options;
using Sentry.Maui.Internal;

namespace Sentry.Maui.Tests;

public partial class MauiEventsBinderTests
{
    private class Fixture
    {
        public MauiEventsBinder Binder { get; }

        public Scope Scope { get; } = new();

        public SentryMauiOptions Options { get; } = new();

        public Fixture()
        {
            var hub = Substitute.For<IHub>();
            hub.When(h => h.ConfigureScope(Arg.Any<Action<Scope>>()))
                .Do(c => c.Arg<Action<Scope>>()(Scope));

            var options = Microsoft.Extensions.Options.Options.Create(Options);
            Binder = new MauiEventsBinder(hub, options);
        }
    }

    private readonly Fixture _fixture = new();

    // Tests are in partial class files for better organization
}
