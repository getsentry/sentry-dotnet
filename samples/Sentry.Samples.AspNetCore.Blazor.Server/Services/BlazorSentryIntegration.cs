using System.Collections.Concurrent;
using System.Diagnostics;

namespace Sentry.Samples.AspNetCore.Blazor.Server.Services;

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
            ShouldListenTo = source => source.Name is "Microsoft.AspNetCore.Components" or "Microsoft.AspNetCore.Components.Server.Circuits",
            Sample = (ref ActivityCreationOptions<ActivityContext> options) =>
                ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = OnActivityStarted,
            ActivityStopped = OnActivityStopped
        };
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        ActivitySource.AddActivityListener(_listener);
        _logger.LogInformation("Blazor Sentry activity listener started");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Dispose();
        return Task.CompletedTask;
    }

    private void OnActivityStarted(Activity activity)
    {
        _logger.LogDebug("Activity started: {OperationName} - {DisplayName}",
            activity.OperationName, activity.DisplayName);
    }

    private void OnActivityStopped(Activity activity)
    {
        // Tags should be populated by the time the activity stops
        var circuitId = GetCircuitId(activity);

        switch (activity.OperationName)
        {
            case "Microsoft.AspNetCore.Components.StartCircuit":
                HandleCircuitStart(activity, circuitId);
                break;

            case "Microsoft.AspNetCore.Components.Navigate":
                HandleNavigation(activity, circuitId);
                break;

            case "Microsoft.AspNetCore.Components.HandleEvent":
                HandleEvent(activity, circuitId);
                break;
        }

        // Check for errors
        var errorType = activity.GetTagItem("error.type")?.ToString();
        if (!string.IsNullOrEmpty(errorType))
        {
            HandleActivityError(activity, circuitId, errorType);
        }
    }

    private void HandleCircuitStart(Activity activity, string? circuitId)
    {
        circuitId ??= activity.GetTagItem("aspnetcore.components.circuit.id")?.ToString();

        if (string.IsNullOrEmpty(circuitId))
        {
            return;
        }

        _circuitContexts[circuitId] = new BlazorCircuitContext
        {
            CircuitId = circuitId,
            StartedAt = DateTimeOffset.UtcNow
        };

        _sentryHub.ConfigureScope(scope =>
        {
            scope.SetTag("blazor.circuit_id", circuitId);
        });

        _sentryHub.AddBreadcrumb(
            message: "Circuit started",
            category: "blazor.circuit",
            data: new Dictionary<string, string> { ["circuit_id"] = circuitId },
            level: BreadcrumbLevel.Info);
    }

    private void HandleNavigation(Activity activity, string? circuitId)
    {
        // Names are from here:
        // https://learn.microsoft.com/en-us/aspnet/core/blazor/performance/?view=aspnetcore-10.0#blazor-tracing
        var route = activity.GetTagItem("aspnetcore.components.route")?.ToString();
        var componentType = activity.GetTagItem("aspnetcore.components.type")?.ToString();

        // Fallback to parsing DisplayName if tags aren't populated
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
        // Names are from here:
        // https://learn.microsoft.com/en-us/aspnet/core/blazor/performance/?view=aspnetcore-10.0#blazor-tracing
        var attributeName = activity.GetTagItem("aspnetcore.components.attribute.name")?.ToString();
        var methodName = activity.GetTagItem("code.function.name")?.ToString();
        var targetComponent = activity.GetTagItem("aspnetcore.components.type")?.ToString();

        // Fallback to parsing DisplayName if tags aren't populated
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

            if (context == null)
            {
                return;
            }

            scope.TransactionName = context.CurrentRoute ?? "unknown";
            scope.SetTag("blazor.route", context.CurrentRoute ?? "unknown");
            scope.SetTag("blazor.component", context.CurrentComponent ?? "unknown");
        });
    }

    private static string? GetCircuitId(Activity activity)
    {
        var circuitId = activity.GetTagItem("aspnetcore.components.circuit.id")?.ToString();
        if (!string.IsNullOrEmpty(circuitId))
        {
            return circuitId;
        }

        var parent = activity.Parent;
        while (parent != null)
        {
            circuitId = parent.GetTagItem("aspnetcore.components.circuit.id")?.ToString();
            if (!string.IsNullOrEmpty(circuitId))
            {
                return circuitId;
            }

            parent = parent.Parent;
        }

        return null;
    }

    private static (string? route, string? componentType) ParseNavigationDisplayName(string? displayName)
    {
        if (string.IsNullOrEmpty(displayName))
        {
            return (null, null);
        }

        const string prefix = "Route ";
        if (!displayName.StartsWith(prefix))
        {
            return (null, null);
        }

        var withoutPrefix = displayName[prefix.Length..];
        var arrowIndex = withoutPrefix.IndexOf(" -> ", StringComparison.Ordinal);

        if (arrowIndex < 0)
        {
            return (null, null);
        }

        var route = withoutPrefix[..arrowIndex];
        var componentType = withoutPrefix[(arrowIndex + 4)..];

        return (route, componentType);
    }

    private static (string? attributeName, string? componentType, string? methodName) ParseEventDisplayName(string? displayName)
    {
        if (string.IsNullOrEmpty(displayName))
        {
            return (null, null, null);
        }

        const string prefix = "Event ";
        if (!displayName.StartsWith(prefix))
        {
            return (null, null, null);
        }

        var withoutPrefix = displayName[prefix.Length..];
        var arrowIndex = withoutPrefix.IndexOf(" -> ", StringComparison.Ordinal);

        if (arrowIndex < 0)
        {
            return (null, null, null);
        }

        var attributeName = withoutPrefix[..arrowIndex];
        var remainder = withoutPrefix[(arrowIndex + 4)..];

        var lastDot = remainder.LastIndexOf('.');
        if (lastDot < 0)
        {
            return (attributeName, remainder, null);
        }

        var componentType = remainder[..lastDot];
        var methodName = remainder[(lastDot + 1)..];

        return (attributeName, componentType, methodName);
    }

    private static string GetShortTypeName(string? fullTypeName)
    {
        if (string.IsNullOrEmpty(fullTypeName))
        {
            return "unknown";
        }

        var lastDot = fullTypeName.LastIndexOf('.');
        return lastDot >= 0 ? fullTypeName[(lastDot + 1)..] : fullTypeName;
    }

    public void Dispose()
    {
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
