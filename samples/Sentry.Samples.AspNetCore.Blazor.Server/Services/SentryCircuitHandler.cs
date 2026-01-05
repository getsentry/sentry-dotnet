using Microsoft.AspNetCore.Components.Server.Circuits;

namespace Sentry.Samples.AspNetCore.Blazor.Server.Services;

/// <summary>
/// This class hooks into Blazor Server's circuit lifecycle events
/// to notify the <see cref="BlazorSentryIntegration"/> about circuit openings and closures.
/// This helps clean up the circuit contexts to avoid memory leaks in long-running Blazor Server applications.
/// </summary>
public class SentryCircuitHandler : CircuitHandler
{
    private readonly BlazorSentryIntegration _integration;

    public SentryCircuitHandler(BlazorSentryIntegration integration)
    {
        _integration = integration;
    }

    public override Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken ct)
    {
        _integration.OnCircuitOpened(circuit.Id);
        return Task.CompletedTask;
    }

    public override Task OnCircuitClosedAsync(Circuit circuit, CancellationToken ct)
    {
        _integration.OnCircuitClosed(circuit.Id);
        return Task.CompletedTask;
    }
}
