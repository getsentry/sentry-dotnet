using System;
using System.Collections.Generic;
using Sentry.Extensibility;
using Sentry.Protocol;

namespace Sentry.Exceptions
{
    public abstract class ExceptionProcessor : ISentryEventExceptionProcessor
    {
        private readonly SentryOptions _options;

        internal static readonly string ExceptionDataTagKey = "sentry:tag:";
        internal static readonly string ExceptionDataContextKey = "sentry:context:";

        protected ExceptionProcessor(SentryOptions options) => _options = options;

        public void Process(Exception exception, SentryEvent sentryEvent)
        {
            _options.LogDebug("Running processor {0} on exception: {1}", GetType(), exception.Message);

            var sentryExceptionList = new List<SentryException>();

            foreach (var flattenedEx in Flatten(exception))
            {
                var sentryException = CreateSentryException(flattenedEx);

                sentryExceptionList.Add(sentryException);

                Process(flattenedEx, sentryException, sentryEvent);
                ProcessExceptionData(flattenedEx, sentryException, sentryEvent);
            }

            sentryEvent.SentryExceptions = sentryExceptionList;
        }

        protected abstract void Process(Exception exception, SentryException sentryException, SentryEvent sentryEvent);
        protected abstract SentryStackTrace CreateStackTrace(Exception exception);

        private void ProcessExceptionData(Exception exception, SentryException sentryException, SentryEvent sentryEvent)
        {
            int i = 0;
            foreach (var key in exception.Data.Keys)
            {
                if (key is not string keyString)
                {
                    continue;
                }

                var value = exception.Data[key];
                sentryException.Data[keyString] = value;

                // Support for Tag and Extra via Exception.Data:
                if (keyString.StartsWith("sentry:", StringComparison.OrdinalIgnoreCase) &&
                    value != null)
                {
                    if (keyString.StartsWith(ExceptionDataTagKey, StringComparison.OrdinalIgnoreCase) &&
                        value is string tagValue &&
                        ExceptionDataTagKey.Length < keyString.Length)
                    {
                        // Set the key after the ExceptionDataTagKey string.
                        sentryEvent.SetTag(keyString.Substring(ExceptionDataTagKey.Length), tagValue);
                    }
                    else if (keyString.StartsWith(ExceptionDataContextKey, StringComparison.OrdinalIgnoreCase) &&
                             ExceptionDataContextKey.Length < keyString.Length)
                    {
                        // Set the key after the ExceptionDataTagKey string.
                        _ = sentryEvent.Contexts[keyString.Substring(ExceptionDataContextKey.Length)] = value;
                    }
                    else
                    {
                        sentryEvent.SetExtra($"Exception[{i++}][{keyString}]", sentryException.Data[keyString]);
                    }
                }
                else
                {
                    sentryEvent.SetExtra($"Exception[{i++}][{keyString}]", sentryException.Data[keyString]);
                }
            }
        }

        private SentryException CreateSentryException(Exception exception)
            => new()
            {
                Type = exception.GetType()?.FullName,
                Module = exception.GetType()?.Assembly?.FullName,
                Value = exception.Message,
                ThreadId = Environment.CurrentManagedThreadId,
                Mechanism = GetMechanism(exception),
                Stacktrace = CreateStackTrace(exception)
            };

        private IEnumerable<Exception> Flatten(Exception exception)
        {
            while (true)
            {
                if (exception is AggregateException ae)
                {
                    foreach (var inner in ae.InnerExceptions)
                    {
                        yield return inner;
                    }

                    if (_options.KeepAggregateException)
                    {
                        yield return ae;
                    }
                }
                else if (exception.InnerException != null)
                {
                    exception = exception.InnerException;
                    continue;
                }
                else
                {
                    yield return exception;
                }

                break;
            }
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
