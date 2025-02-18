using Sentry.Extensibility;
using Sentry.Internal.Extensions;
using Sentry.Protocol.Envelopes;

namespace Sentry.Cocoa.Extensions;

internal static class SentryEventExtensions
{
    /*
     * These methods map between a SentryEvent and it's Cocoa counterpart by serializing as JSON into memory on one side,
     * then deserializing back to an object on the other side.  It is not expected to be performant, as this code is only
     * used when a BeforeSend option is set, and then only when an event is captured by the Cocoa SDK (which should be
     * relatively rare).
     *
     * This approach avoids having to write to/from methods for the entire object graph.  However, it's also important to
     * recognize that there's not necessarily a one-to-one mapping available on all objects (even through serialization)
     * between the two SDKs, so some optional details may be lost when roundtripping.  That's generally OK, as this is
     * still better than nothing.  If a specific part of the object graph becomes important to roundtrip, we can consider
     * updating the objects on either side.
     */

    public static SentryEvent? ToSentryEvent(this CocoaSdk.SentryEvent sentryEvent)
    {
        using var stream = sentryEvent.ToJsonStream();
        if (stream == null)
            return null;

        using var json = JsonDocument.Parse(stream);
        var exception = sentryEvent.Error == null ? null : new NSErrorException(sentryEvent.Error);
        var ev = SentryEvent.FromJson(json.RootElement, exception);
        return ev;
    }

    public static CocoaSdk.SentryEvent ToCocoaSentryEvent(this SentryEvent sentryEvent, SentryOptions options)
    {
        var native = new CocoaSdk.SentryEvent();

        native.ServerName = sentryEvent.ServerName;
        native.Dist = sentryEvent.Distribution;
        native.Logger = sentryEvent.Logger;
        native.ReleaseName = sentryEvent.Release;
        native.Environment = sentryEvent.Environment;
        native.Platform = sentryEvent.Platform!;
        native.Transaction = sentryEvent.TransactionName!;
        native.Fingerprint = sentryEvent.Fingerprint?.ToArray();
        native.Timestamp = sentryEvent.Timestamp.ToNSDate();
        native.Modules = sentryEvent.Modules.ToDictionary(kv => kv.Key, kv => kv.Value);

        native.Tags = sentryEvent.Tags?.ToDictionary(kv => kv.Key, kv => kv.Value);
        native.EventId = sentryEvent.EventId.ToCocoaSentryId();
        native.Extra = sentryEvent.Extra?.ToDictionary(kv => kv.Key, kv => kv.Value);
        native.Breadcrumbs = sentryEvent.Breadcrumbs?.Select(x => x.ToCocoaBreadcrumb()).ToArray();
        native.User = sentryEvent.User?.ToCocoaUser();
        // native.Error = NSError.FromDomain() sentryEvent.Exception
        //sentryEvent.Request;
        // native.Level = sentryEvent.Level;
        // native.Sdk = sentryEvent.Sdk
        // native.Context = sentryEvent.Contexts;
        // sentryEvent.DebugImages
        // sentryEvent.SentryExceptions
        // native.Type = sentryEvent.T
        // native.Message = sentryEvent.Message
        // native.Threads = sentryEvent.SentryThreads

        return native;
    }
}
// var envelope = Envelope.FromEvent(sentryEvent);
// var native = new CocoaSdk.SentryEvent();
//
// using var stream = new MemoryStream();
// envelope.Serialize(stream, options.DiagnosticLogger);
// stream.Seek(0, SeekOrigin.Begin);
//
// using var data = NSData.FromStream(stream)!;
// var cocoaEnvelope = CocoaSdk.PrivateSentrySDKOnly.EnvelopeWithData(data);
//
// var cocoaEvent = (CocoaSdk.SentryEvent)cocoaEnvelope.Items.GetItem<CocoaSdk.SentryEvent>(0);
// // this will return a SentryEnvelopeItem which has isCrashEvent that causes native serialization panic
// return cocoaEvent;
