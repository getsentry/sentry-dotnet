using Sentry.Extensibility;
using Sentry.Internal;
using Sentry.Internal.Extensions;

namespace Sentry.Protocol;

/// <summary>
/// Graphics device unit.
/// </summary>
/// <seealso href="https://develop.sentry.dev/sdk/event-payloads/contexts/#gpu-context"/>
public sealed class Gpu : ISentryJsonSerializable, ICloneable<Gpu>, IUpdatable<Gpu>
{
    /// <summary>
    /// Tells Sentry which type of context this is.
    /// </summary>
    public const string Type = "gpu";

    /// <summary>
    /// The name of the graphics device.
    /// </summary>
    /// <example>
    /// iPod touch: Apple A8 GPU
    /// Samsung S7: Mali-T880
    /// </example>
    public string? Name { get; set; }

    /// <summary>
    /// The PCI Id of the graphics device.
    /// </summary>
    /// <remarks>
    /// Combined with <see cref="VendorId"/> uniquely identifies the GPU.
    /// </remarks>
    public int? Id { get; set; }

    /// <summary>
    /// The PCI vendor Id of the graphics device.
    /// </summary>
    /// <remarks>
    /// Combined with <see cref="Id"/> uniquely identifies the GPU.
    /// </remarks>
    /// <seealso href="https://docs.microsoft.com/en-us/windows-hardware/drivers/install/identifiers-for-pci-devices"/>
    /// <seealso href="http://pci-ids.ucw.cz/read/PC/"/>
    public string? VendorId { get; set; }

    /// <summary>
    /// The vendor name reported by the graphic device.
    /// </summary>
    /// <example>
    /// Apple, ARM, WebKit
    /// </example>
    public string? VendorName { get; set; }

    /// <summary>
    /// Total GPU memory available in mega-bytes.
    /// </summary>
    public int? MemorySize { get; set; }

    /// <summary>
    /// Device type.
    /// </summary>
    /// <remarks>The low level API used.</remarks>
    /// <example>Metal, Direct3D11, OpenGLES3, PlayStation4, XboxOne</example>
    public string? ApiType { get; set; }

    /// <summary>
    /// Whether the GPU is multi-threaded rendering or not.
    /// </summary>
    public bool? MultiThreadedRendering { get; set; }

    /// <summary>
    /// The Version of the API of the graphics device.
    /// </summary>
    /// <example>
    /// iPod touch: Metal
    /// Android: OpenGL ES 3.2 v1.r22p0-01rel0.f294e54ceb2cb2d81039204fa4b0402e
    /// WebGL Windows: OpenGL ES 3.0 (WebGL 2.0 (OpenGL ES 3.0 Chromium))
    /// OpenGL 2.0, Direct3D 9.0c
    /// </example>
    public string? Version { get; set; }

    /// <summary>
    /// The Non-Power-Of-Two support level.
    /// </summary>
    /// <example>
    /// Full
    /// </example>
    public string? NpotSupport { get; set; }

    /// <summary>
    /// Largest size of a texture that is supported by the graphics hardware.
    /// </summary>
    public int? MaxTextureSize { get; set; }

    /// <summary>
    /// Approximate "shader capability" level of the graphics device.
    /// </summary>
    /// <example>
    /// Shader Model 2.0, OpenGL ES 3.0, Metal / OpenGL ES 3.1, 27 (unknown)
    /// </example>
    public string? GraphicsShaderLevel { get; set; }

    /// <summary>
    /// Is GPU draw call instancing supported?
    /// </summary>
    public bool? SupportsDrawCallInstancing { get; set; }

    /// <summary>
    /// Is ray tracing available on the device?
    /// </summary>
    public bool? SupportsRayTracing { get; set; }

    /// <summary>
    /// Are compute shaders available on the device?
    /// </summary>
    public bool? SupportsComputeShaders { get; set; }

    /// <summary>
    /// Are geometry shaders available on the device?
    /// </summary>
    public bool? SupportsGeometryShaders { get; set; }

    /// <summary>
    /// Clones this instance.
    /// </summary>
    internal Gpu Clone() => ((ICloneable<Gpu>)this).Clone();

    Gpu ICloneable<Gpu>.Clone()
        => new()
        {
            Name = Name,
            Id = Id,
            VendorId = VendorId,
            VendorName = VendorName,
            MemorySize = MemorySize,
            ApiType = ApiType,
            MultiThreadedRendering = MultiThreadedRendering,
            Version = Version,
            NpotSupport = NpotSupport,
            MaxTextureSize = MaxTextureSize,
            GraphicsShaderLevel = GraphicsShaderLevel,
            SupportsDrawCallInstancing = SupportsDrawCallInstancing,
            SupportsRayTracing = SupportsRayTracing,
            SupportsComputeShaders = SupportsComputeShaders,
            SupportsGeometryShaders = SupportsGeometryShaders
        };

    /// <summary>
    /// Updates this instance with data from the properties in the <paramref name="source"/>,
    /// unless there is already a value in the existing property.
    /// </summary>
    internal void UpdateFrom(Gpu source) => ((IUpdatable<Gpu>)this).UpdateFrom(source);

    void IUpdatable.UpdateFrom(object source)
    {
        if (source is Gpu gpu)
        {
            ((IUpdatable<Gpu>)this).UpdateFrom(gpu);
        }
    }

    void IUpdatable<Gpu>.UpdateFrom(Gpu source)
    {
        Name ??= source.Name;
        Id ??= source.Id;
        VendorId ??= source.VendorId;
        VendorName ??= source.VendorName;
        MemorySize ??= source.MemorySize;
        ApiType ??= source.ApiType;
        MultiThreadedRendering ??= source.MultiThreadedRendering;
        Version ??= source.Version;
        NpotSupport ??= source.NpotSupport;
        MaxTextureSize ??= source.MaxTextureSize;
        GraphicsShaderLevel ??= source.GraphicsShaderLevel;
        SupportsDrawCallInstancing ??= source.SupportsDrawCallInstancing;
        SupportsRayTracing ??= source.SupportsRayTracing;
        SupportsComputeShaders ??= source.SupportsComputeShaders;
        SupportsGeometryShaders ??= source.SupportsGeometryShaders;
    }

    /// <inheritdoc />
    public void WriteTo(Utf8JsonWriter writer, IDiagnosticLogger? _)
    {
        writer.WriteStartObject();

        writer.WriteString("type", Type);
        writer.WriteStringIfNotWhiteSpace("name", Name);
        writer.WriteNumberIfNotNull("id", Id);
        writer.WriteStringIfNotWhiteSpace("vendor_id", VendorId);
        writer.WriteStringIfNotWhiteSpace("vendor_name", VendorName);
        writer.WriteNumberIfNotNull("memory_size", MemorySize);
        writer.WriteStringIfNotWhiteSpace("api_type", ApiType);
        writer.WriteBooleanIfNotNull("multi_threaded_rendering", MultiThreadedRendering);
        writer.WriteStringIfNotWhiteSpace("version", Version);
        writer.WriteStringIfNotWhiteSpace("npot_support", NpotSupport);
        writer.WriteNumberIfNotNull("max_texture_size", MaxTextureSize);
        writer.WriteStringIfNotWhiteSpace("graphics_shader_level", GraphicsShaderLevel);
        writer.WriteBooleanIfNotNull("supports_draw_call_instancing", SupportsDrawCallInstancing);
        writer.WriteBooleanIfNotNull("supports_ray_tracing", SupportsRayTracing);
        writer.WriteBooleanIfNotNull("supports_compute_shaders", SupportsComputeShaders);
        writer.WriteBooleanIfNotNull("supports_geometry_shaders", SupportsGeometryShaders);

        writer.WriteEndObject();
    }

    /// <summary>
    /// Parses from JSON.
    /// </summary>
    public static Gpu FromJson(JsonElement json)
    {
        var name = json.GetPropertyOrNull("name")?.GetString();
        var id = json.GetPropertyOrNull("id")?.GetInt32();
        var vendorId = json.GetPropertyOrNull("vendor_id")?.GetString();
        var vendorName = json.GetPropertyOrNull("vendor_name")?.GetString();
        var memorySize = json.GetPropertyOrNull("memory_size")?.GetInt32();
        var apiType = json.GetPropertyOrNull("api_type")?.GetString();
        var multiThreadedRendering = json.GetPropertyOrNull("multi_threaded_rendering")?.GetBoolean();
        var version = json.GetPropertyOrNull("version")?.GetString();
        var npotSupport = json.GetPropertyOrNull("npot_support")?.GetString();
        var maxTextureSize = json.GetPropertyOrNull("max_texture_size")?.GetInt32();
        var graphicsShaderLevel = json.GetPropertyOrNull("graphics_shader_level")?.GetString();
        var supportsDrawCallInstancing = json.GetPropertyOrNull("supports_draw_call_instancing")?.GetBoolean();
        var supportsRayTracing = json.GetPropertyOrNull("supports_ray_tracing")?.GetBoolean();
        var supportsComputeShaders = json.GetPropertyOrNull("supports_compute_shaders")?.GetBoolean();
        var supportsGeometryShaders = json.GetPropertyOrNull("supports_geometry_shaders")?.GetBoolean();

        return new Gpu
        {
            Name = name,
            Id = id,
            VendorId = vendorId,
            VendorName = vendorName,
            MemorySize = memorySize,
            ApiType = apiType,
            MultiThreadedRendering = multiThreadedRendering,
            Version = version,
            NpotSupport = npotSupport,
            MaxTextureSize = maxTextureSize,
            GraphicsShaderLevel = graphicsShaderLevel,
            SupportsDrawCallInstancing = supportsDrawCallInstancing,
            SupportsRayTracing = supportsRayTracing,
            SupportsComputeShaders = supportsComputeShaders,
            SupportsGeometryShaders = supportsGeometryShaders
        };
    }
}
