using Sentry.PlatformAbstractions;
using OperatingSystem = Sentry.Protocol.OperatingSystem;
using Runtime = Sentry.Protocol.Runtime;

namespace Sentry.Internal;

internal class Enricher
{
    internal const string DefaultIpAddress = "{{auto}}";

    private readonly SentryOptions _options;

    private readonly Lazy<Runtime> _runtimeLazy = new(() =>
    {
        var current = PlatformAbstractions.SentryRuntime.Current;
        return new Runtime
        {
            Name = current.Name,
            Version = current.Version,
            Identifier = current.Identifier,
            RawDescription = current.Raw
        };
    });

    public Enricher(SentryOptions options) => _options = options;

    public void Apply(IEventLike eventLike)
    {
        // Runtime
        if (!eventLike.Contexts.ContainsKey(Runtime.Type))
        {
            eventLike.Contexts[Runtime.Type] = _runtimeLazy.Value;
        }

        // Operating System
        if (!eventLike.Contexts.ContainsKey(OperatingSystem.Type))
        {
            // RuntimeInformation.OSDescription is throwing on Mono 5.12
            if (!PlatformAbstractions.SentryRuntime.Current.IsMono())
            {
#if NETFRAMEWORK
                // RuntimeInformation.* throws on .NET Framework on macOS/Linux
                try {
                    eventLike.Contexts.OperatingSystem.RawDescription = RuntimeInformation.OSDescription;
                } catch {
                    eventLike.Contexts.OperatingSystem.RawDescription = Environment.OSVersion.VersionString;
                }
#else
                eventLike.Contexts.OperatingSystem.RawDescription = RuntimeInformation.OSDescription;
#endif
            }
        }

        // SDK
        // SDK Name/Version might have be already set by an outer package
        // e.g: ASP.NET Core can set itself as the SDK
        if (eventLike.Sdk.Version is null && eventLike.Sdk.Name is null)
        {
            eventLike.Sdk.Name = Constants.SdkName;
            eventLike.Sdk.Version = SdkVersion.Instance.Version;
        }

        if (SdkVersion.Instance.Version is not null)
        {
            eventLike.Sdk.AddPackage("nuget:" + SdkVersion.Instance.Name, SdkVersion.Instance.Version);
        }

        // Release
        eventLike.Release ??= _options.SettingLocator.GetRelease();

        // Distribution
        eventLike.Distribution ??= _options.Distribution;

        // Environment
        eventLike.Environment ??= _options.SettingLocator.GetEnvironment();

        // User
        // Report local user if opt-in PII, no user was already set to event and feature not opted-out:
        if (_options.SendDefaultPii)
        {
            if (_options.IsEnvironmentUser && !eventLike.HasUser())
            {
                eventLike.User.Username = Environment.UserName;
            }

            eventLike.User.IpAddress ??= DefaultIpAddress;
        }
        eventLike.User.Id ??= _options.InstallationId;

        //Apply App startup and Boot time
        eventLike.Contexts.App.StartTime ??= ProcessInfo.Instance?.StartupTime;
        eventLike.Contexts.Device.BootTime ??= ProcessInfo.Instance?.BootTime;

        eventLike.Contexts.App.InForeground = ProcessInfo.Instance?.ApplicationIsActivated(_options);

        // Default tags
        _options.ApplyDefaultTags(eventLike);
    }

    public void Apply(SentryCheckIn checkIn)
    {
        checkIn.Release ??= _options.SettingLocator.GetRelease();
        checkIn.Environment ??= _options.SettingLocator.GetEnvironment();
    }
}
