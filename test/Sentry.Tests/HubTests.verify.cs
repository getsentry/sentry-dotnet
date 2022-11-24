using Sentry.Testing;

namespace Sentry.Tests;

[UsesVerify]
public partial class HubTests
{
    [Fact]
    public async Task CaptureEvent_ActiveTransaction_UnhandledExceptionTransactionEndedAsCrashed()
    {
        // Arrange
        var worker = new FakeBackgroundWorker();

        var options = new SentryOptions
        {
            Dsn = ValidDsn,
            Release = "release",
            TracesSampleRate = 1.0
        };
        var client = new SentryClient(options, worker);
        var hub = new Hub(options, client);

        var transaction = hub.StartTransaction("my transaction", "my operation");
        hub.ConfigureScope(scope => scope.Transaction = transaction);
        hub.StartSession();

        // Act
        hub.CaptureEvent(new()
        {
            SentryExceptions = new[]
            {
                new SentryException
                {
                    Mechanism = new()
                    {
                        Handled = false
                    }
                }
            }
        });

        await Verify(worker.Envelopes)
            .IgnoreStandardSentryMembers()
            .IgnoreMember("Stacktrace")
            .IgnoreMember<SentryThread>(_ => _.Name);
    }
}
