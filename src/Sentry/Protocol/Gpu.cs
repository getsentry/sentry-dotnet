using System.Text.Json;
using Sentry.Internal.Extensions;

namespace Sentry.Protocol
{
    /// <summary>
    /// Graphics device unit.
    /// </summary>
    /// <seealso href="https://develop.sentry.dev/sdk/event-payloads/contexts/#gpu-context"/>
    public sealed class Gpu : IJsonSerializable
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
        /// Clones this instance.
        /// </summary>
        internal Gpu Clone()
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
            };

        /// <inheritdoc />
        public void WriteTo(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();

            writer.WriteString("type", Type);

            if (!string.IsNullOrWhiteSpace(Name))
            {
                writer.WriteString("name", Name);
            }

            if (Id is {} id)
            {
                writer.WriteNumber("id", id);
            }

            if (!string.IsNullOrWhiteSpace(VendorId))
            {
                writer.WriteString("vendor_id", VendorId);
            }

            if (!string.IsNullOrWhiteSpace(VendorName))
            {
                writer.WriteString("vendor_name", VendorName);
            }

            if (MemorySize is {} memorySize)
            {
                writer.WriteNumber("memory_size", memorySize);
            }

            if (!string.IsNullOrWhiteSpace(ApiType))
            {
                writer.WriteString("api_type", ApiType);
            }

            if (MultiThreadedRendering is {} multiThreadedRendering)
            {
                writer.WriteBoolean("multi_threaded_rendering", multiThreadedRendering);
            }

            if (!string.IsNullOrWhiteSpace(Version))
            {
                writer.WriteString("version", Version);
            }

            if (!string.IsNullOrWhiteSpace(NpotSupport))
            {
                writer.WriteString("npot_support", NpotSupport);
            }

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
                NpotSupport = npotSupport
            };
        }
    }
}
