using Sentry.Extensibility;
using Sentry.Protocol;

namespace Sentry.Internal.DiagnosticSource;

/// <summary>
/// Class that consumes Entity Framework Core events.
/// </summary>
internal class SentryEFCoreListener : IObserver<KeyValuePair<string, object?>>
{
    internal const string EFConnectionOpening = "Microsoft.EntityFrameworkCore.Database.Connection.ConnectionOpening";
    internal const string EFConnectionClosed = "Microsoft.EntityFrameworkCore.Database.Connection.ConnectionClosed";
    internal const string EFCommandExecuting = "Microsoft.EntityFrameworkCore.Database.Command.CommandExecuting";
    internal const string EFCommandExecuted = "Microsoft.EntityFrameworkCore.Database.Command.CommandExecuted";
    internal const string EFCommandFailed = "Microsoft.EntityFrameworkCore.Database.Command.CommandError";

    internal static readonly Origin EFCoreListenerOrigin = "auto.db.ef-core-listener";

    /// <summary>
    /// Used for EF Core 2.X and 3.X.
    /// <seealso href="https://docs.microsoft.com/dotnet/api/microsoft.entityframeworkcore.diagnostics.coreeventid.querymodelcompiling?view=efcore-3.1"/>
    /// </summary>
    internal const string EFQueryStartCompiling = "Microsoft.EntityFrameworkCore.Query.QueryCompilationStarting";

    /// <summary>
    /// Used for EF Core 2.X and 3.X.
    /// <seealso href="https://docs.microsoft.com/dotnet/api/microsoft.entityframeworkcore.diagnostics.coreeventid.querymodelcompiling?view=efcore-3.1"/>
    /// </summary>
    internal const string EFQueryCompiling = "Microsoft.EntityFrameworkCore.Query.QueryModelCompiling";
    internal const string EFQueryCompiled = "Microsoft.EntityFrameworkCore.Query.QueryExecutionPlanned";

    private readonly IHub _hub;
    private readonly SentryOptions _options;

    private bool _logConnectionEnabled = true;
    private bool _logQueryEnabled = true;

    public SentryEFCoreListener(IHub hub, SentryOptions options)
    {
        _hub = hub;
        _options = options;
    }

    internal void DisableConnectionSpan() => _logConnectionEnabled = false;

    internal void DisableQuerySpan() => _logQueryEnabled = false;

    public void OnCompleted() { }

    public void OnError(Exception error) { }

    private EFQueryCompilerDiagnosticSourceHelper QueryCompilerDiagnosticSourceHelper => new(_hub, _options);

    private EFConnectionDiagnosticSourceHelper ConnectionDiagnosticSourceHelper => new(_hub, _options);

    private EFCommandDiagnosticSourceHelper CommandDiagnosticSourceHelper => new(_hub, _options);

    public void OnNext(KeyValuePair<string, object?> value)
    {
        try
        {
            switch (value.Key)
            {
                // Query compiler span
                case EFQueryStartCompiling or EFQueryCompiling:
                    QueryCompilerDiagnosticSourceHelper.AddSpan(value.Value);
                    break;
                case EFQueryCompiled:
                    QueryCompilerDiagnosticSourceHelper.FinishSpan(value.Value, SpanStatus.Ok);
                    break;

                // Connection span (A transaction may or may not show a connection with it.)
                case EFConnectionOpening when _logConnectionEnabled:
                    ConnectionDiagnosticSourceHelper.AddOrReuseSpan(value.Value);
                    break;
                case EFConnectionClosed when _logConnectionEnabled:
                    ConnectionDiagnosticSourceHelper.FinishSpan(value.Value, SpanStatus.Ok);
                    break;

                // Query Execution span
                case EFCommandExecuting when _logQueryEnabled:
                    CommandDiagnosticSourceHelper.AddSpan(value.Value);
                    break;
                case EFCommandFailed when _logQueryEnabled:
                    CommandDiagnosticSourceHelper.FinishSpan(value.Value, SpanStatus.InternalError);
                    break;
                case EFCommandExecuted when _logQueryEnabled:
                    CommandDiagnosticSourceHelper.FinishSpan(value.Value, SpanStatus.Ok);
                    break;
            }
        }
        catch (Exception ex)
        {
            _options.LogError(ex, "Failed to intercept EF Core event.");
        }
    }
}
