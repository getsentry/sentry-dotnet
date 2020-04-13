using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Sentry.Extensibility;
using Sentry.Infrastructure;
using Sentry.Integrations;
using Sentry.Internal;

namespace Sentry
{
    /// <summary>
    /// SentryOptions extensions.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class SentryOptionsExtensions
    {
        /// <summary>
        /// Disables the strategy to detect duplicate events
        /// </summary>
        /// <remarks>
        /// In case a second event is being sent out from the same exception, that event will be discarded.
        /// It is possible the second event had in fact more data. In which case it'd be ideal to avoid the first
        /// event going out in the first place.
        /// </remarks>
        /// <param name="options">The SentryOptions to remove the processor from.</param>
        public static void DisableDuplicateEventDetection(this SentryOptions options)
            => options.EventProcessors = options.EventProcessors.RemoveAll(p => p.GetType() == typeof(DuplicateEventDetectionEventProcessor));

        /// <summary>
        /// Disables the capture of errors through <see cref="AppDomain.UnhandledException"/>
        /// </summary>
        /// <param name="options">The SentryOptions to remove the integration from.</param>
        public static void DisableAppDomainUnhandledExceptionCapture(this SentryOptions options) => options.RemoveIntegration<AppDomainUnhandledExceptionIntegration>();

        /// <summary>
        /// Disables the capture of errors through <see cref="AppDomain.ProcessExit"/>
        /// </summary>
        /// <param name="options">The SentryOptions to remove the integration from.</param>
        public static void DisableAppDomainProcessExitFlush(this SentryOptions options) => options.RemoveIntegration<AppDomainProcessExitIntegration>();

        /// <summary>
        /// Add an integration
        /// </summary>
        /// <param name="options">The SentryOptions to hold the processor.</param>
        /// <param name="integration">The integration.</param>
        public static void AddIntegration(this SentryOptions options, ISdkIntegration integration)
            => options.Integrations = options.Integrations.Add(integration);

        /// <summary>
        /// Removes all integrations of type <typeparamref name="TIntegration"/>.
        /// </summary>
        /// <typeparam name="TIntegration">The type of the integration(s) to remove.</typeparam>
        /// <param name="options">The SentryOptions to remove the integration(s) from.</param>
        /// <returns></returns>
        internal static void RemoveIntegration<TIntegration>(this SentryOptions options) where TIntegration : ISdkIntegration
            => options.Integrations = options.Integrations.RemoveAll(p => p.GetType() == typeof(TIntegration));

        /// <summary>
        /// Add prefix to exclude from 'InApp' stack trace list
        /// </summary>
        /// <param name="options"></param>
        /// <param name="prefix"></param>
        public static void AddInAppExclude(this SentryOptions options, string prefix)
            => options.InAppExclude = options.InAppExclude.Add(prefix);

        /// <summary>
        /// Add prefix to include as in 'InApp' stack trace.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="prefix"></param>
        public static void AddInAppInclude(this SentryOptions options, string prefix)
            => options.InAppInclude = options.InAppInclude.Add(prefix);

        /// <summary>
        /// Add an exception processor
        /// </summary>
        /// <param name="options">The SentryOptions to hold the processor.</param>
        /// <param name="processor">The exception processor.</param>
        public static void AddExceptionProcessor(this SentryOptions options, ISentryEventExceptionProcessor processor)
            => options.ExceptionProcessors = options.ExceptionProcessors.Add(processor);

        /// <summary>
        /// Add the exception processors
        /// </summary>
        /// <param name="options">The SentryOptions to hold the processor.</param>
        /// <param name="processors">The exception processors.</param>
        public static void AddExceptionProcessors(this SentryOptions options, IEnumerable<ISentryEventExceptionProcessor> processors)
            => options.ExceptionProcessors = options.ExceptionProcessors.AddRange(processors);

        /// <summary>
        /// Adds an event processor which is invoked when creating a <see cref="SentryEvent"/>.
        /// </summary>
        /// <param name="options">The SentryOptions to hold the processor.</param>
        /// <param name="processor">The event processor.</param>
        public static void AddEventProcessor(this SentryOptions options, ISentryEventProcessor processor)
            => options.EventProcessors = options.EventProcessors.Add(processor);

        /// <summary>
        /// Adds event processors which are invoked when creating a <see cref="SentryEvent"/>.
        /// </summary>
        /// <param name="options">The SentryOptions to hold the processor.</param>
        /// <param name="processors">The event processors.</param>
        public static void AddEventProcessors(this SentryOptions options, IEnumerable<ISentryEventProcessor> processors)
            => options.EventProcessors = options.EventProcessors.AddRange(processors);

        /// <summary>
        /// Adds an event processor provider which is invoked when creating a <see cref="SentryEvent"/>.
        /// </summary>
        /// <param name="options">The SentryOptions to hold the processor provider.</param>
        /// <param name="processorProvider">The event processor provider.</param>
        public static void AddEventProcessorProvider(this SentryOptions options, Func<IEnumerable<ISentryEventProcessor>> processorProvider)
            => options.EventProcessorsProviders = options.EventProcessorsProviders.Add(processorProvider);

        /// <summary>
        /// Add the exception processor provider
        /// </summary>
        /// <param name="options">The SentryOptions to hold the processor provider.</param>
        /// <param name="processorProvider">The exception processor provider.</param>
        public static void AddExceptionProcessorProvider(this SentryOptions options, Func<IEnumerable<ISentryEventExceptionProcessor>> processorProvider)
            => options.ExceptionProcessorsProviders = options.ExceptionProcessorsProviders.Add(processorProvider);

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
        /// Use custom <see cref="ISentryStackTraceFactory" />
        /// </summary>
        /// <param name="options">The SentryOptions to hold the processor provider.</param>
        /// <param name="sentryStackTraceFactory">The stack trace factory.</param>
        public static SentryOptions UseStackTraceFactory(this SentryOptions options, ISentryStackTraceFactory sentryStackTraceFactory)
        {
            options.SentryStackTraceFactory = sentryStackTraceFactory ?? throw new ArgumentNullException(nameof(sentryStackTraceFactory));

            return options;
        }

        internal static void SetupLogging(this SentryOptions options)
        {
            if (options.Debug)
            {
                if (options.DiagnosticLogger == null)
                {
#if SYSTEM_WEB && DEBUG
                    // ReSharper disable once ConditionIsAlwaysTrueOrFalse - Hosted under IIS/IIS Express: use System.Diagnostic.Debug instead of System.Console.
                    if (System.Web.HttpRuntime.AppDomainAppId != null)
                    {
                        options.DiagnosticLogger = new DebugDiagnosticLogger(options.DiagnosticsLevel);
                    }
                    else
                    {
                        options.DiagnosticLogger = new ConsoleDiagnosticLogger(options.DiagnosticsLevel);
                    }
#else
                    options.DiagnosticLogger = new ConsoleDiagnosticLogger(options.DiagnosticsLevel);
#endif
                    options.DiagnosticLogger?.LogDebug("Logging enabled with ConsoleDiagnosticLogger and min level: {0}", options.DiagnosticsLevel);
                }
            }
            else
            {
                options.DiagnosticLogger = null;
            }
        }

    }
}
