using Sentry.Internal;
using Sentry.Protocol;
using OperatingSystem = Sentry.Protocol.OperatingSystem;

namespace Sentry.Android;

internal static class AndroidDeviceLogAttributes
{
    public static IReadOnlyList<KeyValuePair<string, string>> Create()
    {
        var os = new OperatingSystem { Name = "Android", Version = AndroidBuild.VERSION.Release };
        var device = new Device
        {
            Brand = AndroidBuild.Brand,
            Model = AndroidBuild.Model,
            Family = AndroidBuild.Model?.Split(' ')[0]
        };
        return DeviceLogAttributes.Create(os, device);
    }
}
