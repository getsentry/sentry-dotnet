using Sentry.Extensibility;
using Sentry.Internal.Extensions;

namespace Sentry.Internal.DiagnosticSource;

internal abstract class EFDiagnosticSourceHelper
{
    protected SentryOptions Options { get; }
    protected ITransactionTracer? Transaction { get; }
    protected abstract string Operation { get; }

    protected abstract string? GetDescription(object? diagnosticSourceValue);

    protected string? GetDatabaseName(object? diagnosticSourceValue) =>
        diagnosticSourceValue?.GetStringProperty("Connection.Database", Options.DiagnosticLogger);

    protected string? GetDatabaseSystem(object? diagnosticSourceValue)
    {
        var providerName = diagnosticSourceValue?.GetStringProperty("Context.Database.ProviderName", Options.DiagnosticLogger);
        if (providerName is null)
        {
            return null;
        }

        return DatabaseProviderSystems.ProviderSystems.GetValueOrDefault(providerName);
    }

    protected string? GetDatabaseServerAddress(object? diagnosticSourceValue) =>
        diagnosticSourceValue?.GetStringProperty("Connection.DataSource", Options.DiagnosticLogger);

    internal EFDiagnosticSourceHelper(IHub hub, SentryOptions options)
    {
        Options = options;
        Transaction = hub.GetTransactionIfSampled();
    }

    protected static Guid? TryGetConnectionId(ISpan span) => span.Extra.TryGetValue<string, Guid?>(EFKeys.DbConnectionId);

    protected Guid? GetConnectionId(object? diagnosticSourceValue) => diagnosticSourceValue?.GetGuidProperty("ConnectionId", Options.DiagnosticLogger);

    protected static void SetConnectionId(ISpan span, Guid? connectionId)
    {
        Debug.Assert(connectionId != Guid.Empty);

        span.SetExtra(EFKeys.DbConnectionId, connectionId);
    }

    internal void AddSpan(object? diagnosticSourceValue)
    {
        Options.LogDebug($"(Sentry add span {Operation})");
        LogTransactionSpans();
        if (Transaction == null)
        {
            return;
        }

        // We "flatten" the EF spans so that they all have the same parent span, for two reasons:
        // 1. Each command typically gets it's own connection, which makes the resulting waterfall diagram hard to read.
        // 2. Sentry's performance errors functionality only works when all queries have the same parent span.
        var parent = Transaction.GetDbParentSpan();
        var child = parent.StartChild(Operation, GetDescription(diagnosticSourceValue));
        child.SetOrigin(SentryEFCoreListener.EFCoreListenerOrigin);

        SetDbData(child, diagnosticSourceValue);
        SetSpanReference(child, diagnosticSourceValue);
    }

    internal void FinishSpan(object? diagnosticSourceValue, SpanStatus status)
    {
        if (Transaction == null)
        {
            return;
        }

        Options.LogDebug($"(Sentry finish span {Operation})");
        LogTransactionSpans();

        var sourceSpan = GetSpanReference(Transaction, diagnosticSourceValue);
        if (sourceSpan == null)
        {
            Options.LogWarning("Tried to close {0} span but no matching span could be found.", Operation);
            return;
        }

        sourceSpan.Finish(status);
    }

    protected void SetDbData(ISpan span, object? diagnosticSourceValue)
    {
        if (GetDatabaseName(diagnosticSourceValue) is { } dataBaseName)
        {
            span.SetExtra(OTelKeys.DbName, dataBaseName);
        }

        if (GetDatabaseSystem(diagnosticSourceValue) is { } databaseProviderName)
        {
            span.SetExtra(OTelKeys.DbSystem, databaseProviderName);
        }

        if (GetDatabaseServerAddress(diagnosticSourceValue) is { } databaseServerAddress)
        {
            span.SetExtra(OTelKeys.DbServer, databaseServerAddress);
        }
    }

    protected void LogTransactionSpans()
    {
        if (Transaction == null)
        {
            Options.LogDebug($"(Sentry transaction is null)");
            return;
        }

        Options.LogDebug("Transaction Spans");
        Options.LogDebug("-----------------");
        foreach (var span in Transaction.Spans)
        {
            Options.LogDebug($"id: {span.SpanId} operation: {span.Operation}");
        }
    }

    /// <summary>
    /// Get the Query with error message and remove the unneeded values.
    /// </summary>
    /// <example>
    /// Compiling query model:
    /// EF initialize...\r\nEF Query...
    /// becomes:
    /// EF Query...
    /// </example>
    /// <param name="value">the query to be parsed value</param>
    /// <returns>the filtered query</returns>
    internal static string? FilterNewLineValue(object? value)
    {
        var str = value?.ToString();
        return str?[(str.IndexOf('\n') + 1)..];
    }

    protected abstract ISpan? GetSpanReference(ITransactionTracer transaction, object? diagnosticSourceValue);

    protected abstract void SetSpanReference(ISpan span, object? diagnosticSourceValue);
}
