using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Sentry.Extensibility;
using Sentry.Infrastructure;
using Sentry.Integrations;
using Sentry.Internal;
#if NETFX
using Sentry.PlatformAbstractions;
#endif

namespace Sentry
{
    /// <summary>
    /// SentryOptions extensions.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class SentryOptionsExtensions
    {
        /// <summary>
        /// Disables the strategy to detect duplicate events.
        /// </summary>
        /// <remarks>
        /// In case a second event is being sent out from the same exception, that event will be discarded.
        /// It is possible the second event had in fact more data. In which case it'd be ideal to avoid the first
        /// event going out in the first place.
        /// </remarks>
        /// <param name="options">The SentryOptions to remove the processor from.</param>
        public static void DisableDuplicateEventDetection(this SentryOptions options)
            => options.EventProcessors =
                options.EventProcessors?.Where(p => p.GetType() != typeof(DuplicateEventDetectionEventProcessor)).ToArray();

        /// <summary>
        /// Disables the capture of errors through <see cref="AppDomain.UnhandledException"/>.
        /// </summary>
        /// <param name="options">The SentryOptions to remove the integration from.</param>
        public static void DisableAppDomainUnhandledExceptionCapture(this SentryOptions options) =>
            options.RemoveIntegration<AppDomainUnhandledExceptionIntegration>();

#if NETFX
        /// <summary>
        /// Disables the list addition of .Net Frameworks into events.
        /// </summary>
        /// <param name="options">The SentryOptions to remove the integration from.</param>
        public static void DisableNetFxInstallationsIntegration(this SentryOptions options)
        {
            options.EventProcessors =
                options.EventProcessors?.Where(p => p.GetType() != typeof(NetFxInstallationsEventProcessor)).ToArray();
            options.RemoveIntegration<NetFxInstallationsIntegration>();
        }
#endif

        /// <summary>
        /// Disables the capture of errors through <see cref="AppDomain.ProcessExit"/>
        /// </summary>
        /// <param name="options">The SentryOptions to remove the integration from.</param>
        public static void DisableAppDomainProcessExitFlush(this SentryOptions options) =>
            options.RemoveIntegration<AppDomainProcessExitIntegration>();

        /// <summary>
        /// Add an integration
        /// </summary>
        /// <param name="options">The SentryOptions to hold the processor.</param>
        /// <param name="integration">The integration.</param>
        public static void AddIntegration(this SentryOptions options, ISdkIntegration integration)
            => options.Integrations = options.Integrations != null
                ? options.Integrations.Concat(new[] {integration}).ToArray()
                : new[] {integration};

        /// <summary>
        /// Removes all integrations of type <typeparamref name="TIntegration"/>.
        /// </summary>
        /// <typeparam name="TIntegration">The type of the integration(s) to remove.</typeparam>
        /// <param name="options">The SentryOptions to remove the integration(s) from.</param>
        /// <returns></returns>
        internal static void RemoveIntegration<TIntegration>(this SentryOptions options) where TIntegration : ISdkIntegration
            => options.Integrations = options.Integrations?.Where(p => p.GetType() != typeof(TIntegration)).ToArray();

        /// <summary>
        /// Add an exception filter.
        /// </summary>
        /// <param name="options">The SentryOptions to hold the processor.</param>
        /// <param name="exceptionFilter">The exception filter to add.</param>
        public static void AddExceptionFilter(this SentryOptions options, IExceptionFilter exceptionFilter)
            => options.ExceptionFilters = options.ExceptionFilters != null
                ? options.ExceptionFilters.Concat(new[] {exceptionFilter}).ToArray()
                : new[] {exceptionFilter};

        /// <summary>
        /// Ignore exception of type <typeparamref name="TException"/> or derived.
        /// </summary>
        /// <typeparam name="TException">The type of the exception to ignore.</typeparam>
        /// <param name="options">The SentryOptions to store the exceptions type ignore.</param>
        public static void AddExceptionFilterForType<TException>(this SentryOptions options) where TException : Exception
            => options.AddExceptionFilter(new ExceptionTypeFilter<TException>());

        /// <summary>
        /// Add prefix to exclude from 'InApp' stack trace list.
        /// </summary>
        public static void AddInAppExclude(this SentryOptions options, string prefix)
            => options.InAppExclude = options.InAppExclude != null
                ? options.InAppExclude.Concat(new[] {prefix}).ToArray()
                : new[] {prefix};

        /// <summary>
        /// Add prefix to include as in 'InApp' stack trace.
        /// </summary>
        public static void AddInAppInclude(this SentryOptions options, string prefix)
            => options.InAppInclude = options.InAppInclude != null
                ? options.InAppInclude.Concat(new[] {prefix}).ToArray()
                : new[] {prefix};

        /// <summary>
        /// Add an exception processor.
        /// </summary>
        /// <param name="options">The SentryOptions to hold the processor.</param>
        /// <param name="processor">The exception processor.</param>
        public static void AddExceptionProcessor(this SentryOptions options, ISentryEventExceptionProcessor processor)
            => options.ExceptionProcessors = options.ExceptionProcessors != null
                ? options.ExceptionProcessors.Concat(new[] {processor}).ToArray()
                : new[] {processor};

        /// <summary>
        /// Add the exception processors.
        /// </summary>
        /// <param name="options">The SentryOptions to hold the processor.</param>
        /// <param name="processors">The exception processors.</param>
        public static void AddExceptionProcessors(this SentryOptions options, IEnumerable<ISentryEventExceptionProcessor> processors)
            => options.ExceptionProcessors = options.ExceptionProcessors != null
                ? options.ExceptionProcessors.Concat(processors).ToArray()
                : processors.ToArray();

        /// <summary>
        /// Adds an event processor which is invoked when creating a <see cref="SentryEvent"/>.
        /// </summary>
        /// <param name="options">The SentryOptions to hold the processor.</param>
        /// <param name="processor">The event processor.</param>
        public static void AddEventProcessor(this SentryOptions options, ISentryEventProcessor processor)
            => options.EventProcessors = options.EventProcessors != null
                ? options.EventProcessors.Concat(new[] {processor}).ToArray()
                : new[] {processor};

        /// <summary>
        /// Adds event processors which are invoked when creating a <see cref="SentryEvent"/>.
        /// </summary>
        /// <param name="options">The SentryOptions to hold the processor.</param>
        /// <param name="processors">The event processors.</param>
        public static void AddEventProcessors(this SentryOptions options, IEnumerable<ISentryEventProcessor> processors)
            => options.EventProcessors = options.EventProcessors != null
                ? options.EventProcessors.Concat(processors).ToArray()
                : processors.ToArray();

        /// <summary>
        /// Adds an event processor provider which is invoked when creating a <see cref="SentryEvent"/>.
        /// </summary>
        /// <param name="options">The SentryOptions to hold the processor provider.</param>
        /// <param name="processorProvider">The event processor provider.</param>
        public static void AddEventProcessorProvider(this SentryOptions options, Func<IEnumerable<ISentryEventProcessor>> processorProvider)
            => options.EventProcessorsProviders = options.EventProcessorsProviders != null
                ? options.EventProcessorsProviders.Concat(new[] {processorProvider}).ToArray()
                : new[] {processorProvider};

        /// <summary>
        /// Add the exception processor provider.
        /// </summary>
        /// <param name="options">The SentryOptions to hold the processor provider.</param>
        /// <param name="processorProvider">The exception processor provider.</param>
        public static void AddExceptionProcessorProvider(this SentryOptions options,
            Func<IEnumerable<ISentryEventExceptionProcessor>> processorProvider)
            => options.ExceptionProcessorsProviders = options.ExceptionProcessorsProviders != null
                ? options.ExceptionProcessorsProviders.Concat(new[] {processorProvider}).ToArray()
                : new[] {processorProvider};

        /// <summary>
        /// Invokes all event processor providers available.
        /// </summary>
        /// <param name="options">The SentryOptions which holds the processor providers.</param>
        public static IEnumerable<ISentryEventProcessor> GetAllEventProcessors(this SentryOptions options)
            => options.EventProcessorsProviders?.SelectMany(p => p()) ?? Enumerable.Empty<ISentryEventProcessor>();

        /// <summary>
        /// Invokes all exception processor providers available.
        /// </summary>
        /// <param name="options">The SentryOptions which holds the processor providers.</param>
        public static IEnumerable<ISentryEventExceptionProcessor> GetAllExceptionProcessors(this SentryOptions options)
            => options.ExceptionProcessorsProviders?.SelectMany(p => p()) ?? Enumerable.Empty<ISentryEventExceptionProcessor>();

        /// <summary>
        /// Use custom <see cref="ISentryStackTraceFactory" />.
        /// </summary>
        /// <param name="options">The SentryOptions to hold the processor provider.</param>
        /// <param name="sentryStackTraceFactory">The stack trace factory.</param>
        public static SentryOptions UseStackTraceFactory(this SentryOptions options, ISentryStackTraceFactory sentryStackTraceFactory)
        {
            options.SentryStackTraceFactory = sentryStackTraceFactory ?? throw new ArgumentNullException(nameof(sentryStackTraceFactory));

            return options;
        }

        /// <summary>
        /// Applies the default tags to an event without resetting existing tags.
        /// </summary>
        /// <param name="options">The options to read the default tags from.</param>
        /// <param name="event">The event to apply the tags to.</param>
        public static void ApplyDefaultTags(this SentryOptions options, SentryEvent @event)
        {
            foreach (var defaultTag in options.DefaultTags
                .Where(t => !@event.Tags.TryGetValue(t.Key, out _)))
            {
                @event.SetTag(defaultTag.Key, defaultTag.Value);
            }
        }

        internal static void SetupLogging(this SentryOptions options)
        {
            if (options.Debug)
            {
                if (options.DiagnosticLogger == null)
                {
                    options.DiagnosticLogger = new ConsoleDiagnosticLogger(options.DiagnosticLevel);
                    options.DiagnosticLogger.LogDebug("Logging enabled with ConsoleDiagnosticLogger and min level: {0}",
                        options.DiagnosticLevel);
                }
            }
            else
            {
                options.DiagnosticLogger = null;
            }
        }
    }
}
