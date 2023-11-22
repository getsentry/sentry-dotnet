namespace Sentry.Internal;

/// <summary>
/// Allows synchronizing Context from .NET to native layers.
/// We're providing a single method that the implementations should override.
/// They can choose to either have the single method directly in native using p/invoke,
/// or use a more fine-grained interface, whatever is best for the platform.
/// </summary>
/// <remarks>
/// WriteScope() is called in a new Task (background thread from a pool).
/// </remarks>
internal abstract class ContextWriter
{
    public void Write(Scope scope)
    {
        WriteScope(
            scope.Contexts.App.StartTime?.ToString("o"),
            scope.Contexts.App.BuildType,
            scope.Contexts.OperatingSystem.RawDescription,
            scope.Contexts.Device.ProcessorCount,
            scope.Contexts.Device.CpuDescription,
            scope.Contexts.Device.Timezone?.Id,
            scope.Contexts.Device.SupportsVibration,
            scope.Contexts.Device.Name,
            scope.Contexts.Device.Simulator,
            scope.Contexts.Device.DeviceUniqueIdentifier,
            scope.Contexts.Device.DeviceType,
            scope.Contexts.Device.Model,
            scope.Contexts.Device.MemorySize,
            scope.Contexts.Gpu.Id,
            scope.Contexts.Gpu.Name,
            scope.Contexts.Gpu.VendorName,
            scope.Contexts.Gpu.MemorySize,
            scope.Contexts.Gpu.NpotSupport,
            scope.Contexts.Gpu.Version,
            scope.Contexts.Gpu.ApiType,
            scope.Contexts.Gpu.MaxTextureSize,
            scope.Contexts.Gpu.SupportsDrawCallInstancing,
            scope.Contexts.Gpu.SupportsRayTracing,
            scope.Contexts.Gpu.SupportsComputeShaders,
            scope.Contexts.Gpu.SupportsGeometryShaders,
            scope.Contexts.Gpu.VendorId,
            scope.Contexts.Gpu.MultiThreadedRendering,
            scope.Contexts.Gpu.GraphicsShaderLevel
        );
    }

    protected abstract void WriteScope(
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
    );
}
