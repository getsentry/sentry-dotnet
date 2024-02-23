using Hangfire;
using Hangfire.MemoryStorage;
using Hangfire.Storage;

namespace Sentry.Hangfire.Tests;

public class HangfireFixture : IDisposable
{
    public HangfireFixture()
    {
        GlobalConfiguration.Configuration
            .UseMemoryStorage()
            .UseSentry();
        Server = new BackgroundJobServer();
        MonitoringApi = JobStorage.Current.GetMonitoringApi();
    }

    public BackgroundJobServer Server { get; }

    public IMonitoringApi MonitoringApi { get; }

    public void Dispose()
    {
        Server.Dispose();
    }
}
