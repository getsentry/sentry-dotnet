using System.Text;
using System.Text.Json;
using Sentry.Internal.Extensions;

namespace Sentry.Tests.Helpers;

internal static class JsonSerializableExtensions
{
    public static string ToJsonString(this IJsonSerializable serializable)
    {
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        writer.WriteSerializableValue(serializable);
        writer.Flush();

        return Encoding.UTF8.GetString(stream.ToArray());
    }
}
