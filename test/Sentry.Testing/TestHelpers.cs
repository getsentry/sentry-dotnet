using Xunit.Sdk;

namespace Sentry.Testing;

public static class TestHelpers
{
    public static void RetryTest(int maxAttempts, Action test) =>
        RetryTest(maxAttempts, null, test);

    public static void RetryTest(int maxAttempts, ITestOutputHelper output, Action test) =>
        RetryTest(false, maxAttempts, output, test);

    public static void RetryTest(bool retryOnMobileOnly, int maxAttempts, Action test) =>
        RetryTest(retryOnMobileOnly, maxAttempts, null, test);

    public static void RetryTest(bool retryOnMobileOnly, int maxAttempts, ITestOutputHelper output, Action test)
    {
#if !__MOBILE__
        if (retryOnMobileOnly)
        {
            test.Invoke();
            return;
        }
#endif

        for (var i = 1; i <= maxAttempts; i++)
        {
            try
            {
                test.Invoke();
                return;
            }
            catch (XunitException)
            {
                if (i == maxAttempts)
                {
                    output?.WriteLine($"Attempt #{i} failed. Max attempts reached.");
                    throw;
                }

                output?.WriteLine($"Attempt #{i} failed. Retrying.");
            }
        }
    }

    public static Task RetryTestAsync(int maxAttempts, Func<Task> test) =>
        RetryTestAsync(maxAttempts, null, test);

    public static Task RetryTestAsync(int maxAttempts, ITestOutputHelper output, Func<Task> test) =>
        RetryTestAsync(false, maxAttempts, output, test);

    public static Task RetryTestAsync(bool retryOnMobileOnly, int maxAttempts, Func<Task> test) =>
        RetryTestAsync(retryOnMobileOnly, maxAttempts, null, test);

    public static async Task RetryTestAsync(bool retryOnMobileOnly, int maxAttempts, ITestOutputHelper output, Func<Task> test)
    {
#if !__MOBILE__
        if (retryOnMobileOnly)
        {
            await test.Invoke();
            return;
        }
#endif

        for (var i = 1; i <= maxAttempts; i++)
        {
            try
            {
                await test.Invoke();
                return;
            }
            catch (XunitException)
            {
                if (i == maxAttempts)
                {
                    output?.WriteLine($"Attempt #{i} failed. Max attempts reached.");
                    throw;
                }

                output?.WriteLine($"Attempt #{i} failed. Retrying.");
            }
        }
    }
}
