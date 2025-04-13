using Sentry.Internal;
using Sentry.PlatformAbstractions;

// ReSharper disable once CheckNamespace
namespace Sentry;

internal static class SentryNative
{
#if NET8_0_OR_GREATER
    internal static bool IsEnabled => !AppContext.TryGetSwitch("Sentry.Native.IsEnabled", out var isEnabled) || isEnabled;

    internal static bool IsAvailable { get; }

    static SentryNative()
    {
        IsAvailable = IsEnabled && AotHelper.IsTrimmed && !SentryRuntime.Current.IsBrowserWasm();
    }
#else
    // This is a compile-time const so that the irrelevant code is removed during compilation.
    internal const bool IsAvailable = false;
#endif
}
