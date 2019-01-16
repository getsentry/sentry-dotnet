using System.Runtime.Serialization;

// ReSharper disable once CheckNamespace
namespace Sentry.Protocol
{
    /// <summary>
    /// Graphics device unit
    /// </summary>
    /// <seealso href="https://docs.sentry.io/development/sdk-dev/interfaces/gpu/"/>
    [DataContract]
    public class Gpu
    {
        /// <summary>
        /// Tells Sentry which type of context this is.
        /// </summary>
        [DataMember(Name = "type", EmitDefaultValue = false)]
        public const string Type = "gpu";

        /// <summary>
        /// The name of the graphics device
        /// </summary>
        /// <example>
        /// iPod touch: Apple A8 GPU
        /// Samsung S7: Mali-T880
        /// </example>
        [DataMember(Name = "name", EmitDefaultValue = false)]
        public string Name { get; set; }

        /// <summary>
        /// The PCI Id of the graphics device
        /// </summary>
        /// <remarks>
        /// Combined with <see cref="VendorId"/> uniquely identifies the GPU
        /// </remarks>
        [DataMember(Name = "id", EmitDefaultValue = false)]
        public int? Id { get; set; }

        /// <summary>
        /// The PCI vendor Id of the graphics device
        /// </summary>
        /// <remarks>
        /// Combined with <see cref="Id"/> uniquely identifies the GPU
        /// </remarks>
        /// <seealso href="https://docs.microsoft.com/en-us/windows-hardware/drivers/install/identifiers-for-pci-devices"/>
        /// <seealso href="http://pci-ids.ucw.cz/read/PC/"/>
        [DataMember(Name = "vendor_id", EmitDefaultValue = false)]
        public int? VendorId { get; set; }

        /// <summary>
        /// The vendor name reported by the graphic device
        /// </summary>
        /// <example>
        /// Apple, ARM, WebKit
        /// </example>
        [DataMember(Name = "vendor_name", EmitDefaultValue = false)]
        public string VendorName { get; set; }

        /// <summary>
        /// Total GPU memory available in mega-bytes.
        /// </summary>
        [DataMember(Name = "memory_size", EmitDefaultValue = false)]
        public int? MemorySize { get; set; }

        /// <summary>
        /// Device type
        /// </summary>
        /// <remarks>The low level API used</remarks>
        /// <example>Metal, Direct3D11, OpenGLES3, PlayStation4, XboxOne</example>
        [DataMember(Name = "api_type", EmitDefaultValue = false)]
        public string ApiType { get; set; }

        /// <summary>
        /// Whether the GPU is multi-threaded rendering or not.
        /// </summary>
        [DataMember(Name = "multi_threaded_rendering", EmitDefaultValue = false)]
        public bool? MultiThreadedRendering { get; set; }

        /// <summary>
        /// The Version of the API of the graphics device
        /// </summary>
        /// <example>
        /// iPod touch: Metal
        /// Android: OpenGL ES 3.2 v1.r22p0-01rel0.f294e54ceb2cb2d81039204fa4b0402e
        /// WebGL Windows: OpenGL ES 3.0 (WebGL 2.0 (OpenGL ES 3.0 Chromium))
        /// OpenGL 2.0, Direct3D 9.0c
        /// </example>
        [DataMember(Name = "version", EmitDefaultValue = false)]
        public string Version { get; set; }

        /// <summary>
        /// The Non-Power-Of-Two support level
        /// </summary>
        /// <example>
        /// Full
        /// </example>
        [DataMember(Name = "npot_support", EmitDefaultValue = false)]
        public string NpotSupport { get; set; }

        /// <summary>
        /// Clones this instance
        /// </summary>
        /// <returns></returns>
        internal Gpu Clone()
            => new Gpu
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
    }
}
