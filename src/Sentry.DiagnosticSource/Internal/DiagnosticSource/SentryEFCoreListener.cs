using Sentry.Extensibility;

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

    private readonly AsyncLocal<WeakReference<ISpan>> _spansCompilerLocal = new();
    private readonly AsyncLocal<WeakReference<ISpan>> _spansQueryLocal = new();
    private readonly AsyncLocal<WeakReference<ISpan>> _spansConnectionLocal = new();

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

    private EFQueryCompilerDiagnosticSourceHelper QueryCompilerDiagnosticSourceHelper(object? diagnosticsSourceValue) =>
        new(_hub, _options, _spansCompilerLocal, diagnosticsSourceValue);

    private EFConnectionDiagnosticSourceHelper ConnectionDiagnosticSourceHelper(object? diagnosticsSourceValue) =>
        new(_hub, _options, _spansConnectionLocal, diagnosticsSourceValue);

    private EFCommandDiagnosticSourceHelper CommandDiagnosticSourceHelper(object? diagnosticsSourceValue) =>
        new(_hub, _options, _spansQueryLocal, diagnosticsSourceValue);

    public void OnNext(KeyValuePair<string, object?> value)
    {
        try
        {
            // Because we have to support the .NET framework, we can't get at strongly typed diagnostic source events.
            // We do know they're objects, that can be converted to strings though... and we can get the correlation
            // data we need from there by parsing the string. Not as reliable, but works against all frameworks.
            var diagnosticSourceValue = value.Value?.ToString();
            switch (value.Key)
            {
                // Query compiler span
                case EFQueryStartCompiling or EFQueryCompiling:
                    QueryCompilerDiagnosticSourceHelper(value.Value).AddSpan();
                    break;
                case EFQueryCompiled:
                    QueryCompilerDiagnosticSourceHelper(value.Value).FinishSpan(SpanStatus.Ok);
                    break;

                // Connection span (A transaction may or may not show a connection with it.)
                case EFConnectionOpening when _logConnectionEnabled:
                    ConnectionDiagnosticSourceHelper(value.Value).AddSpan();
                    break;
                case EFConnectionClosed when _logConnectionEnabled:
                    ConnectionDiagnosticSourceHelper(value.Value).FinishSpan(SpanStatus.Ok);
                    break;

                // Query Execution span
                case EFCommandExecuting when _logQueryEnabled:
                    CommandDiagnosticSourceHelper(value.Value).AddSpan();
                    break;
                case EFCommandFailed when _logQueryEnabled:
                    CommandDiagnosticSourceHelper(value.Value).FinishSpan(SpanStatus.InternalError);
                    break;
                case EFCommandExecuted when _logQueryEnabled:
                    CommandDiagnosticSourceHelper(value.Value).FinishSpan(SpanStatus.Ok);
                    break;
            }
        }
        catch (Exception ex)
        {
            _options.LogError("Failed to intercept EF Core event.", ex);
        }
    }
}
