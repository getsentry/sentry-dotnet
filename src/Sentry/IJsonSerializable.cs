using System.Text.Json;

namespace Sentry
{
    /// <summary>
    /// Sentry JsonSerializable.
    /// </summary>
    public interface IJsonSerializable
    {
        /// <summary>
        /// Writes the object as JSON.
        /// </summary>
        /// <remarks>
        /// Note: this method is meant only for internal use and is exposed due to a language limitation.
        /// Avoid relying on this method in user code.
        /// </remarks>
        void WriteTo(Utf8JsonWriter writer);
    }
}
