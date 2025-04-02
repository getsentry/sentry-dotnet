namespace Sentry.Android.AssemblyReader.V2;

internal static class DeviceArchitectureExtensions
{
    public static AndroidTargetArch AbiToDeviceArchitecture(this string abi) =>
        abi switch
        {
            "armeabi-v7a" => AndroidTargetArch.Arm,
            "arm64-v8a" => AndroidTargetArch.Arm64,
            "x86" => AndroidTargetArch.X86,
            "x86_64" => AndroidTargetArch.X86_64,
            "mips" => AndroidTargetArch.Mips,
            _ => AndroidTargetArch.Other,
        };
}
