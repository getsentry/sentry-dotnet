using System;
using System.Collections.Generic;
using Sentry.Internal;

namespace Sentry.Extensibility
{
    /// <summary>
    /// TODO
    /// </summary>
    public static class ExceptionExtensions
    {
        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public static void AddSentryTag(this Exception ex, string name, string value)
            => ex.Data.Add($"{MainExceptionProcessor.ExceptionDataTagKey}{name}", value);

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="name"></param>
        /// <param name="data"></param>
        public static void AddSentryContext(this Exception ex, string name, Dictionary<string, object> data)
            => ex.Data.Add($"{MainExceptionProcessor.ExceptionDataContextKey}{name}", data);
    }
}
