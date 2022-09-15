using System;
using System.Collections.Generic;
using System.Linq;
using Sentry.Extensibility;
using Sentry.Internal.Extensions;
using Sentry.Protocol;

namespace Sentry.Internal
{
    internal class MainExceptionProcessor : ISentryEventExceptionProcessor
    {
        internal static readonly string ExceptionDataTagKey = "sentry:tag:";
        internal static readonly string ExceptionDataContextKey = "sentry:context:";

        private readonly SentryOptions _options;
        internal Func<ISentryStackTraceFactory> SentryStackTraceFactoryAccessor { get; }

        public MainExceptionProcessor(SentryOptions options, Func<ISentryStackTraceFactory> sentryStackTraceFactoryAccessor)
        {
            _options = options;
            SentryStackTraceFactoryAccessor = sentryStackTraceFactoryAccessor;
        }

        public void Process(Exception exception, SentryEvent sentryEvent)
        {
            _options.LogDebug("Running processor on exception: {0}", exception.Message);

            var sentryExceptions = CreateSentryException(exception)
                // Otherwise realization happens on the worker thread before sending event.
                .ToList();

            MoveExceptionExtrasToEvent(sentryEvent, sentryExceptions);

            sentryEvent.SentryExceptions = sentryExceptions;
        }

        // SentryException.Extra is not supported by Sentry yet.
        // Move the extras to the Event Extra while marking
        // by index the Exception which owns it.
        private static void MoveExceptionExtrasToEvent(
            SentryEvent sentryEvent,
            IReadOnlyList<SentryException> sentryExceptions)
        {
            for (var i = 0; i < sentryExceptions.Count; i++)
            {
                var sentryException = sentryExceptions[i];

                if (sentryException.Data.Count <= 0)
                {
                    continue;
                }

                foreach (var keyValue in sentryException.Data)
                {
                    if (keyValue.Key.StartsWith("sentry:", StringComparison.OrdinalIgnoreCase) &&
                        keyValue.Value != null)
                    {
                        if (keyValue.Key.StartsWith(ExceptionDataTagKey, StringComparison.OrdinalIgnoreCase) &&
                            keyValue.Value is string tagValue &&
                            ExceptionDataTagKey.Length < keyValue.Key.Length)
                        {
                            // Set the key after the ExceptionDataTagKey string.
                            sentryEvent.SetTag(keyValue.Key.Substring(ExceptionDataTagKey.Length), tagValue);
                        }
                        else if (keyValue.Key.StartsWith(ExceptionDataContextKey, StringComparison.OrdinalIgnoreCase) &&
                            ExceptionDataContextKey.Length < keyValue.Key.Length)
                        {
                            // Set the key after the ExceptionDataTagKey string.
                            _ = sentryEvent.Contexts[keyValue.Key.Substring(ExceptionDataContextKey.Length)] = keyValue.Value;
                        }
                        else
                        {
                            sentryEvent.SetExtra($"Exception[{i}][{keyValue.Key}]", sentryException.Data[keyValue.Key]);
                        }
                    }
                    else
                    {
                        sentryEvent.SetExtra($"Exception[{i}][{keyValue.Key}]", sentryException.Data[keyValue.Key]);
                    }
                }
            }
        }

        internal IEnumerable<SentryException> CreateSentryException(Exception exception)
        {
            var exceptions = exception
                .EnumerateChainedExceptions(_options)
                .Select(BuildSentryException)
                .ToList();

            // If we've filtered out the aggregate exception, we'll need to copy over details from it.
            if (exception is AggregateException && !_options.KeepAggregateException)
            {
                var original = BuildSentryException(exception);

                // Exceptions are sent from oldest to newest, so the details belong on the LAST exception.
                var last = exceptions.Last();
                last.Stacktrace = original.Stacktrace;
                last.Mechanism = original.Mechanism;
                original.Data.TryCopyTo(last.Data);
            }

            return exceptions;
        }

        private SentryException BuildSentryException(Exception innerException)
        {
            var sentryEx = new SentryException
            {
                Type = innerException.GetType().FullName,
                Module = innerException.GetType().Assembly.FullName,
                Value = innerException.Message,
                ThreadId = Environment.CurrentManagedThreadId,
                Mechanism = GetMechanism(innerException)
            };

            if (innerException.Data.Count != 0)
            {
                foreach (var key in innerException.Data.Keys)
                {
                    if (key is string keyString)
                    {
                        sentryEx.Data[keyString] = innerException.Data[key];
                    }
                }
            }

            sentryEx.Stacktrace = SentryStackTraceFactoryAccessor().Create(innerException);
            return sentryEx;
        }

        internal static Mechanism GetMechanism(Exception exception)
        {
            var mechanism = new Mechanism();

            if (exception.HelpLink != null)
            {
                mechanism.HelpLink = exception.HelpLink;
            }

            if (exception.Data[Mechanism.HandledKey] is bool handled)
            {
                mechanism.Handled = handled;
                exception.Data.Remove(Mechanism.HandledKey);
            }

            if (exception.Data[Mechanism.MechanismKey] is string mechanismName)
            {
                mechanism.Type = mechanismName;
                exception.Data.Remove(Mechanism.MechanismKey);
            }

            return mechanism;
        }
    }
}
