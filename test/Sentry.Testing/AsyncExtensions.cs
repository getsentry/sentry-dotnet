namespace Sentry.Testing;

public static class AsyncExtensions
{
    // Adapted from:
    // https://stackoverflow.com/a/18766131
    //
    // See also:
    // https://docs.microsoft.com/dotnet/standard/asynchronous-programming-patterns/interop-with-other-asynchronous-patterns-and-types#tasks-and-wait-handles

    public static Task AsTask(this WaitHandle handle)
    {
        return AsTask(handle, Timeout.InfiniteTimeSpan);
    }

    public static Task AsTask(this WaitHandle handle, TimeSpan timeout)
    {
        var tcs = new TaskCompletionSource<object>();
        var registration = ThreadPool.RegisterWaitForSingleObject(handle, (state, timedOut) =>
        {
            var localTcs = (TaskCompletionSource<object>)state;
            if (timedOut)
            {
                localTcs.TrySetCanceled();
            }
            else
            {
                localTcs.TrySetResult(null);
            }
        }, tcs, timeout, executeOnlyOnce: true);
        tcs.Task.ContinueWith((_, state) =>
            ((RegisteredWaitHandle)state).Unregister(null), registration, TaskScheduler.Default);
        return tcs.Task;
    }

    public static async Task WaitAsync(this ManualResetEventSlim manualResetEvent)
    {
        await manualResetEvent.WaitHandle.AsTask();
    }

    public static async Task<bool> WaitAsync(this ManualResetEventSlim manualResetEvent, TimeSpan timeout)
    {
        var task = manualResetEvent.WaitHandle.AsTask(timeout);
        await task;
        return task.IsCompleted && !task.IsCanceled;
    }
}
