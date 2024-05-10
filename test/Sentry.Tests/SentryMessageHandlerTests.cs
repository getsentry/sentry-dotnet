using Sentry.Internal.OpenTelemetry;

namespace Sentry.Tests;

public class SentryMessageHandlerTests
{
    private protected class Fixture
    {
        public readonly SentryOptions Options = new();

        public readonly ISentryClient Client = Substitute.For<ISentryClient>();

        public readonly ISessionManager SessionManager = Substitute.For<ISessionManager>();

        public readonly ISystemClock Clock = Substitute.For<ISystemClock>();

        public Fixture()
        {
            Options.Dsn = ValidDsn;
            Options.TracesSampleRate = 1.0;
        }

        public Hub GetHub()
        {
            var scopeManager = new SentryScopeManager(Options, Client);
            return new Hub(Options, Client, SessionManager, Clock, scopeManager);
        }
    }

    private  protected readonly Fixture _fixture = new();
}
