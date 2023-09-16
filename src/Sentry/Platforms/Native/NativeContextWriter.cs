namespace Sentry.Native;
using CWUtil = ContextWriter;

// https://github.com/getsentry/sentry-unity/blob/3eb6eca6ed270c5ec023bf75ee53c1ca00bb7c82/src/Sentry.Unity.Native/NativeContextWriter.cs

internal class NativeContextWriter : ContextWriter
{
    protected override void WriteScope(
        string? AppStartTime,
        string? AppBuildType,
        string? OperatingSystemRawDescription,
        int? DeviceProcessorCount,
        string? DeviceCpuDescription,
        string? DeviceTimezone,
        bool? DeviceSupportsVibration,
        string? DeviceName,
        bool? DeviceSimulator,
        string? DeviceDeviceUniqueIdentifier,
        string? DeviceDeviceType,
        string? DeviceModel,
        long? DeviceMemorySize,
        int? GpuId,
        string? GpuName,
        string? GpuVendorName,
        int? GpuMemorySize,
        string? GpuNpotSupport,
        string? GpuVersion,
        string? GpuApiType,
        int? GpuMaxTextureSize,
        bool? GpuSupportsDrawCallInstancing,
        bool? GpuSupportsRayTracing,
        bool? GpuSupportsComputeShaders,
        bool? GpuSupportsGeometryShaders,
        string? GpuVendorId,
        bool? GpuMultiThreadedRendering,
        string? GpuGraphicsShaderLevel
    )
    {
        CWUtil.WriteApp(AppStartTime, AppBuildType);

        CWUtil.WriteOS(OperatingSystemRawDescription);

        CWUtil.WriteDevice(
            DeviceProcessorCount,
            DeviceCpuDescription,
            DeviceTimezone,
            DeviceSupportsVibration,
            DeviceName,
            DeviceSimulator,
            DeviceDeviceUniqueIdentifier,
            DeviceDeviceType,
            DeviceModel,
            DeviceMemorySize
        );

        CWUtil.WriteGpu(
            GpuId,
            GpuName,
            GpuVendorName,
            GpuMemorySize,
            GpuNpotSupport,
            GpuVersion,
            GpuApiType,
            GpuMaxTextureSize,
            GpuSupportsDrawCallInstancing,
            GpuSupportsRayTracing,
            GpuSupportsComputeShaders,
            GpuSupportsGeometryShaders,
            GpuVendorId,
            GpuMultiThreadedRendering,
            GpuGraphicsShaderLevel);
    }
}
