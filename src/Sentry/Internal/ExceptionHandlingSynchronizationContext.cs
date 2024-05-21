namespace Sentry.Internal;

/// <summary>
/// See https://www.jamescrosswell.dev/posts/catching-async-void-exceptions/ for a detailed explanation
/// </summary>
internal class ExceptionHandlingSynchronizationContext(Action<Exception> exceptionHandler, SynchronizationContext? innerContext)
    : SynchronizationContext
{
    public override void Post(SendOrPostCallback d, object? state)
    {
        if (state is ExceptionDispatchInfo exceptionInfo)
        {
            exceptionHandler(exceptionInfo.SourceException);
            return;
        }
        if (innerContext != null)
        {
            innerContext.Post(d, state);
            return;
        }
        base.Post(d, state);
    }
}
