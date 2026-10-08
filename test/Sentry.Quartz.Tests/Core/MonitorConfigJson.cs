namespace Sentry.Quartz.Tests;

internal static class MonitorConfigJson
{
    public static JsonElement Render(Action<SentryMonitorOptions> configureMonitorOptions)
    {
        var options = new SentryMonitorOptions();
        configureMonitorOptions(options);
        var checkIn = new SentryCheckIn("monitor-slug", CheckInStatus.InProgress) { MonitorOptions = options };

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            checkIn.WriteTo(writer, null);
        }

        using var document = JsonDocument.Parse(stream.ToArray());
        return document.RootElement.GetProperty("monitor_config").Clone();
    }

    public static Action<SentryMonitorOptions>? ReceivedConfigureMonitorOptions(this IHub hub, string monitorSlug) =>
        hub.ReceivedCalls()
            .Where(call => call.GetMethodInfo().Name == nameof(IHub.CaptureCheckIn))
            .Select(call => call.GetArguments())
            .Where(args => (string?)args[0] == monitorSlug && (CheckInStatus?)args[1] == CheckInStatus.InProgress)
            .Select(args => args[5] as Action<SentryMonitorOptions>)
            .Single();
}
