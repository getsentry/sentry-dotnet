using Sentry.Extensibility;

namespace Sentry.Samples.AspNetCore.Blazor.Server.Services;

/// <summary>
/// This event processor adds Blazor-specific context to Sentry events
/// and improves transaction naming for Blazor SignalR requests.
/// </summary>
public class BlazorEventProcessor : ISentryEventProcessor
{
    public SentryEvent Process(SentryEvent @event)
    {
        // Add Blazor-specific context to the event
        if (@event.Tags.ContainsKey("blazor.circuit_id"))
        {
            @event.SetExtra("blazor", new
            {
                Route = @event.Tags.GetValueOrDefault("blazor.route"),
                Component = @event.Tags.GetValueOrDefault("blazor.component"),
                CircuitId = @event.Tags.GetValueOrDefault("blazor.circuit_id")
            });
        }

        // For transaction name, we need to set it through Contexts
        var transactionName = @event.Contexts.Trace?.Operation;
        if (!IsBlazorSignalRTransaction(transactionName))
        {
            return @event;
        }

        var betterName = GetBetterTransactionName(@event);
        @event.SetTag("transaction.original", transactionName ?? "unknown");
        @event.SetTag("transaction.name", betterName);

        return @event;
    }

    private static bool IsBlazorSignalRTransaction(string? transaction)
    {
        if (string.IsNullOrEmpty(transaction))
        {
            return false;
        }

        return transaction.Contains("_blazor", StringComparison.OrdinalIgnoreCase) ||
               transaction.Contains("negotiate", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetBetterTransactionName(SentryEvent @event)
    {
        if (@event.Tags.TryGetValue("blazor.route", out var route) &&
            !string.IsNullOrEmpty(route))
        {
            return route;
        }

        if (@event.Tags.TryGetValue("blazor.component", out var component) &&
            !string.IsNullOrEmpty(component))
        {
            var lastDot = component.LastIndexOf('.');
            var shortName = lastDot >= 0 ? component[(lastDot + 1)..] : component;
            return $"Blazor/{shortName}";
        }

        return "Blazor/Unknown";
    }
}
