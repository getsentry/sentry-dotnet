using System.Text.Json;

namespace Sentry.AspNetCore.Blazor.WebAssembly.PlaywrightTests;

internal static class SentryEnvelopeParser
{
    /// <summary>
    /// Extracts the first event payload from a Sentry envelope body.
    /// Envelope format: newline-delimited JSON lines.
    /// Line 0 = envelope header, then pairs of (item header, item payload).
    /// </summary>
    public static JsonElement? ExtractEventFromEnvelope(string envelopeBody)
    {
        var lines = envelopeBody.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        // lines[0] = envelope header
        // lines[1..] = pairs of (item header, item payload)
        for (var i = 1; i < lines.Length - 1; i += 2)
        {
            using var itemHeaderDoc = JsonDocument.Parse(lines[i]);
            var itemHeader = itemHeaderDoc.RootElement;

            if (itemHeader.TryGetProperty("type", out var typeEl) &&
                typeEl.GetString() == "event")
            {
                return JsonDocument.Parse(lines[i + 1]).RootElement;
            }
        }

        return null;
    }
}
