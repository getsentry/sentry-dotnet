using Sentry.Extensibility;
using Sentry.Internal;

namespace Sentry.Cocoa;

internal static class CocoaDeviceLogAttributes
{
    public static IReadOnlyList<KeyValuePair<string, string>> Create(IDiagnosticLogger? logger)
    {
        try
        {
            return CocoaEventProcessor.GetNativeContexts() is { } contexts ? Create(contexts) : [];
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error getting Cocoa device attributes for logs.");
            return [];
        }
    }

    internal static IReadOnlyList<KeyValuePair<string, string>> Create(SentryContexts contexts)
    {
        contexts.Device.Brand = "Apple";
        return DeviceLogAttributes.Create(contexts.OperatingSystem, contexts.Device);
    }
}
