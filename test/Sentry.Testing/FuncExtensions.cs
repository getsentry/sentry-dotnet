using System;
using System.Threading.Tasks;

namespace Sentry.Testing
{
    public static class FuncExtensions
    {
        /// <summary>
        /// Wait for the condition to match the expected result.
        /// </summary>
        /// <param name="condition">the condition function.</param>
        /// <param name="expectedResult">the expected return of condition.</param>
        /// <param name="timeout">the Timeout where the default value is 10 seconds.</param>
        /// <returns>True if the condition output matches the expectedResult.</returns>
        public static async Task<bool> WaitConditionAsync(this Func<bool> condition, bool expectedResult,  TimeSpan? timeout = null)
        {
            var now = DateTime.UtcNow;
            timeout ??= TimeSpan.FromSeconds(10);

            while (now.Add(timeout.Value) >= DateTime.UtcNow && condition() != expectedResult) 
            {
                await Task.Delay(50);
            }

            if (condition() != expectedResult)
            {
                throw new TaskCanceledException();
            }

            return true;
        }
    }
}
