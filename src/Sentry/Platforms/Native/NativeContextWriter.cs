using Sentry.Internal;

namespace Sentry.Native;

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
        WriteApp(AppStartTime, AppBuildType);

        WriteOS(OperatingSystemRawDescription);

        WriteDevice(
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

        WriteGpu(
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

    internal static void WriteApp(string? AppStartTime, string? AppBuildType)
    {
        var obj = C.sentry_value_new_object();
        C.SetValueIfNotNull(obj, "app_start_time", AppStartTime);
        C.SetValueIfNotNull(obj, "build_type", AppBuildType);
        C.sentry_set_context(Sentry.Protocol.App.Type, obj);
    }

    internal static void WriteOS(string? OperatingSystemRawDescription)
    {
        var obj = C.sentry_value_new_object();
        C.SetValueIfNotNull(obj, "raw_description", OperatingSystemRawDescription);
        C.sentry_set_context(Sentry.Protocol.OperatingSystem.Type, obj);
    }

    internal static void WriteDevice(
        int? DeviceProcessorCount,
        string? DeviceCpuDescription,
        string? DeviceTimezone,
        bool? DeviceSupportsVibration,
        string? DeviceName,
        bool? DeviceSimulator,
        string? DeviceDeviceUniqueIdentifier,
        string? DeviceDeviceType,
        string? DeviceModel,
        long? DeviceMemorySize)
    {
        var obj = C.sentry_value_new_object();
        C.SetValueIfNotNull(obj, "processor_count", DeviceProcessorCount);
        C.SetValueIfNotNull(obj, "cpu_description", DeviceCpuDescription);
        C.SetValueIfNotNull(obj, "timezone", DeviceTimezone);
        C.SetValueIfNotNull(obj, "supports_vibration", DeviceSupportsVibration);
        C.SetValueIfNotNull(obj, "name", DeviceName);
        C.SetValueIfNotNull(obj, "simulator", DeviceSimulator);
        C.SetValueIfNotNull(obj, "device_unique_identifier", DeviceDeviceUniqueIdentifier);
        C.SetValueIfNotNull(obj, "device_type", DeviceDeviceType);
        C.SetValueIfNotNull(obj, "model", DeviceModel);
        C.SetValueIfNotNull(obj, "memory_size", DeviceMemorySize);
        C.sentry_set_context(Sentry.Protocol.Device.Type, obj);
    }

    internal static void WriteGpu(
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
        string? GpuGraphicsShaderLevel)
    {
        var obj = C.sentry_value_new_object();
        C.SetValueIfNotNull(obj, "id", GpuId);
        C.SetValueIfNotNull(obj, "name", GpuName);
        C.SetValueIfNotNull(obj, "vendor_name", GpuVendorName);
        C.SetValueIfNotNull(obj, "memory_size", GpuMemorySize);
        C.SetValueIfNotNull(obj, "npot_support", GpuNpotSupport);
        C.SetValueIfNotNull(obj, "version", GpuVersion);
        C.SetValueIfNotNull(obj, "api_type", GpuApiType);
        C.SetValueIfNotNull(obj, "max_texture_size", GpuMaxTextureSize);
        C.SetValueIfNotNull(obj, "supports_draw_call_instancing", GpuSupportsDrawCallInstancing);
        C.SetValueIfNotNull(obj, "supports_ray_tracing", GpuSupportsRayTracing);
        C.SetValueIfNotNull(obj, "supports_compute_shaders", GpuSupportsComputeShaders);
        C.SetValueIfNotNull(obj, "supports_geometry_shaders", GpuSupportsGeometryShaders);
        C.SetValueIfNotNull(obj, "vendor_id", GpuVendorId);
        C.SetValueIfNotNull(obj, "multi_threaded_rendering", GpuMultiThreadedRendering);
        C.SetValueIfNotNull(obj, "graphics_shader_level", GpuGraphicsShaderLevel);
        C.sentry_set_context(Sentry.Protocol.Gpu.Type, obj);
    }

}
