#if !NETSTANDARD2_0
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Sentry.Extensions.Logging
{
    public class NoLockInterceptor : IObserver<KeyValuePair<string, object?>>
    {
        private AsyncLocal<ISpan?> _span = new AsyncLocal<ISpan?>();
        private AsyncLocal<ISpan?> _spanConnection = new AsyncLocal<ISpan?>();
        private AsyncLocal<ISpan?> _spanContext = new AsyncLocal<ISpan?>();
        public void OnCompleted() { }

        public void OnError(Exception error) { }

        public void OnNext(KeyValuePair<string, object?> value)
        {
            if (value.Key == "Microsoft.EntityFrameworkCore.Infrastructure.ContextInitialized")
            {
                _spanContext.Value = SentrySdk.GetSpan()?.StartChild("ef.core", "context");
            }
            else if (value.Key == "Microsoft.EntityFrameworkCore.Database.Connection.ConnectionOpening")
            {
                _spanConnection.Value = StartChildrenFromSpanOrTransaction(_spanContext.Value, "connection");
            }
            else if (value.Key == "Microsoft.EntityFrameworkCore.Database.Connection.ConnectionClosed")
            {
                _spanConnection.Value?.Finish();
                _spanContext.Value?.Finish();
            }
            else if (value.Key == "Microsoft.EntityFrameworkCore.Database.Command.CommandExecuting")
            {

                _span.Value = StartChildrenFromSpanOrTransaction(_spanConnection.Value, GetLimitedQuery(value.Value?.ToString()));
            }
            else if (value.Key == "Microsoft.EntityFrameworkCore.Database.Command.CommandExecuted")
            {
                _span.Value?.Finish(status: SpanStatus.Ok);
            }
        }

        private string? GetLimitedQuery(string? value)
            => value?.Length > 512 ? value.Substring(0, 512) + "..." : value;

        private ISpan? StartChildrenFromSpanOrTransaction(ISpan? span, string? description)
            => span?.StartChild("db", description) ?? SentrySdk.GetSpan()?.StartChild("db", description);
    }
    public class SentryDiagnosticListener : IObserver<DiagnosticListener>
    {
        private readonly NoLockInterceptor _noLockInterceptor = new NoLockInterceptor();

        public void OnCompleted() { }

        public void OnError(Exception error) { }

        public void OnNext(DiagnosticListener listener)
        {
            if (listener.Name == "Microsoft.EntityFrameworkCore")
            {
                var x = listener.Subscribe(_noLockInterceptor);
            }
        }
    }
}
#endif
