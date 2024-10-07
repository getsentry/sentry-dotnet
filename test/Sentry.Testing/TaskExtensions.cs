namespace Sentry.Testing;

public static class TaskExtensions
{
    /// <summary>
    /// See https://devblogs.microsoft.com/pfxteam/tasks-and-unhandled-exceptions/
    /// </summary>
    public static Task FailFastOnException(this Task task)
    {
        task.ContinueWith(
            c => Environment.FailFast("Task faulted", c.Exception),
            TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously
            );
        return task;
    }
}
