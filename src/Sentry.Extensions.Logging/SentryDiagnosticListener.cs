using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Sentry.Extensions.Logging
{
    /// <summary>
    /// Class that consumes EntityFrameworkCore events
    /// </summary>
    internal class NoLockInterceptor : IObserver<KeyValuePair<string, object?>>
    {
        internal const string EFContextInitializedKey = "Microsoft.EntityFrameworkCore.Infrastructure.ContextInitialized";
        internal const string EFContextDisposedKey = "EntityFrameworkCore.Infrastructure.ContextDisposed";
        internal const string EFConnectionOpening = "Microsoft.EntityFrameworkCore.Database.Connection.ConnectionOpening";
        internal const string EFConnectionClosed = "Microsoft.EntityFrameworkCore.Database.Connection.ConnectionClosed";
        internal const string EFCommandExecuting = "Microsoft.EntityFrameworkCore.Database.Command.CommandExecuting";
        internal const string EFCommandExecuted = "Microsoft.EntityFrameworkCore.Database.Command.CommandExecuted";

        private AsyncLocal<ISpan?> _contextSpan = new();

        public void OnCompleted() { }

        public void OnError(Exception error) { }

        public void OnNext(KeyValuePair<string, object?> value)
        {
            if (value.Key == EFContextInitializedKey)
            {
                _contextSpan.Value = SentrySdk.GetSpan()?.StartChild("ef.core", "Opening EF Core context.");
            }
            else if (value.Key == EFConnectionOpening)
            {
                SentrySdk.GetSpan()?.StartChild("db", "connection");
            }
            else if (value.Key == EFCommandExecuting)
            {
                SentrySdk.GetSpan()?.StartChild("db", null);
            }
            else if (value.Key == EFCommandExecuted &&
                     SentrySdk.GetSpan() is { } querySpan &&
                     querySpan.Operation == "db" &&
                     querySpan.Description is null)

            {
                querySpan.Description = value.Value?.ToString();
                querySpan.Finish(status: SpanStatus.Ok);
            }
            else if (value.Key == EFConnectionClosed &&
                     SentrySdk.GetSpan() is { } connectionSpan &&
                     connectionSpan.Operation == "db" &&
                     connectionSpan.Description == "connection"
                )
            {
                connectionSpan.Finish();
                // We finish it here because the transaction will be dispsed once the context is ended.
                _contextSpan.Value?.Finish();
            }
        }
    }

    /// <summary>
    /// Class that subscribes to specific listeners from DiagnosticListener.
    /// </summary>
    internal class SentryDiagnosticListener : IObserver<DiagnosticListener>
    {
        private readonly NoLockInterceptor _noLockInterceptor = new();

        public void OnCompleted() { }

        public void OnError(Exception error) { }

        public void OnNext(DiagnosticListener listener)
        {
            if (listener.Name == "Microsoft.EntityFrameworkCore")
            {
                listener.Subscribe(_noLockInterceptor);
            }
        }
    }
}
