// using Sentry.Extensibility;
// using Sentry.Protocol.Envelopes;
//
// namespace Sentry.Cocoa.Extensions;
//
// internal static class SentryEventExtensions
// {
//     /*
//      * These methods map between a SentryEvent and it's Cocoa counterpart by serializing as JSON into memory on one side,
//      * then deserializing back to an object on the other side.  It is not expected to be performant, as this code is only
//      * used when a BeforeSend option is set, and then only when an event is captured by the Cocoa SDK (which should be
//      * relatively rare).
//      *
//      * This approach avoids having to write to/from methods for the entire object graph.  However, it's also important to
//      * recognize that there's not necessarily a one-to-one mapping available on all objects (even through serialization)
//      * between the two SDKs, so some optional details may be lost when roundtripping.  That's generally OK, as this is
//      * still better than nothing.  If a specific part of the object graph becomes important to roundtrip, we can consider
//      * updating the objects on either side.
//      */
//
//     public static SentryEvent ToSentryEvent(this CocoaSdk.SentryEvent sentryEvent, SentryCocoaOptions cocoaOptions)
//     {
//         using var stream = sentryEvent.ToJsonStream()!;
//         //stream.Seek(0, SeekOrigin.Begin); ??
//
//         using var json = JsonDocument.Parse(stream);
//         var exception = sentryEvent.Error == null ? null : new NSErrorException(sentryEvent.Error);
//         return SentryEvent.FromJson(json.RootElement, exception);
//     }
//
//     public static CocoaSdk.SentryEvent ToCocoaSentryEvent(this SentryEvent sentryEvent, SentryOptions options, SentryCocoaOptions cocoaOptions)
//     {
//         var envelope = Envelope.FromEvent(sentryEvent);
//
//         using var stream = new MemoryStream();
//         envelope.Serialize(stream, options.DiagnosticLogger);
//         stream.Seek(0, SeekOrigin.Begin);
//
//         using var data = NSData.FromStream(stream)!;
//         var cocoaEnvelope = CocoaSdk.PrivateSentrySDKOnly.EnvelopeWithData(data);
//
//         var cocoaEvent = (CocoaSdk.SentryEvent) cocoaEnvelope.Items[0];
//         return cocoaEvent;
//     }
// }
