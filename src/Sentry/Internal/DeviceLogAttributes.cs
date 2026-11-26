using Sentry.Protocol;
using OperatingSystem = Sentry.Protocol.OperatingSystem;

namespace Sentry.Internal;

internal static class DeviceLogAttributes
{
    public static IReadOnlyList<KeyValuePair<string, string>> Create(OperatingSystem os, Device device)
    {
        var attributes = new List<KeyValuePair<string, string>>();
        Add(attributes, "os.name", os.Name);
        Add(attributes, "os.version", os.Version);
        Add(attributes, "device.brand", device.Brand);
        Add(attributes, "device.model", device.Model);
        Add(attributes, "device.family", device.Family);
        return attributes;
    }

    private static void Add(List<KeyValuePair<string, string>> attributes, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            attributes.Add(new KeyValuePair<string, string>(key, value!));
        }
    }
}
