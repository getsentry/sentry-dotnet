using System;
using System.Diagnostics;
using Sentry.Internal;
using Sentry.Protocol;

namespace Sentry.Ben.Demystifier
{
    internal class AsyncStackTraceFactory : SentryStackTraceFactory
    {
        public AsyncStackTraceFactory(SentryOptions options) : base(options) { }

        protected override StackTrace CreateStackTrace(Exception exception, bool isCurrentStackTrace) =>
            isCurrentStackTrace ? new StackTrace(true) : new EnhancedStackTrace(exception);

        protected override SentryStackFrame CreateFrame(StackFrame stackFrame, bool isCurrentStackTrace) =>
            InternalCreateFrame(stackFrame, isCurrentStackTrace);
    }
}
