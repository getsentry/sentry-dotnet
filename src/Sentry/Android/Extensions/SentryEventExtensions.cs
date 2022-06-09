namespace Sentry.Android.Extensions;

internal static class SentryEventExtensions
{
    public static SentryEvent ToSentryEvent(this Java.SentryEvent sentryEvent, Java.SentryOptions javaOptions)
    {
        // TODO: Map properties instead of using serialization

        using var stream = new MemoryStream();
        using var streamWriter = new JavaOutputStreamWriter(stream);
        using var jsonWriter = new Java.JsonObjectWriter(streamWriter, javaOptions.MaxDepth);
        sentryEvent.Serialize(jsonWriter, javaOptions.Logger);
        jsonWriter.Flush();
        stream.Seek(0, SeekOrigin.Begin);

        using var json = JsonDocument.Parse(stream);
        return SentryEvent.FromJson(json.RootElement, sentryEvent.Throwable);
    }

    public static Java.SentryEvent ToJavaSentryEvent(this SentryEvent sentryEvent)
    {
        var timestamp = sentryEvent.Timestamp.ToJavaDate();
        var result = new Java.SentryEvent(timestamp)
        {
            Throwable = sentryEvent.Exception?.ToThrowable(),
            EventId = sentryEvent.EventId.ToJavaSentryId(),
            Message = sentryEvent.Message?.ToJavaMessage(),
            Logger = sentryEvent.Logger,
            Platform = sentryEvent.Platform,
            ServerName = sentryEvent.ServerName,
            Release = sentryEvent.Release,
            Exceptions = sentryEvent.SentryExceptions?.Select(x => x.ToJavaSentryException()).ToList(),
            Threads = sentryEvent.SentryThreads?.Select(x => x.ToJavaSentryThread()).ToList(),
            DebugMeta = sentryEvent.DebugImages?.ToJavaDebugMeta(sentryEvent.Sdk),
            Level = sentryEvent.Level?.ToJavaSentryLevel(),
            Transaction = sentryEvent.TransactionName,
            User = sentryEvent.User.ToJavaUser(),
            Environment = sentryEvent.Environment,
            Sdk = sentryEvent.Sdk.ToJavaSdkVersion(),
            Fingerprints = sentryEvent.Fingerprint.ToList(),
            Breadcrumbs = sentryEvent.Breadcrumbs.Select(x => x.ToJavaBreadcrumb()).ToList(),
            Tags = sentryEvent.Tags.ToDictionary(x => x.Key, x => x.Value)
        };

        // NOTE: We don't bother mapping the Request property because it's not used in Android applications.

        result.SetModules(sentryEvent.Modules);
        result.SetExtras(sentryEvent.Extra.ToDictionary(x => x.Key, x => (JavaObject)x.Value!));

        // TODO: Does this work? Or do we need to translate each type of item value as well?
        result.Contexts.PutAll(sentryEvent.Contexts);

        return result;
    }
}
