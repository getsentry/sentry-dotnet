using System.Linq.Expressions;
using Hangfire;
using Hangfire.MemoryStorage;
using Hangfire.Storage;

namespace Sentry.Hangfire.Tests;

public class HangfireFixture : IDisposable
{
    public IHub Hub { get; set; } = Substitute.For<IHub>();
    public IDiagnosticLogger Logger { get; }
    private readonly BackgroundJobServer _server;
    private readonly IMonitoringApi _monitoringApi;
    public TimeSpan Timeout { get; } = TimeSpan.FromSeconds(3);

    public HangfireFixture()
    {
        Logger = Substitute.For<IDiagnosticLogger>();
        Logger.IsEnabled(SentryLevel.Debug).Returns(true);
        Hub.IsEnabled.Returns(true);

        GlobalConfiguration.Configuration
            .UseMemoryStorage()
            .UseSentry(Hub, Logger);
        _server = new BackgroundJobServer();
        _monitoringApi = JobStorage.Current.GetMonitoringApi();
    }

    public Task Enqueue<T>(Expression<Action<T>> methodCall)
    {
        var jobId = BackgroundJob.Enqueue(methodCall);
        var checkJobState = Task.Run(() =>
        {
            while (true)
            {
                var jobDetails = _monitoringApi.JobDetails(jobId);
                var currentState = jobDetails.History[^1].StateName;

                if (currentState != "Enqueued" && currentState != "Processing")
                {
                    break;
                }

                if (DateTime.UtcNow - jobDetails.CreatedAt > Timeout)
                {
                    throw new TimeoutException("Timed out waiting for the job to finish.");
                }

                Thread.Sleep(100);
            }
        });

        return checkJobState;
    }

    public void Dispose() => _server.Dispose();
}
