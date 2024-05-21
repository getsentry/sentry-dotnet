using Sentry.Internal;

namespace Sentry;

/// <summary>
/// Utility class for running `async void` methods safely.
/// </summary>
public static class AsyncVoid
{
    /// <summary>
    /// Runs an `async void` method safely.
    /// </summary>
    /// <param name="task">Typically either a method group or an async lambda that executes some async void code</param>
    /// <param name="handler">
    /// An optional callback that will be run if an exception is thrown. If no callback is provided then by default the
    /// exception will be captured and sent to Sentry.
    /// </param>
    /// <example>
    /// <code>
    /// AsyncVoid.RunSafely(async () => await MyAsyncMethod(), ex => Console.WriteLine(ex.Message));
    /// </code>
    /// </example>
    public static void RunSafely(Action task, Action<Exception>? handler = null)
    {
        var syncCtx = SynchronizationContext.Current;
        try
        {
            handler ??= DefaultExceptionHandler;
            SynchronizationContext.SetSynchronizationContext(new ExceptionHandlingSynchronizationContext(handler, syncCtx));
            task();
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(syncCtx);
        }
    }

    private static void DefaultExceptionHandler(Exception ex)
    {
        SentrySdk.CaptureException(ex);
        // Note we explicitly don't rethrow here... doing so would crash the program if the exception was raised in the
        // context of an async void method
    }
}
