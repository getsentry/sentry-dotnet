#if NETFRAMEWORK
using Sentry.Extensibility;

namespace Sentry.PlatformAbstractions;

internal class NetFxInstallationsEventProcessor : ISentryEventProcessor
{
    internal static readonly string NetFxInstallationsKey = ".NET Framework";

    private readonly Lazy<Dictionary<string, string>> _netFxInstallations =
        new(GetInstallationsDictionary, LazyThreadSafetyMode.ExecutionAndPublication);

    private volatile bool _netFxInstallationEnabled = true;

    private readonly SentryOptions _options;

    internal NetFxInstallationsEventProcessor(SentryOptions options) => _options = options;

    private static Dictionary<string, string> GetInstallationsDictionary() =>
        FrameworkInfo.GetInstallations()
            .GroupBy(installation => installation.Profile)
            .ToDictionary(
                grouping => $"{NetFxInstallationsKey} {grouping.Key}",
                grouping => string.Join(", ", grouping.Select(i => $"\"{i.GetVersionNumber()}\"").Distinct())
            );

    public SentryEvent? Process(SentryEvent @event)
    {
        if (!_netFxInstallationEnabled)
        {
            _options.LogDebug("NetFxInstallation disabled due to previous error.");
        }
        else if (!@event.Contexts.ContainsKey(NetFxInstallationsKey))
        {
            try
            {
                @event.Contexts[NetFxInstallationsKey] = _netFxInstallations.Value;
            }
            catch (Exception ex)
            {
                _options.LogError(ex, "Failed to add NetFxInstallations into event.");

                // In case of any failure, this process function will be disabled to avoid throwing exceptions for future events.
                _netFxInstallationEnabled = false;
            }
        }

        return @event;
    }
}
#endif
