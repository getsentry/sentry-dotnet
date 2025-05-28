using Sentry.Internal;

namespace Sentry.Android.Extensions;

internal static class SentryEventExtensions
{
    /*
     * These methods map between a SentryEvent and it's Java counterpart by serializing as JSON into memory on one side,
     * then deserializing back to an object on the other side.  It is not expected to be performant, as this code is only
     * used when a BeforeSend option is set, and then only when an event is captured by the Java SDK (which should be
     * relatively rare).
     *
     * This approach avoids having to write to/from methods for the entire object graph.  However, it's also important to
     * recognize that there's not necessarily a one-to-one mapping available on all objects (even through serialization)
     * between the two SDKs, so some optional details may be lost when roundtripping.  That's generally OK, as this is
     * still better than nothing.  If a specific part of the object graph becomes important to roundtrip, we can consider
     * updating the objects on either side.
     */

    public static SentryEvent ToSentryEvent(this JavaSdk.SentryEvent sentryEvent, JavaSdk.SentryOptions javaOptions)
    {
        if (sentryEvent.Sdk != null)
        {
            // when we cast this serialize this over, this value must be set
            sentryEvent.Sdk.Name ??= Constants.SdkName;
            sentryEvent.Sdk.Version ??= SdkVersion.Instance.Version ?? "0.0.0";
        }
        using var stream = new MemoryStream();
        using var streamWriter = new JavaOutputStreamWriter(stream);
        using var jsonWriter = new JavaSdk.JsonObjectWriter(streamWriter, javaOptions.MaxDepth);
        sentryEvent.Serialize(jsonWriter, javaOptions.Logger);

        streamWriter.Flush();
        stream.Seek(0, SeekOrigin.Begin);

        using var json = JsonDocument.Parse(stream);
        return SentryEvent.FromJson(json.RootElement, sentryEvent.Throwable);
    }

    public static JavaSdk.SentryEvent ToJavaSentryEvent(this SentryEvent sentryEvent, SentryOptions options, JavaSdk.SentryOptions javaOptions)
    {
        if (sentryEvent.Sdk != null)
        {
            sentryEvent.Sdk.Name ??= Constants.SdkName;
            sentryEvent.Sdk.Version ??= SdkVersion.Instance.Version ?? "0.0.0";
        }
        using var stream = new MemoryStream();
        using var jsonWriter = new Utf8JsonWriter(stream);
        sentryEvent.WriteTo(jsonWriter, options.DiagnosticLogger);
        jsonWriter.Flush();
        stream.Seek(0, SeekOrigin.Begin);

        using var streamReader = new JavaInputStreamReader(stream);
        using var jsonReader = new JavaSdk.JsonObjectReader(streamReader);
        using var deserializer = new JavaSdk.SentryEvent.Deserializer();
        return deserializer.Deserialize(jsonReader, javaOptions.Logger);
    }
}
