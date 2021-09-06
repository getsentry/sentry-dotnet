using System.IO;
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

    internal static class JsonSerializableExtensions
    {
        public static void WriteToFile(this IJsonSerializable serializable, string filePath)
        {
            using var file = File.Create(filePath);
            using var writer = new Utf8JsonWriter(file);

            serializable.WriteTo(writer);
            writer.Flush();
        }
    }
}
