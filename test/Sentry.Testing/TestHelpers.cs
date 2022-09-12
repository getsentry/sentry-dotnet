using Xunit.Sdk;

namespace Sentry.Testing;

public static class TestHelpers
{
    public static void RetryTest(int maxAttempts, Action test) =>
        RetryTest(maxAttempts, null, test);

    public static void RetryTest(int maxAttempts, ITestOutputHelper output, Action test)
    {
        for (var i = 1; i <= maxAttempts; i++)
        {
            try
            {
                test.Invoke();
                return;
            }
            catch (XunitException)
            {
                output?.WriteLine(i == maxAttempts
                    ? $"Attempt #{i} failed. Max attempts reached."
                    : $"Attempt #{i} failed. Retrying.");
            }
        }
    }

    public static async Task RetryTestAsync(int maxAttempts, Func<Task> test) =>
        await RetryTestAsync(maxAttempts, null, test);

    public static async Task RetryTestAsync(int maxAttempts, ITestOutputHelper output, Func<Task> test)
    {
        for (var i = 1; i <= maxAttempts; i++)
        {
            try
            {
                await test.Invoke();
                return;
            }
            catch (XunitException)
            {
                output?.WriteLine(i == maxAttempts
                    ? $"Attempt #{i} failed. Max attempts reached."
                    : $"Attempt #{i} failed. Retrying.");
            }
        }
    }
}
