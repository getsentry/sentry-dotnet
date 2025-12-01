using System.Collections.Concurrent;
using System.Diagnostics;

namespace Sentry.Samples.AspNetCore.Blazor.Server.Services;

/// <summary>
/// Integrates Blazor Server activities with Sentry for enhanced telemetry.
/// Tracks circuit lifecycle events and navigation within circuits,
/// adding relevant breadcrumbs and context to Sentry events and transactions.
/// </summary>
public class BlazorSentryIntegration : IHostedService, IDisposable
{
    private readonly ActivityListener _listener;
    private readonly IHub _sentryHub;
    private readonly ILogger<BlazorSentryIntegration> _logger;
    private readonly ConcurrentDictionary<string, BlazorCircuitContext> _circuitContexts = new();

    public BlazorSentryIntegration(IHub sentryHub, ILogger<BlazorSentryIntegration> logger)
    {
        _sentryHub = sentryHub;
        _logger = logger;

        _listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name is
                "Microsoft.AspNetCore.Components" or
                "Microsoft.AspNetCore.Components.Server.Circuits",
            Sample = (ref ActivityCreationOptions<ActivityContext> options) =>
                ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = OnActivityStarted,
            ActivityStopped = OnActivityStopped,
        };
    }

    private Timer? _cleanupTimer;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        ActivitySource.AddActivityListener(_listener);
        _logger.LogInformation("Blazor Sentry activity listener started");

        // Set up a timer to periodically clean up stale circuit contexts.
        // This helps prevent memory leaks in long-running Blazor Server applications.
        _cleanupTimer = new Timer(
            _ => PurgeStaleCircuits(TimeSpan.FromHours(24)),
            null,
            TimeSpan.FromHours(1),
            TimeSpan.FromHours(1)
        );

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Blazor Sentry activity listener stopping");

        Interlocked.Exchange(ref _cleanupTimer, null)?.Dispose();

        return Task.CompletedTask;
    }

    // Called by SentryCircuitHandler
    public void OnCircuitOpened(string circuitId)
    {
        _circuitContexts[circuitId] = new BlazorCircuitContext
        {
            CircuitId = circuitId,
            StartedAt = DateTimeOffset.UtcNow
        };

        _sentryHub.AddBreadcrumb(
            message: "Circuit opened",
            category: "blazor.circuit",
            data: new Dictionary<string, string> { ["circuit_id"] = circuitId },
            level: BreadcrumbLevel.Info);

        _logger.LogDebug("Circuit {CircuitId} opened", circuitId);
    }

    // Called by SentryCircuitHandler
    public void OnCircuitClosed(string circuitId)
    {
        if (_circuitContexts.TryRemove(circuitId, out var context))
        {
            var duration = DateTimeOffset.UtcNow - context.StartedAt;

            _sentryHub.AddBreadcrumb(
                message: "Circuit closed",
                category: "blazor.circuit",
                data: new Dictionary<string, string>
                {
                    ["circuit_id"] = circuitId,
                    ["duration_seconds"] = duration.TotalSeconds.ToString("F1")
                },
                level: BreadcrumbLevel.Info);

            _logger.LogDebug("Circuit {CircuitId} closed after {Duration}", circuitId, duration);
        }
    }

    private void OnActivityStarted(Activity activity)
    {
        _logger.LogDebug("Activity started: {OperationName} - {DisplayName}",
            activity.OperationName, activity.DisplayName);
    }

    private void OnActivityStopped(Activity activity)
    {
        var circuitId = GetCircuitId(activity);

        switch (activity.OperationName)
        {
            case "Microsoft.AspNetCore.Components.Navigate":
                HandleNavigation(activity, circuitId);
                break;

            case "Microsoft.AspNetCore.Components.HandleEvent":
                HandleEvent(activity, circuitId);
                break;
        }

        var errorType = activity.GetTagItem("error.type")?.ToString();
        if (!string.IsNullOrEmpty(errorType))
        {
            HandleActivityError(activity, circuitId, errorType);
        }
    }

    private void HandleNavigation(Activity activity, string? circuitId)
    {
        var route = activity.GetTagItem("aspnetcore.components.route")?.ToString();
        var componentType = activity.GetTagItem("aspnetcore.components.type")?.ToString();

        if (string.IsNullOrEmpty(route) && string.IsNullOrEmpty(componentType))
        {
            (route, componentType) = ParseNavigationDisplayName(activity.DisplayName);
        }

        _logger.LogDebug("Navigation: {Route} -> {Component}", route, componentType);

        if (circuitId != null && _circuitContexts.TryGetValue(circuitId, out var context))
        {
            context.CurrentRoute = route;
            context.CurrentComponent = componentType;
        }

        _sentryHub.ConfigureScope(scope =>
        {
            scope.TransactionName = route ?? $"Blazor/{GetShortTypeName(componentType)}";
            scope.SetTag("blazor.route", route ?? "unknown");
            scope.SetTag("blazor.component", componentType ?? "unknown");

            if (circuitId != null)
            {
                scope.SetTag("blazor.circuit_id", circuitId);
            }
        });

        _sentryHub.AddBreadcrumb(
            message: $"Navigated to {route}",
            category: "navigation",
            data: new Dictionary<string, string>
            {
                ["route"] = route ?? "unknown",
                ["component"] = componentType ?? "unknown"
            },
            level: BreadcrumbLevel.Info);
    }

    private void HandleEvent(Activity activity, string? circuitId)
    {
        var attributeName = activity.GetTagItem("aspnetcore.components.attribute.name")?.ToString();
        var methodName = activity.GetTagItem("code.function.name")?.ToString();
        var targetComponent = activity.GetTagItem("aspnetcore.components.type")?.ToString();

        if (string.IsNullOrEmpty(attributeName))
        {
            (attributeName, targetComponent, methodName) = ParseEventDisplayName(activity.DisplayName);
        }

        var category = attributeName?.ToLowerInvariant() switch
        {
            "onclick" => "ui.click",
            "onchange" => "ui.input",
            "onsubmit" => "ui.submit",
            "onfocus" or "onblur" => "ui.focus",
            "onkeydown" or "onkeyup" or "onkeypress" => "ui.keyboard",
            _ => "ui.event"
        };

        _sentryHub.AddBreadcrumb(
            message: $"{attributeName} -> {GetShortTypeName(targetComponent)}.{methodName}",
            category: category,
            data: new Dictionary<string, string>
            {
                ["attribute"] = attributeName ?? "unknown",
                ["method"] = methodName ?? "unknown",
                ["component"] = targetComponent ?? "unknown"
            },
            level: BreadcrumbLevel.Info);
    }

    private void HandleActivityError(Activity activity, string? circuitId, string errorType)
    {
        var context = circuitId != null ? _circuitContexts.GetValueOrDefault(circuitId) : null;

        _sentryHub.ConfigureScope(scope =>
        {
            scope.SetTag("error.type", errorType);

            if (context == null) return;

            scope.TransactionName = context.CurrentRoute ?? "unknown";
            scope.SetTag("blazor.route", context.CurrentRoute ?? "unknown");
            scope.SetTag("blazor.component", context.CurrentComponent ?? "unknown");
        });
    }

    private static string? GetCircuitId(Activity activity)
    {
        var circuitId = activity.GetTagItem("aspnetcore.components.circuit.id")?.ToString();
        if (!string.IsNullOrEmpty(circuitId))
            return circuitId;

        var parent = activity.Parent;
        while (parent != null)
        {
            circuitId = parent.GetTagItem("aspnetcore.components.circuit.id")?.ToString();
            if (!string.IsNullOrEmpty(circuitId))
                return circuitId;
            parent = parent.Parent;
        }

        return null;
    }

    private static (string? route, string? componentType) ParseNavigationDisplayName(string? displayName)
    {
        if (string.IsNullOrEmpty(displayName)) return (null, null);

        const string prefix = "Route ";
        if (!displayName.StartsWith(prefix)) return (null, null);

        var withoutPrefix = displayName[prefix.Length..];
        var arrowIndex = withoutPrefix.IndexOf(" -> ", StringComparison.Ordinal);
        if (arrowIndex < 0) return (null, null);

        return (withoutPrefix[..arrowIndex], withoutPrefix[(arrowIndex + 4)..]);
    }

    private static (string? attributeName, string? componentType, string? methodName) ParseEventDisplayName(string? displayName)
    {
        if (string.IsNullOrEmpty(displayName)) return (null, null, null);

        const string prefix = "Event ";
        if (!displayName.StartsWith(prefix)) return (null, null, null);

        var withoutPrefix = displayName[prefix.Length..];
        var arrowIndex = withoutPrefix.IndexOf(" -> ", StringComparison.Ordinal);
        if (arrowIndex < 0) return (null, null, null);

        var attributeName = withoutPrefix[..arrowIndex];
        var remainder = withoutPrefix[(arrowIndex + 4)..];
        var lastDot = remainder.LastIndexOf('.');

        if (lastDot < 0) return (attributeName, remainder, null);

        return (attributeName, remainder[..lastDot], remainder[(lastDot + 1)..]);
    }

    private static string GetShortTypeName(string? fullTypeName)
    {
        if (string.IsNullOrEmpty(fullTypeName)) return "unknown";
        var lastDot = fullTypeName.LastIndexOf('.');
        return lastDot >= 0 ? fullTypeName[(lastDot + 1)..] : fullTypeName;
    }

    private void PurgeStaleCircuits(TimeSpan maxAge)
    {
        var cutoff = DateTimeOffset.UtcNow - maxAge;
        var staleCircuits = _circuitContexts
            .Where(kvp => kvp.Value.StartedAt < cutoff)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var circuitId in staleCircuits)
        {
            if (_circuitContexts.TryRemove(circuitId, out var context))
            {
                _logger.LogWarning("Purged stale circuit {CircuitId} started at {StartedAt}",
                    circuitId, context.StartedAt);
            }
        }
    }

    public void Dispose()
    {
        Interlocked.Exchange(ref _cleanupTimer, null)?.Dispose();
        _listener.Dispose();
        _circuitContexts.Clear();
    }
}



public class BlazorCircuitContext
{
    public required string CircuitId { get; init; }
    public DateTimeOffset StartedAt { get; init; }
    public string? CurrentRoute { get; set; }
    public string? CurrentComponent { get; set; }
}
