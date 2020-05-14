using System;
using System.Collections.Generic;
using System.ComponentModel;
using Sentry.Extensibility;
using Sentry.Internal;

// ReSharper disable once CheckNamespace
namespace Sentry
{
    /// <summary>
    /// Scope extensions.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class ScopeExtensions
    {
        /// <summary>
        /// Invokes all event processor providers available
        /// </summary>
        /// <param name="scope">The Scope which holds the processor providers.</param>
        /// <returns></returns>
        public static IEnumerable<ISentryEventProcessor> GetAllEventProcessors(this Scope scope)
        {
            if (scope.Options is SentryOptions options)
            {
                foreach (var processor in options.GetAllEventProcessors())
                {
                    yield return processor;
                }
            }

            foreach (var processor in scope.EventProcessors)
            {
                yield return processor;
            }
        }

        /// <summary>
        /// Invokes all exception processor providers available
        /// </summary>
        /// <param name="scope">The Scope which holds the processor providers.</param>
        /// <returns></returns>
        public static IEnumerable<ISentryEventExceptionProcessor> GetAllExceptionProcessors(this Scope scope)
        {
            if (scope.Options is SentryOptions options)
            {
                foreach (var processor in options.GetAllExceptionProcessors())
                {
                    yield return processor;
                }
            }

            foreach (var processor in scope.ExceptionProcessors)
            {
                yield return processor;
            }
        }

        /// <summary>
        /// Add an exception processor
        /// </summary>
        /// <param name="scope">The Scope to hold the processor.</param>
        /// <param name="processor">The exception processor.</param>
        public static void AddExceptionProcessor(this Scope scope, ISentryEventExceptionProcessor processor)
            => scope.ExceptionProcessors.Add(processor);

        /// <summary>
        /// Add the exception processors
        /// </summary>
        /// <param name="scope">The Scope to hold the processor.</param>
        /// <param name="processors">The exception processors.</param>
        public static void AddExceptionProcessors(this Scope scope, IEnumerable<ISentryEventExceptionProcessor> processors)
        {
            foreach (var processor in processors)
            {
                scope.ExceptionProcessors.Add(processor);
            }
        }

        /// <summary>
        /// Adds an event processor which is invoked when creating a <see cref="SentryEvent"/>.
        /// </summary>
        /// <param name="scope">The Scope to hold the processor.</param>
        /// <param name="processor">The event processor.</param>
        public static void AddEventProcessor(this Scope scope, ISentryEventProcessor processor)
            => scope.EventProcessors.Add(processor);

        /// <summary>
        /// Adds an event processor which is invoked when creating a <see cref="SentryEvent"/>.
        /// </summary>
        /// <param name="scope">The Scope to hold the processor.</param>
        /// <param name="processor">The event processor.</param>
        public static void AddEventProcessor(this Scope scope, Func<SentryEvent, SentryEvent> processor)
            => scope.AddEventProcessor(new DelegateEventProcessor(processor));

        /// <summary>
        /// Adds event processors which are invoked when creating a <see cref="SentryEvent"/>.
        /// </summary>
        /// <param name="scope">The Scope to hold the processor.</param>
        /// <param name="processors">The event processors.</param>
        public static void AddEventProcessors(this Scope scope, IEnumerable<ISentryEventProcessor> processors)
        {
            foreach (var processor in processors)
            {
                scope.EventProcessors.Add(processor);
            }
        }
    }
}
