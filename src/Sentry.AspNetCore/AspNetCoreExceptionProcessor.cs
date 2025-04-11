using Sentry.Extensibility;
using Sentry.Protocol;

namespace Sentry.AspNetCore;

internal class AspNetCoreExceptionProcessor : ISentryEventExceptionProcessor
{
    public void Process(Exception exception, SentryEvent @event)
    {
        // Mark events collected from the exception handler middlewares via logging as unhandled
        if (@event.Logger is "Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware" or "Microsoft.AspNetCore.Diagnostics.DeveloperExceptionPageMiddleware")
        {
            if (@event.SentryExceptions != null)
            {
                foreach (var ex in @event.SentryExceptions)
                {
                    ex.Mechanism ??= new Mechanism();
                    ex.Mechanism.Type = "ExceptionHandlerMiddleware";
                    ex.Mechanism.Handled = false;
                }
            }
        }
    }
}
