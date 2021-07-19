using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Sentry.Extensions.Logging
{
    public class NoLockInterceptor : IObserver<KeyValuePair<string, object?>>
    {
        internal const string EFContextInitializedKey = "Microsoft.EntityFrameworkCore.Infrastructure.ContextInitialized";
        internal const string EFContextDisposedKey = "EntityFrameworkCore.Infrastructure.ContextDisposed";
        internal const string EFConnectionOpening = "Microsoft.EntityFrameworkCore.Database.Connection.ConnectionOpening";
        internal const string EFConnectionClosed = "Microsoft.EntityFrameworkCore.Database.Connection.ConnectionClosed";
        internal const string EFCommandExecuting = "Microsoft.EntityFrameworkCore.Database.Connection.ConnectionClosed";
        internal const string EFCommandExecuted = "Microsoft.EntityFrameworkCore.Database.Command.CommandExecuted";

        public void OnCompleted() { }

        public void OnError(Exception error) { }

        public void OnNext(KeyValuePair<string, object?> value)
        {
            if (value.Key == EFContextInitializedKey)
            {
                SentrySdk.GetSpan()?.StartChild("ef.core", "Opening EF Core context.");
            }
            else if (value.Key == EFConnectionOpening)
            {
                SentrySdk.GetSpan()?.StartChild("db", "connection");
            }
            else if (value.Key == EFCommandExecuting)
            {
                SentrySdk.GetSpan()?.StartChild("db", value.Value?.ToString());
            }
            else if (value.Key == EFCommandExecuted &&
                     SentrySdk.GetSpan() is { } querySpan &&
                     querySpan.Operation == "db" &&
                     querySpan.Description != "connection")

            {
                querySpan.Finish(status: SpanStatus.Ok);
            }
            else if (value.Key == EFConnectionClosed &&
                     SentrySdk.GetSpan() is { } connectionSpan &&
                     connectionSpan.Operation == "db" &&
                     connectionSpan.Description == "connection"
                )
            {
                connectionSpan.Finish();
            }
            else if (value.Key == EFContextDisposedKey &&
                     SentrySdk.GetSpan() is { } contextSpan &&
                     contextSpan.Operation == "ef.core"
                )
            {
                contextSpan.Finish();
            }
        }
    }
    /// <summary>
    /// Class that subscribes to specific listeners from DiagnosticListener.
    /// </summary>
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
