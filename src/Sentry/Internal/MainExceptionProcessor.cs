using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Sentry.Extensibility;
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
            if (exception is AggregateException ae)
            {
                foreach (var inner in ae.InnerExceptions.SelectMany(CreateSentryException))
                {
                    yield return inner;
                }
            }
            else if (exception.InnerException != null)
            {
                foreach (var inner in CreateSentryException(exception.InnerException))
                {
                    yield return inner;
                }
            }

            var sentryEx = new SentryException
            {
                Type = exception.GetType()?.FullName,
                Module = exception.GetType()?.Assembly?.FullName,
                Value = exception.Message,
                ThreadId = Environment.CurrentManagedThreadId,
                Mechanism = GetMechanism(exception)
            };

            if (exception.Data.Count != 0)
            {
                foreach (var key in exception.Data.Keys)
                {
                    if (key is string keyString)
                    {
                        sentryEx.Data[keyString] = exception.Data[key];
                    }
                }
            }

            sentryEx.Stacktrace = SentryStackTraceFactoryAccessor().Create(exception);

            yield return sentryEx;
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
