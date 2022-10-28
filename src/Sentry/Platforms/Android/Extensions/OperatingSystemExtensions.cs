using System.Runtime.InteropServices;
using OperatingSystem = Sentry.Protocol.OperatingSystem;

namespace Sentry.Android.Extensions;

internal static class OperatingSystemExtensions
{
    public static void ApplyFromAndroidRuntime(this OperatingSystem operatingSystem)
    {
        operatingSystem.Name ??= "Android";
        operatingSystem.Version ??= AndroidBuild.VERSION.Release;
        operatingSystem.Build ??= AndroidBuild.Display;

        if (operatingSystem.KernelVersion == null)
        {
            // ex: "Linux 5.10.98-android13-0-00003-g6ea688a79989-ab8162051 #1 SMP PREEMPT Tue Feb 8 00:20:26 UTC 2022"
            var osParts = RuntimeInformation.OSDescription.Split(' ');
            if (osParts.Length >= 2 && osParts[1].Contains("android", StringComparison.OrdinalIgnoreCase))
            {
                // ex: "5.10.98-android13-0-00003-g6ea688a79989-ab8162051"
                operatingSystem.KernelVersion = osParts[1];
            }
        }

        // operatingSystem.RawDescription is already set in Enricher.cs
    }

    public static void ApplyFromSentryAndroidSdk(this OperatingSystem operatingSystem, JavaSdk.Protocol.OperatingSystem os)
    {
        // We already have everything above, except the Rooted flag.
        // The Android SDK figures this out in RootChecker.java
        // TODO: Consider porting the Java root checker to .NET.  We probably have access to all the same information.
        // https://github.com/getsentry/sentry-java/blob/main/sentry-android-core/src/main/java/io/sentry/android/core/internal/util/RootChecker.java
        operatingSystem.Rooted ??= os.IsRooted()?.BooleanValue();
    }
}
