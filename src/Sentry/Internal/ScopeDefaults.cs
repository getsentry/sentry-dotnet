using Sentry.Internal.Extensions;
using Sentry.PlatformAbstractions;
using OperatingSystem = Sentry.Protocol.OperatingSystem;
using Runtime = Sentry.Protocol.Runtime;

namespace Sentry.Internal;

internal sealed class ScopeDefaults
{
    public ScopeDefaults(SentryOptions options)
    {
        Release = ResolveRelease(options);
        Distribution = options.Distribution ?? GetPlatformDefaultDistribution();
        Environment = options.SettingLocator.GetEnvironment();
        Sdk = CreateSdk(options.Sdk);
        Tags = new Dictionary<string, string>(options.DefaultTags);
        Contexts = CreateContexts();
    }

    public string? Release { get; }

    public string? Distribution { get; }

    public string Environment { get; }

    public SdkVersion Sdk { get; }

    public IReadOnlyDictionary<string, string> Tags { get; }

    public SentryContexts Contexts { get; }

    public string GetEnvironment(Scope? scope) => scope?.Environment ?? Environment;

    public SdkVersion GetSdk(Scope? scope) => scope?.Sdk is { Name: not null } sdk ? sdk : Sdk;

    public void Apply(IEventLike item)
    {
        switch (item)
        {
            case SentryEvent @event:
                @event.Release ??= Release;
                @event.Distribution ??= Distribution;
                break;
            case ITransactionData transaction:
                transaction.Release ??= Release;
                transaction.Distribution ??= Distribution;
                break;
        }

        item.Environment ??= Environment;

        if (item.Sdk.Name is null && item.Sdk.Version is null)
        {
            item.Sdk.Name = Sdk.Name;
            item.Sdk.Version = Sdk.Version;
        }

        foreach (var package in Sdk.InternalPackages)
        {
            item.Sdk.AddPackage(package);
        }

        foreach (var (key, value) in Tags)
        {
            if (!item.Tags.ContainsKey(key))
            {
                item.SetTag(key, value);
            }
        }

        Contexts.CopyTo(item.Contexts);
    }

    public void Apply(SentryCheckIn checkIn, Scope? scope)
    {
        checkIn.Release ??= Release;
        checkIn.Environment ??= GetEnvironment(scope);
    }

    private static string? ResolveRelease(SentryOptions options)
    {
#if ANDROID || __IOS__
        if (string.IsNullOrWhiteSpace(options.Release)
            && options.SettingLocator.GetEnvironmentVariable(Constants.ReleaseEnvironmentVariable).NullIfWhitespace() is null)
        {
            options.Release = SentrySdk.GetDefaultReleaseString();
        }
#endif
        return options.SettingLocator.GetRelease();
    }

    private static string? GetPlatformDefaultDistribution()
    {
#if ANDROID || __IOS__
        return SentrySdk.GetDefaultDistributionString();
#else
        return null;
#endif
    }

    private static SdkVersion CreateSdk(SdkVersion identity)
    {
        var sdk = new SdkVersion { Name = identity.Name, Version = identity.Version };
        foreach (var package in identity.InternalPackages)
        {
            sdk.AddPackage(package);
        }

        if (SdkVersion.Instance.Version is { } version)
        {
            sdk.AddPackage("nuget:" + SdkVersion.Instance.Name, version);
        }

        return sdk;
    }

    private static SentryContexts CreateContexts()
    {
        var contexts = new SentryContexts();

        var runtime = SentryRuntime.Current;
        contexts[Runtime.Type] = new Runtime
        {
            Name = runtime.Name,
            Version = runtime.Version,
            Identifier = runtime.Identifier,
            RawDescription = runtime.Raw
        };

        // RuntimeInformation.OSDescription is throwing on Mono 5.12
        if (!runtime.IsMono())
        {
            var os = contexts.OperatingSystem;
#if NETFRAMEWORK
            // RuntimeInformation.* throws on .NET Framework on macOS/Linux
            try
            {
                os.RawDescription = RuntimeInformation.OSDescription;
            }
            catch
            {
                os.RawDescription = System.Environment.OSVersion.VersionString;
            }
#else
            os.RawDescription = RuntimeInformation.OSDescription;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // works for catalyst and net9 base
                os.Name = "macOS";
                os.Version = System.Environment.OSVersion.Version.ToString(); // reports macOS version (ie. 15.3.0)
            }
#endif
        }

        contexts.Device.BootTime = ProcessInfo.Instance?.BootTime;

        return contexts;
    }
}
