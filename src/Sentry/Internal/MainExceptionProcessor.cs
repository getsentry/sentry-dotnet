using System;
using System.Collections.Generic;
using System.Linq;
using Sentry.Exceptions;
using Sentry.Extensibility;
using Sentry.Protocol;

namespace Sentry.Internal
{
    internal class MainExceptionProcessor : ExceptionProcessor
    {
        private readonly SentryOptions _options;
        internal Func<ISentryStackTraceFactory> SentryStackTraceFactoryAccessor { get; }

        public MainExceptionProcessor(SentryOptions options, Func<ISentryStackTraceFactory> sentryStackTraceFactoryAccessor)
            : base(options)
        {
            _options = options;
            SentryStackTraceFactoryAccessor = sentryStackTraceFactoryAccessor;
        }

        protected override void Process(Exception exception, SentryException sentryException, SentryEvent sentryEvent)
        {
            throw new NotImplementedException();
        }

        protected override SentryStackTrace CreateStackTrace(Exception exception)
            => SentryStackTraceFactoryAccessor().Create(exception);
    }
}
