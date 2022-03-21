namespace Sentry.Testing;

public static class AsyncExtensions
{
    // Adapted from:
    // https://docs.microsoft.com/dotnet/standard/asynchronous-programming-patterns/interop-with-other-asynchronous-patterns-and-types#tasks-and-wait-handles
    public static Task WaitOneAsync(this WaitHandle waitHandle)
    {
        if (waitHandle == null)
        {
            throw new ArgumentNullException(nameof(waitHandle));
        }

        var tcs = new TaskCompletionSource<bool>();
        var rwh = ThreadPool.RegisterWaitForSingleObject(waitHandle,
            (_, _) => tcs.TrySetResult(true), null, -1, true);

        var t = tcs.Task;
        t.ContinueWith( _ => rwh.Unregister(null));
        return t;
    }

    public static async Task WaitAsync(this ManualResetEventSlim manualResetEvent)
    {
        await manualResetEvent.WaitHandle.WaitOneAsync();
    }

    public static async Task<bool> WaitAsync(this ManualResetEventSlim manualResetEvent, TimeSpan timeout)
    {
        var task = manualResetEvent.WaitAsync();
        await Task.WhenAny(task, Task.Delay(timeout));
        return task.IsCompleted;
    }
}
