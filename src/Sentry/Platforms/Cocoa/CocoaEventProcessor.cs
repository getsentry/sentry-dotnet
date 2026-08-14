using Sentry.Cocoa.Extensions;
using Sentry.Extensibility;

namespace Sentry.Cocoa;

internal class CocoaEventProcessor : ISentryEventProcessor, IDisposable
{
    public SentryEvent Process(SentryEvent @event)
    {
        // Enrich the event with the native SDK's contexts (device, OS, app, ...). The Cocoa SDK
        // exposes the current scope's contexts in Sentry wire format via the structured hybrid API,
        // so we no longer need a throwaway native event or the private applyToEvent.
        // We leverage the fact that the JSON serialization is compatible, since both SDKs are
        // designed to send the same data to Sentry.
        var json = SentryCocoaHybridSdk.Internal.Scope.SerializedContexts.ToJsonString();
        if (json != null)
        {
            var jsonDoc = JsonDocument.Parse(json);
            var contexts = SentryContexts.FromJson(jsonDoc.RootElement);

            // The native contexts include a "trace" whose ids belong to the Cocoa SDK. This event was
            // captured by the .NET SDK and already carries its own trace context linking it to the
            // managed transaction, so drop the native one rather than let it overwrite ours.
            contexts.Remove("trace");

            contexts.CopyTo(@event.Contexts);
        }

        return @event;
    }

    public void Dispose()
    {
    }
}
