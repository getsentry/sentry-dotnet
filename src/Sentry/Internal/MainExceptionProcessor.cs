using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Sentry.Extensibility;
using Sentry.Protocol;

namespace Sentry.Internal
{
    internal class MainExceptionProcessor : ISentryEventExceptionProcessor
    {
        private readonly SentryOptions _options;
        private readonly ISentryStackTraceFactory _sentryStackTraceFactory;

        public MainExceptionProcessor(SentryOptions options, ISentryStackTraceFactory sentryStackTraceFactory)
        {
            Debug.Assert(options != null);
            Debug.Assert(sentryStackTraceFactory != null);
            _options = options;
            _sentryStackTraceFactory = sentryStackTraceFactory;
        }

        public void Process(Exception exception, SentryEvent sentryEvent)
        {
            Debug.Assert(sentryEvent != null);

            _options.DiagnosticLogger?.LogDebug("Running processor on exception: {0}", exception.Message);

            if (exception != null)
            {
                var sentryExceptions = CreateSentryException(exception)
                    // Otherwise realization happens on the worker thread before sending event.
                    .ToList();

                MoveExceptionExtrasToEvent(sentryEvent, sentryExceptions);

                sentryEvent.SentryExceptionValues = new SentryValues<SentryException>(sentryExceptions);
            }
        }

        // SentryException.Extra is not supported by Sentry yet.
        // Move the extras to the Event Extra while marking
        // by index the Exception which owns it
        private static void MoveExceptionExtrasToEvent(
            SentryEvent sentryEvent,
            IReadOnlyList<SentryException> sentryExceptions)
        {
            for (var i = 0; i < sentryExceptions.Count; i++)
            {
                var sentryException = sentryExceptions[i];

                if (!(sentryException.Data?.Count > 0))
                {
                    continue;
                }

                foreach (var key in sentryException.Data.Keys)
                {
                    sentryEvent.SetExtra($"Exception[{i}][{key}]", sentryException.Data[key]);
                }
            }
        }

        internal IEnumerable<SentryException> CreateSentryException(Exception exception)
        {
            Debug.Assert(exception != null);

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
                ThreadId = Thread.CurrentThread.ManagedThreadId,
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

            sentryEx.Stacktrace = _sentryStackTraceFactory.Create(exception);

            yield return sentryEx;
        }

        internal static Mechanism GetMechanism(Exception exception)
        {
            Debug.Assert(exception != null);

            Mechanism mechanism = null;

            if (exception.HelpLink != null)
            {
                mechanism = new Mechanism
                {
                    HelpLink = exception.HelpLink
                };
            }

            return mechanism;
        }
    }
}
