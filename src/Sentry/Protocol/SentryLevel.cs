using System.Runtime.Serialization;

namespace Sentry.Protocol
{
    /// <summary>
    /// The level of the event sent to Sentry
    /// </summary>
    public enum SentryLevel : short
    {
        /// <summary>
        /// Fatal
        /// </summary>
        [EnumMember(Value = "fatal")]
        Fatal = -1,
        /// <summary>
        /// Error
        /// </summary>
        [EnumMember(Value = "error")]
        Error, // defaults to 0
        /// <summary>
        /// Warning
        /// </summary>
        [EnumMember(Value = "warning")]
        Warning,
        /// <summary>
        /// Informational
        /// </summary>
        [EnumMember(Value = "info")]
        Info,
        /// <summary>
        /// Debug
        /// </summary>
        [EnumMember(Value = "debug")]
        Debug
    }
}
