using Hangfire;
using Hangfire.MemoryStorage;
using Hangfire.Storage;

namespace Sentry.Hangfire.Tests;

public class HangfireFixture : IDisposable
{
    public IHub Hub { get; set; } = Substitute.For<IHub>();
    public IDiagnosticLogger Logger { get; }
    public BackgroundJobServer Server { get; }

    public HangfireFixture()
    {
        Logger = Substitute.For<IDiagnosticLogger>();
        Logger.IsEnabled(SentryLevel.Warning).Returns(true);
        Hub.IsEnabled.Returns(true);

        GlobalConfiguration.Configuration
            .UseMemoryStorage()
            .UseSentry(Hub, Logger);
        Server = new BackgroundJobServer();
    }

    public void Dispose()
    {
        Server.Dispose();
    }
}
