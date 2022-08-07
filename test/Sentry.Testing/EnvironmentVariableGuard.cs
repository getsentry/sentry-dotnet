namespace Sentry.Testing;

public static class EnvironmentVariableGuard
{
    // To allow different xunit collections use of this
    private static readonly SemaphoreSlim Lock = new(1, 1);

    public static void WithVariable(string key, string value, Action action)
    {
        Lock.Wait();
        Environment.SetEnvironmentVariable(key, value, EnvironmentVariableTarget.Process);
        try
        {
            action();
        }
        finally
        {
            Environment.SetEnvironmentVariable(key, null, EnvironmentVariableTarget.Process);
            Lock.Release();
        }
    }

    public static async Task WithVariableAsync(string key, string value, Func<Task> asyncAction)
    {
        await Lock.WaitAsync();
        Environment.SetEnvironmentVariable(key, value, EnvironmentVariableTarget.Process);
        try
        {
            await asyncAction();
        }
        finally
        {
            Environment.SetEnvironmentVariable(key, null, EnvironmentVariableTarget.Process);
            Lock.Release();
        }
    }
}
