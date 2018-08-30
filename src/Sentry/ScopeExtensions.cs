using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sentry.Extensibility;
using Sentry.Protocol;

namespace Sentry
{
    public static class ScopeExtensions
    {
        /// <summary>
        /// Invokes all event processor providers available
        /// </summary>
        /// <param name="options">The SentryOptions which holds the processor providers.</param>
        /// <returns></returns>
        public static IEnumerable<ISentryEventProcessor> GetAllEventProcessors(this SentryOptions options)
            => options.EventProcessorsProviders.SelectMany(p => p());

        /// <summary>
        /// Invokes all exception processor providers available
        /// </summary>
        /// <param name="options">The SentryOptions which holds the processor providers.</param>
        /// <returns></returns>
        public static IEnumerable<ISentryEventExceptionProcessor> GetAllExceptionProcessors(this SentryOptions options)
            => options.ExceptionProcessorsProviders.SelectMany(p => p());

        /// <summary>
        /// Add an exception processor
        /// </summary>
        /// <param name="scope">The SentryOptions to hold the processor.</param>
        /// <param name="processor">The exception processor.</param>
        public static void AddExceptionProcessor(this Scope scope, ISentryEventExceptionProcessor processor)
            => scope.ExceptionProcessors.Add(processor.Process);

        /// <summary>
        /// Add the exception processors
        /// </summary>
        /// <param name="scope">The SentryOptions to hold the processor.</param>
        /// <param name="processors">The exception processors.</param>
        public static void AddExceptionProcessors(this Scope scope, IEnumerable<ISentryEventExceptionProcessor> processors)
            => scope.ExceptionProcessors.AddRange(processors.Select(p => new Action<Exception, SentryEvent>(p.Process)));

        /// <summary>
        /// Adds an event processor which is invoked when creating a <see cref="SentryEvent"/>.
        /// </summary>
        /// <param name="scope">The SentryOptions to hold the processor.</param>
        /// <param name="processor">The event processor.</param>
        public static void AddEventProcessor(this Scope scope, ISentryEventProcessor processor)
            => scope.EventProcessors.Add(processor.Process);

        /// <summary>
        /// Adds event processors which are invoked when creating a <see cref="SentryEvent"/>.
        /// </summary>
        /// <param name="scope">The SentryOptions to hold the processor.</param>
        /// <param name="processors">The event processors.</param>
        public static void AddEventProcessors(this Scope scope, IEnumerable<ISentryEventProcessor> processors)
            => scope.EventProcessors.AddRange(processors.Select(p => new Func<SentryEvent, SentryEvent>(p.Process)));

        /// <summary>
        /// Adds an event processor provider which is invoked when creating a <see cref="SentryEvent"/>.
        /// </summary>
        /// <param name="scope">The SentryOptions to hold the processor provider.</param>
        /// <param name="processorProvider">The event processor provider.</param>
        public static void AddEventProcessorProvider(this Scope scope, Func<IEnumerable<ISentryEventProcessor>> processorProvider)
            => scope.EventProcessorsProviders.Add(processorProvider);

        /// <summary>
        /// Add the exception processor provider
        /// </summary>
        /// <param name="scope">The SentryOptions to hold the processor provider.</param>
        /// <param name="processorProvider">The exception processor provider.</param>
        public static void AddExceptionProcessorProvider(this Scope scope, Func<IEnumerable<ISentryEventExceptionProcessor>> processorProvider)
            => scope.ExceptionProcessorsProviders.Add(processorProvider);
    }
}
