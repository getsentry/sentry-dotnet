namespace Sentry.Quartz.Tests;

internal class NoOpJob : IJob
{
    public Task Execute(IJobExecutionContext context) => Task.CompletedTask;
}
