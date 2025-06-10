using Sentry.Internal;
using Sentry.PlatformAbstractions;

// ReSharper disable once CheckNamespace
namespace Sentry;

internal static class SentryNative
{
#if NET8_0_OR_GREATER
    // Should be in-sync with Sentry.Native.targets const.
    private const string SentryNativeIsEnabledSwitchName = "Sentry.Native.IsEnabled";

    private static readonly bool IsAvailableCore;

#if NET9_0_OR_GREATER
    // FeatureSwitchDefinition should help with trimming disabled code.
    // This way, `SentryNative.IsEnabled` should be treated as a compile-time constant for trimmed apps.
    [FeatureSwitchDefinition(SentryNativeIsEnabledSwitchName)]
#endif
    private static bool IsEnabled => !AppContext.TryGetSwitch(SentryNativeIsEnabledSwitchName, out var isEnabled) || isEnabled;

    internal static bool IsAvailable => IsEnabled && IsAvailableCore;

    static SentryNative()
    {
        IsAvailableCore = !SentryRuntime.Current.IsBrowserWasm();
    }
#else
    // This is a compile-time const so that the irrelevant code is removed during compilation.
    internal const bool IsAvailable = false;
#endif
}
