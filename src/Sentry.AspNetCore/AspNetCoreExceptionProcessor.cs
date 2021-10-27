using System;
using System.Linq;
using Sentry.Extensibility;
using Sentry.Protocol;

namespace Sentry.AspNetCore
{
    internal class AspNetCoreExceptionProcessor : ISentryEventExceptionProcessor
    {
        public void Process(Exception exception, SentryEvent @event)
        {
            // Mark events collected from the exception handler middlewares via logging as unhandled
            if (@event.Logger is "Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware" or "Microsoft.AspNetCore.Diagnostics.DeveloperExceptionPageMiddleware")
            {
                exception.Data.Add(Mechanism.HandledKey, false);
                exception.Data.Add(Mechanism.Mechanism.MechanismKey, event.Logger.Substring(input.LastIndexOf('.') + 1);
                if (@event.SentryExceptions != null)
                {
                    foreach (var ex in @event.SentryExceptions.Where(x => x.Mechanism != null))
                    {
                        ex.Mechanism!.Handled = false;
                    }
                }
            }
        }
    }
}
