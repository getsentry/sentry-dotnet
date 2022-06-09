namespace Sentry.Android.Extensions;

internal static class SdkVersionExtensions
{
    public static SdkVersion ToSdkVersion(this Java.Protocol.SdkVersion sdkVersion)
    {
        var result = new SdkVersion
        {
            Name = sdkVersion.Name,
            Version = sdkVersion.Version,
        };

        if (sdkVersion.Packages is { } packages)
        {
            foreach (var package in packages)
            {
                result.AddPackage(package.Name, package.Version);
            }
        }

        return result;
    }

    public static Java.Protocol.SdkVersion ToJavaSdkVersion(this SdkVersion sdkVersion)
    {
        var result = new Java.Protocol.SdkVersion(sdkVersion.Name ?? "", sdkVersion.Version ?? "");

        foreach (var package in sdkVersion.Packages)
        {
            result.AddPackage(package.Name, package.Version);
        }

        return result;
    }
}
