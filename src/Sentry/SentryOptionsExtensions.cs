using Sentry.Extensibility;
using Sentry.Infrastructure;
using Sentry.Integrations;
using Sentry.Internal;
using Sentry.Internal.Extensions;

#if NETFRAMEWORK
using Sentry.PlatformAbstractions;
#endif

#if HAS_DIAGNOSTIC_INTEGRATION
using Sentry.Internal.DiagnosticSource;
#endif

namespace Sentry;

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
        => options.RemoveEventProcessor<DuplicateEventDetectionEventProcessor>();

    /// <summary>
    /// Disables the capture of errors through <see cref="AppDomain.UnhandledException"/>.
    /// </summary>
    /// <param name="options">The SentryOptions to remove the integration from.</param>
    public static void DisableAppDomainUnhandledExceptionCapture(this SentryOptions options) =>
        options.RemoveDefaultIntegration(SentryOptions.DefaultIntegrations.AppDomainUnhandledExceptionIntegration);

#if HAS_DIAGNOSTIC_INTEGRATION
    /// <summary>
    /// Disables the integrations with Diagnostic source.
    /// </summary>
    /// <param name="options">The SentryOptions to remove the integration from.</param>
    public static void DisableDiagnosticSourceIntegration(this SentryOptions options)
        => options.RemoveDefaultIntegration(SentryOptions.DefaultIntegrations.SentryDiagnosticListenerIntegration);
#endif

    /// <summary>
    /// Disables the capture of errors through <see cref="TaskScheduler.UnobservedTaskException"/>.
    /// </summary>
    /// <param name="options">The SentryOptions to remove the integration from.</param>
    [Obsolete("Method has been renamed to DisableUnobservedTaskExceptionCapture.  Please update usage.")]
    public static void DisableTaskUnobservedTaskExceptionCapture(this SentryOptions options) =>
        options.DisableUnobservedTaskExceptionCapture();

    /// <summary>
    /// Disables the capture of errors through <see cref="TaskScheduler.UnobservedTaskException"/>.
    /// </summary>
    /// <param name="options">The SentryOptions to remove the integration from.</param>
    public static void DisableUnobservedTaskExceptionCapture(this SentryOptions options) =>
        options.RemoveDefaultIntegration(SentryOptions.DefaultIntegrations.UnobservedTaskExceptionIntegration);

#if NETFRAMEWORK
    /// <summary>
    /// Disables the list addition of .Net Frameworks into events.
    /// </summary>
    /// <param name="options">The SentryOptions to remove the integration from.</param>
    public static void DisableNetFxInstallationsIntegration(this SentryOptions options)
    {
        options.RemoveEventProcessor<NetFxInstallationsEventProcessor>();
        options.RemoveDefaultIntegration(SentryOptions.DefaultIntegrations.NetFxInstallationsIntegration);
    }
#endif

    /// <summary>
    /// By default, any queued events (i.e: captures errors) are flushed on <see cref="AppDomain.ProcessExit"/>.
    /// This method disables that behaviour.
    /// </summary>
    /// <param name="options">The SentryOptions to remove the integration from.</param>
    public static void DisableAppDomainProcessExitFlush(this SentryOptions options) =>
        options.RemoveDefaultIntegration(SentryOptions.DefaultIntegrations.AppDomainProcessExitIntegration);

#if NET5_0_OR_GREATER
    /// <summary>
    /// Disables WinUI exception handler
    /// </summary>
    /// <param name="options">The SentryOptions to remove the integration from.</param>
    public static void DisableWinUiUnhandledExceptionIntegration(this SentryOptions options)
        => options.RemoveDefaultIntegration(SentryOptions.DefaultIntegrations.WinUiUnhandledExceptionIntegration);
#endif

    /// <summary>
    /// Add an integration
    /// </summary>
    /// <param name="options">The SentryOptions to hold the processor.</param>
    /// <param name="integration">The integration.</param>
    public static void AddIntegration(this SentryOptions options, ISdkIntegration integration)
        => options.AddIntegration(integration);

    /// <summary>
    /// Removes all integrations of type <typeparamref name="TIntegration"/>.
    /// </summary>
    /// <typeparam name="TIntegration">The type of the integration(s) to remove.</typeparam>
    /// <param name="options">The SentryOptions to remove the integration(s) from.</param>
    public static void RemoveIntegration<TIntegration>(this SentryOptions options)
        where TIntegration : ISdkIntegration
        => options.RemoveIntegration<TIntegration>();

    /// <summary>
    /// Add an exception filter.
    /// </summary>
    /// <param name="options">The SentryOptions to hold the processor.</param>
    /// <param name="exceptionFilter">The exception filter to add.</param>
    public static void AddExceptionFilter(this SentryOptions options, IExceptionFilter exceptionFilter)
    {
        if (options.ExceptionFilters == null)
        {
            options.ExceptionFilters = new () {exceptionFilter};
        }
        else
        {
            options.ExceptionFilters.Add(exceptionFilter);
        }
    }

    /// <summary>
    /// Removes all filters of type <typeparamref name="TFilter"/>
    /// </summary>
    /// <typeparam name="TFilter">The type of filter(s) to remove.</typeparam>
    /// <param name="options">The SentryOptions to remove the filter(s) from.</param>
    public static void RemoveExceptionFilter<TFilter>(this SentryOptions options)
        where TFilter : IExceptionFilter
        => options.ExceptionFilters?.RemoveAll(filter => filter is TFilter);

    /// <summary>
    /// Ignore exception of type <typeparamref name="TException"/> or derived.
    /// </summary>
    /// <typeparam name="TException">The type of the exception to ignore.</typeparam>
    /// <param name="options">The SentryOptions to store the exceptions type ignore.</param>
    public static void AddExceptionFilterForType<TException>(this SentryOptions options)
        where TException : Exception
        => options.AddExceptionFilter(new ExceptionTypeFilter<TException>());

    /// <summary>
    /// Add prefix to exclude from 'InApp' stacktrace list.
    /// </summary>
    /// <param name="options">The SentryOptions which holds the stacktrace list.</param>
    /// <param name="prefix">The string used to filter the stacktrace to be excluded from InApp.</param>
    /// <remarks>
    /// Sentry by default filters the stacktrace to display only application code.
    /// A user can optionally click to see all which will include framework and libraries.
    /// A <see cref="string.StartsWith(string)"/> is executed
    /// </remarks>
    /// <example>
    /// 'System.', 'Microsoft.'
    /// </example>
    public static void AddInAppExclude(this SentryOptions options, string prefix)
    {
        if (options.InAppExclude == null)
        {
            options.InAppExclude = new () {prefix};
        }
        else
        {
            options.InAppExclude.Add(prefix);
        }
    }

    /// <summary>
    /// Add prefix to include as in 'InApp' stacktrace.
    /// </summary>
    /// <param name="options">The SentryOptions which holds the stacktrace list.</param>
    /// <param name="prefix">The string used to filter the stacktrace to be included in InApp.</param>
    /// <remarks>
    /// Sentry by default filters the stacktrace to display only application code.
    /// A user can optionally click to see all which will include framework and libraries.
    /// A <see cref="string.StartsWith(string)"/> is executed
    /// </remarks>
    /// <example>
    /// 'System.CustomNamespace', 'Microsoft.Azure.App'
    /// </example>
    public static void AddInAppInclude(this SentryOptions options, string prefix)
    {
        if (options.InAppInclude == null)
        {
            options.InAppInclude = new () {prefix};
        }
        else
        {
            options.InAppInclude.Add(prefix);
        }
    }

    /// <summary>
    /// Add an exception processor.
    /// </summary>
    /// <param name="options">The SentryOptions to hold the processor.</param>
    /// <param name="processor">The exception processor.</param>
    public static void AddExceptionProcessor(this SentryOptions options, ISentryEventExceptionProcessor processor)
    {
        options.ExceptionProcessors.Add((processor.GetType(), new Lazy<ISentryEventExceptionProcessor>(() => processor)));
    }

    /// <summary>
    /// Add the exception processors.
    /// </summary>
    /// <param name="options">The SentryOptions to hold the processor.</param>
    /// <param name="processors">The exception processors.</param>
    public static void AddExceptionProcessors(this SentryOptions options, IEnumerable<ISentryEventExceptionProcessor> processors)
    {
        foreach (var processor in processors)
        {
            AddExceptionProcessor(options, processor);
        }
    }

    /// <summary>
    /// Adds an event processor which is invoked when creating a <see cref="SentryEvent"/>.
    /// </summary>
    /// <param name="options">The SentryOptions to hold the processor.</param>
    /// <param name="processor">The event processor.</param>
    public static void AddEventProcessor(this SentryOptions options, ISentryEventProcessor processor)
    {
        options.EventProcessors.Add((processor.GetType(), new Lazy<ISentryEventProcessor>(() => processor)));
    }

    /// <summary>
    /// Adds event processors which are invoked when creating a <see cref="SentryEvent"/>.
    /// </summary>
    /// <param name="options">The SentryOptions to hold the processor.</param>
    /// <param name="processors">The event processors.</param>
    public static void AddEventProcessors(this SentryOptions options, IEnumerable<ISentryEventProcessor> processors)
    {
        foreach (var processor in processors)
        {
            AddEventProcessor(options, processor);
        }
    }

    /// <summary>
    /// Removes all event processors of type <typeparamref name="TProcessor"/>
    /// </summary>
    /// <typeparam name="TProcessor">The type of processor(s) to remove.</typeparam>
    /// <param name="options">The SentryOptions to remove the processor(s) from.</param>
    public static void RemoveEventProcessor<TProcessor>(this SentryOptions options)
        where TProcessor : ISentryEventProcessor
        => options.EventProcessors.RemoveAll(processor => processor.Type == typeof(TProcessor));

    /// <summary>
    /// Adds an event processor provider which is invoked when creating a <see cref="SentryEvent"/>.
    /// </summary>
    /// <param name="options">The SentryOptions to hold the processor provider.</param>
    /// <param name="processorProvider">The event processor provider.</param>
    public static void AddEventProcessorProvider(this SentryOptions options, Func<IEnumerable<ISentryEventProcessor>> processorProvider)
    {
        options.EventProcessorsProviders.Add(processorProvider);
    }

    /// <summary>
    /// Adds an transaction processor which is invoked when creating a <see cref="Transaction"/>.
    /// </summary>
    /// <param name="options">The SentryOptions to hold the processor.</param>
    /// <param name="processor">The transaction processor.</param>
    public static void AddTransactionProcessor(this SentryOptions options, ISentryTransactionProcessor processor)
    {
        if (options.TransactionProcessors == null)
        {
            options.TransactionProcessors = new() {processor};
        }
        else
        {
            options.TransactionProcessors.Add(processor);
        }
    }

    /// <summary>
    /// Adds transaction processors which are invoked when creating a <see cref="Transaction"/>.
    /// </summary>
    /// <param name="options">The SentryOptions to hold the processor.</param>
    /// <param name="processors">The transaction processors.</param>
    public static void AddTransactionProcessors(this SentryOptions options, IEnumerable<ISentryTransactionProcessor> processors)
    {
        if (options.TransactionProcessors == null)
        {
            options.TransactionProcessors = processors.ToList();
        }
        else
        {
            options.TransactionProcessors.AddRange(processors);
        }
    }

    /// <summary>
    /// Removes all transaction processors of type <typeparamref name="TProcessor"/>
    /// </summary>
    /// <typeparam name="TProcessor">The type of processor(s) to remove.</typeparam>
    /// <param name="options">The SentryOptions to remove the processor(s) from.</param>
    public static void RemoveTransactionProcessor<TProcessor>(this SentryOptions options)
        where TProcessor : ISentryTransactionProcessor
        => options.TransactionProcessors?.RemoveAll(processor => processor is TProcessor);

    /// <summary>
    /// Adds an transaction processor provider which is invoked when creating a <see cref="Transaction"/>.
    /// </summary>
    /// <param name="options">The SentryOptions to hold the processor provider.</param>
    /// <param name="processorProvider">The transaction processor provider.</param>
    public static void AddTransactionProcessorProvider(this SentryOptions options, Func<IEnumerable<ISentryTransactionProcessor>> processorProvider)
        => options.TransactionProcessorsProviders = options.TransactionProcessorsProviders.Concat(new[] { processorProvider }).ToList();

    /// <summary>
    /// Add the exception processor provider.
    /// </summary>
    /// <param name="options">The SentryOptions to hold the processor provider.</param>
    /// <param name="processorProvider">The exception processor provider.</param>
    public static void AddExceptionProcessorProvider(this SentryOptions options,
        Func<IEnumerable<ISentryEventExceptionProcessor>> processorProvider)
    {
        options.ExceptionProcessorsProviders.Add(processorProvider);
    }

    /// <summary>
    /// Invokes all event processor providers available.
    /// </summary>
    /// <param name="options">The SentryOptions which holds the processor providers.</param>
    public static IEnumerable<ISentryEventProcessor> GetAllEventProcessors(this SentryOptions options)
        => options.EventProcessorsProviders.SelectMany(p => p());

    /// <summary>
    /// Invokes all transaction processor providers available.
    /// </summary>
    /// <param name="options">The SentryOptions which holds the processor providers.</param>
    public static IEnumerable<ISentryTransactionProcessor> GetAllTransactionProcessors(this SentryOptions options)
        => options.TransactionProcessorsProviders.SelectMany(p => p());

    /// <summary>
    /// Invokes all exception processor providers available.
    /// </summary>
    /// <param name="options">The SentryOptions which holds the processor providers.</param>
    public static IEnumerable<ISentryEventExceptionProcessor> GetAllExceptionProcessors(this SentryOptions options)
        => options.ExceptionProcessorsProviders.SelectMany(p => p());

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
    /// <param name="hasTags">The event to apply the tags to.</param>
    public static void ApplyDefaultTags(this SentryOptions options, IHasTags hasTags)
    {
        foreach (var defaultTag in options.DefaultTags
            .Where(t => !hasTags.Tags.TryGetValue(t.Key, out _)))
        {
            hasTags.SetTag(defaultTag.Key, defaultTag.Value);
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

    internal static string? TryGetDsnSpecificCacheDirectoryPath(this SentryOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.CacheDirectoryPath))
        {
            return null;
        }

        // DSN must be set to use caching
        var dsn = options.Dsn;
        if (string.IsNullOrWhiteSpace(dsn))
        {
            return null;
        }

        return Path.Combine(options.CacheDirectoryPath, "Sentry", dsn.GetHashString());
    }

    internal static string? TryGetProcessSpecificCacheDirectoryPath(this SentryOptions options)
    {
        // In the future, this will most likely contain process ID
        return options.TryGetDsnSpecificCacheDirectoryPath();
    }
}
