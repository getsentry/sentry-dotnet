using System;
using System.Collections.Generic;
using System.Threading;

namespace Sentry.Extensions.Logging
{
    internal enum SentryEFSpanType
    {
        Context,
        Connection,
        Query
    };
    /// <summary>
          /// Class that consumes EntityFrameworkCore events
          /// </summary>
    internal class SentryEFCoreInterceptor : IObserver<KeyValuePair<string, object?>>
    {


        internal const string EFContextInitializedKey = "Microsoft.EntityFrameworkCore.Infrastructure.ContextInitialized";
        internal const string EFContextDisposedKey = "EntityFrameworkCore.Infrastructure.ContextDisposed";
        internal const string EFConnectionOpening = "Microsoft.EntityFrameworkCore.Database.Connection.ConnectionOpening";
        internal const string EFConnectionClosed = "Microsoft.EntityFrameworkCore.Database.Connection.ConnectionClosed";
        internal const string EFCommandExecuting = "Microsoft.EntityFrameworkCore.Database.Command.CommandExecuting";
        internal const string EFCommandExecuted = "Microsoft.EntityFrameworkCore.Database.Command.CommandExecuted";

        private IHub _hub { get; }
        private AsyncLocal<ISpan?> _contextSpan = new();
        private AsyncLocal<ISpan?> _connectionSpan = new();
        private AsyncLocal<ISpan?> _querySpan = new();

        private void SetSpan(SentryEFSpanType type, ISpan? span)
        {
            switch (type)
            {
                case SentryEFSpanType.Context:
                    _contextSpan.Value = span;
                    break;
                case SentryEFSpanType.Connection:
                    _connectionSpan.Value = span;
                    break;
                default:
                    _querySpan.Value = span;
                    break;
            }
        }

        private ISpan? GetSpan(SentryEFSpanType type)
            => type switch
            {
                SentryEFSpanType.Context => _contextSpan.Value,
                SentryEFSpanType.Connection => _connectionSpan.Value,
                _ => _querySpan.Value
            };

        public SentryEFCoreInterceptor(IHub maHub)
        {
            _hub = maHub;
        }

        public void OnCompleted() { }

        public void OnError(Exception error) { }

        public void OnNext(KeyValuePair<string, object?> value)
        {
            if (value.Key == EFContextInitializedKey)
            {
                SetSpan(SentryEFSpanType.Context, _hub.GetSpan()?.StartChild("ef.core", "Opening EF Core context."));
            }
            else if (value.Key == EFConnectionOpening)
            {
                SetSpan(SentryEFSpanType.Connection, _hub.GetSpan()?.StartChild("db", "connection"));
            }
            else if (value.Key == EFCommandExecuting)
            {
                SetSpan(SentryEFSpanType.Query, _hub.GetSpan()?.StartChild("db", null));
            }
            else if (value.Key == EFCommandExecuted &&
                GetSpan(SentryEFSpanType.Query) is { } querySpan)
            {
                querySpan.Description = value.Value?.ToString();
                querySpan.Finish(SpanStatus.Ok);
            }
            else if (value.Key == EFConnectionClosed &&
                     GetSpan(SentryEFSpanType.Connection) is { } connectionSpan)
            {
                connectionSpan.Finish(SpanStatus.Ok);
                // We finish it here because the transaction will be dispsed once the context is ended.
                GetSpan(SentryEFSpanType.Context)?.Finish(SpanStatus.Ok);
            }
        }
    }
}
