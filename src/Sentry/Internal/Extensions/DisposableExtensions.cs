using System;
using System.Collections.Generic;

namespace Sentry.Internal.Extensions
{
    internal static class DisposableExtensions
    {
        public static void DisposeAll(this IEnumerable<IDisposable> disposables)
        {
            List<Exception>? exceptions = null;

            foreach (var i in disposables)
            {
                try
                {
                    i.Dispose();
                }
                catch (Exception ex)
                {
                    exceptions ??= new List<Exception>();
                    exceptions.Add(ex);
                }
            }

            if (exceptions?.Count > 0)
            {
                throw new AggregateException(exceptions);
            }
        }
    }
}
