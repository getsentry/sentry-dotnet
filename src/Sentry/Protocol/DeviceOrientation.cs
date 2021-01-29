using System.Runtime.Serialization;

// ReSharper disable once CheckNamespace
namespace Sentry.Protocol
{
    /// <summary>
    /// Defines the orientation of a device.
    /// </summary>
    public enum DeviceOrientation
    {
        /// <summary>
        /// Portrait.
        /// </summary>
        [EnumMember(Value = "portrait")]
        Portrait,

        /// <summary>
        /// Landscape.
        /// </summary>
        [EnumMember(Value = "landscape")]
        Landscape
    }
}
