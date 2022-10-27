using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using Sentry.Extensibility;
using Sentry.Infrastructure;
using Sentry.Internal;
using Sentry.Internal.Extensions;
using Sentry.Internal.Http;
using Sentry.Protocol.Envelopes;

namespace Sentry.Http;

/// <summary>
/// Provides a base class for Sentry HTTP transports.  Used internally by the Sentry SDK,
/// but also allows for higher-level SDKs (such as Unity) to implement their own transport.
/// </summary>
public abstract class HttpTransportBase
{
    internal const string DefaultErrorMessage = "No message";

    private readonly SentryOptions _options;
    private readonly ISystemClock _clock;
    private readonly Func<string, string?> _getEnvironmentVariable;

    // Keep track of last discarded session init so that we can promote the next update.
    // We only track one because session updates are ordered.
    // Using string instead of SentryId here so that we can use Interlocked.Exchange(...).
    private string? _lastDiscardedSessionInitId;

    /// <summary>
    /// Constructor for this class.
    /// </summary>
    /// <param name="options">The Sentry options.</param>
    /// <param name="getEnvironmentVariable">An optional method used to read environment variables.</param>
    /// <param name="clock">An optional system clock - used for testing.</param>
    protected HttpTransportBase(SentryOptions options,
        Func<string, string?>? getEnvironmentVariable = default,
        ISystemClock? clock = default)
    {
        _options = options;
        _clock = clock ?? SystemClock.Clock;
        _getEnvironmentVariable = getEnvironmentVariable ?? options.SettingLocator.GetEnvironmentVariable;
    }

    // Keep track of rate limits and their expiry dates.
    // Internal for testing.
    internal ConcurrentDictionary<RateLimitCategory, DateTimeOffset> CategoryLimitResets { get; } = new();

    /// <summary>
    /// Processes an envelope before sending.
    /// Repackages the original envelope discarding items that don't fit the rate limit.
    /// </summary>
    /// <param name="envelope">The envelope to process.</param>
    /// <returns>The processed envelope, ready to be sent.</returns>
    protected internal Envelope ProcessEnvelope(Envelope envelope)
    {
        var now = _clock.GetUtcNow();

        // Re-package envelope, discarding items that don't fit the rate limit
        var envelopeItems = new List<EnvelopeItem>();
        foreach (var envelopeItem in envelope.Items)
        {
            ProcessEnvelopeItem(now, envelopeItem, envelopeItems);
        }

        var eventId = envelope.TryGetEventId(_options.DiagnosticLogger);

        var clientReport = _options.ClientReportRecorder.GenerateClientReport();
        if (clientReport != null)
        {
            envelopeItems.Add(EnvelopeItem.FromClientReport(clientReport));
            _options.LogDebug("Attached client report to envelope {0}.", eventId);
        }

        if (envelopeItems.Count == 0)
        {
            if (_options.SendClientReports)
            {
                _options.LogInfo("Envelope {0} was discarded because all contained items are rate-limited " +
                                 "and there are no client reports to send.",
                    eventId);
            }
            else
            {
                _options.LogInfo("Envelope {0} was discarded because all contained items are rate-limited.",
                    eventId);
            }
        }

        return new Envelope(envelope.Header, envelopeItems);
    }

    private void ProcessEnvelopeItem(DateTimeOffset now, EnvelopeItem item, List<EnvelopeItem> items)
    {
        // Check if there is at least one matching category for this item that is rate-limited
        var isRateLimited = CategoryLimitResets
            .Any(kvp => kvp.Value > now && kvp.Key.Matches(item));

        if (isRateLimited)
        {
            _options.ClientReportRecorder
                .RecordDiscardedEvent(DiscardReason.RateLimitBackoff, item.DataCategory);

            _options.LogDebug(
                "Envelope item of type {0} was discarded because it's rate-limited.",
                item.TryGetType());

            // Check if session update with init=true
            if (item.Payload is JsonSerializable
                {
                    Source: SessionUpdate {IsInitial: true} discardedSessionUpdate
                })
            {
                _lastDiscardedSessionInitId = discardedSessionUpdate.Id.ToString();

                _options.LogDebug(
                    "Discarded envelope item containing initial session update (SID: {0}).",
                    discardedSessionUpdate.Id);
            }

            return;
        }

        // If attachment, needs to respect attachment size limit
        if (string.Equals(item.TryGetType(), "attachment", StringComparison.OrdinalIgnoreCase) &&
            item.TryGetLength() > _options.MaxAttachmentSize)
        {
            // note: attachment drops are not currently counted in discarded events

            _options.LogWarning(
                "Attachment '{0}' dropped because it's too large ({1} bytes).",
                item.TryGetFileName(),
                item.TryGetLength());

            return;
        }

        // If it's a session update (not discarded) with init=false, check if it continues
        // a session with previously dropped init and, if so, promote this update to init=true.
        if (item.Payload is JsonSerializable {Source: SessionUpdate {IsInitial: false} sessionUpdate} &&
            string.Equals(sessionUpdate.Id.ToString(),
                Interlocked.Exchange(ref _lastDiscardedSessionInitId, null),
                StringComparison.Ordinal))
        {
            var modifiedEnvelopeItem = new EnvelopeItem(
                item.Header,
                new JsonSerializable(new SessionUpdate(sessionUpdate, true)));

            items.Add(modifiedEnvelopeItem);

            _options.LogDebug(
                "Promoted envelope item with session update to initial following a discarded update (SID: {0}).",
                sessionUpdate.Id);

            return;
        }

        // Finally, add this item to the result
        items.Add(item);
    }

    /// <summary>
    /// Creates an HTTP request message from an envelope.
    /// </summary>
    /// <param name="envelope">The envelope.</param>
    /// <returns>An HTTP request message, with the proper headers and body set.</returns>
    /// <exception cref="InvalidOperationException">Throws if the DSN is not set in the options.</exception>
    protected internal HttpRequestMessage CreateRequest(Envelope envelope)
    {
        if (string.IsNullOrWhiteSpace(_options.Dsn))
        {
            throw new InvalidOperationException("The DSN is expected to be set at this point.");
        }

        var dsn = Dsn.Parse(_options.Dsn);
        var authHeader =
            $"Sentry sentry_version={_options.SentryVersion}," +
            $"sentry_client={SdkVersion.Instance.Name}/{SdkVersion.Instance.Version}," +
            $"sentry_key={dsn.PublicKey}" +
            (dsn.SecretKey is { } secretKey ? $",sentry_secret={secretKey}" : null);

        return new HttpRequestMessage
        {
            RequestUri = dsn.GetEnvelopeEndpointUri(),
            Method = HttpMethod.Post,
            Headers = {{"X-Sentry-Auth", authHeader}},
            Content = new EnvelopeHttpContent(envelope, _options.DiagnosticLogger, _clock)
        };
    }

    /// <summary>
    /// Synchronously handles the response message after it is received, extracting any information from the
    /// response such as rate limits, or error messages.
    /// </summary>
    /// <param name="response">The response message received from Sentry.</param>
    /// <param name="envelope">The envelope that was being sent.</param>
    protected void HandleResponse(HttpResponseMessage response, Envelope envelope)
    {
        // Read & set rate limits for future requests
        ExtractRateLimits(response.Headers);

        // Handle results
        if (response.StatusCode == HttpStatusCode.OK)
        {
            HandleSuccess(envelope);
        }
        else
        {
            HandleFailure(response, envelope);
        }
    }

    /// <summary>
    /// Asynchronously handles the response message after it is received, extracting any information from the
    /// response such as rate limits, or error messages.
    /// </summary>
    /// <param name="response">The response message received from Sentry.</param>
    /// <param name="envelope">The envelope that was being sent.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    protected Task HandleResponseAsync(HttpResponseMessage response, Envelope envelope, CancellationToken cancellationToken)
    {
        // Read & set rate limits for future requests
        ExtractRateLimits(response.Headers);

        // Handle results
        if (response.StatusCode == HttpStatusCode.OK)
        {
            return HandleSuccessAsync(envelope, cancellationToken);
        }

        return HandleFailureAsync(response, envelope, cancellationToken);
    }

    /// <summary>
    /// Reads a stream from an HTTP content object.
    /// </summary>
    /// <param name="content">The HTTP content object to read from.</param>
    /// <returns>A stream of the content.</returns>
    /// <remarks>
    /// This is a helper method that allows higher-level APIs to serialize content synchronously
    /// without exposing our custom <see cref="EnvelopeHttpContent"/> type.
    /// </remarks>
    protected Stream ReadStreamFromHttpContent(HttpContent content)
    {
        return content.ReadAsStream();
    }

    private void ExtractRateLimits(HttpHeaders responseHeaders)
    {
        if (!responseHeaders.TryGetValues("X-Sentry-Rate-Limits", out var rateLimitHeaderValues))
        {
            return;
        }

        var now = _clock.GetUtcNow();

        // Join to a string to handle both single-header and multi-header cases
        var rateLimitsEncoded = string.Join(",", rateLimitHeaderValues);

        // Parse and order by retry-after so that the longer rate limits are set last (and not overwritten)
        var rateLimits = RateLimit.ParseMany(rateLimitsEncoded).OrderBy(rl => rl.RetryAfter);

        // Persist rate limits
        foreach (var rateLimit in rateLimits)
        {
            foreach (var rateLimitCategory in rateLimit.Categories)
            {
                CategoryLimitResets[rateLimitCategory] = now + rateLimit.RetryAfter;
            }
        }
    }

    private void HandleSuccess(Envelope envelope)
    {
        if (_options.DiagnosticLogger?.IsEnabled(SentryLevel.Debug) is true)
        {
            var payload = envelope.SerializeToString(_options.DiagnosticLogger, _clock);
            LogEnvelopeSent(envelope, payload);
        }
        else
        {
            LogEnvelopeSent(envelope);
        }
    }

    private async Task HandleSuccessAsync(Envelope envelope, CancellationToken cancellationToken)
    {
        if (_options.DiagnosticLogger?.IsEnabled(SentryLevel.Debug) is true)
        {
            var payload = await envelope.SerializeToStringAsync(_options.DiagnosticLogger, _clock, cancellationToken)
                .ConfigureAwait(false);

            LogEnvelopeSent(envelope, payload);
        }
        else
        {
            LogEnvelopeSent(envelope);
        }
    }

    private void LogEnvelopeSent(Envelope envelope, string? payload = null)
    {
        var eventId = envelope.TryGetEventId(_options.DiagnosticLogger);
        if (payload == null)
        {
            if (eventId == null)
            {
                _options.LogInfo("Envelope successfully sent.", eventId);
            }
            else
            {
                _options.LogInfo("Envelope '{0}' successfully sent.", eventId);
            }
        }
        else
        {
            if (eventId == null)
            {
                _options.LogDebug("Envelope successfully sent. Content: {1}", eventId, payload);
            }
            else
            {
                _options.LogDebug("Envelope '{0}' successfully sent. Content: {1}", eventId, payload);
            }
        }
    }

    private void HandleFailure(HttpResponseMessage response, Envelope envelope)
    {
        IncrementDiscardsForHttpFailure(response.StatusCode, envelope);

        var eventId = envelope.TryGetEventId(_options.DiagnosticLogger);

        // Spare the overhead if level is not enabled
        if (_options.DiagnosticLogger?.IsEnabled(SentryLevel.Error) is true && response.Content is { } content)
        {
            if (HasJsonContent(content))
            {
                var responseJson = content.ReadAsJson();
                LogFailure(responseJson, response.StatusCode, eventId);
            }
            else
            {
                var responseString = content.ReadAsString();
                LogFailure(responseString, response.StatusCode, eventId);
            }
        }

        // If debug level, dump the whole envelope to the logger
        if (_options.DiagnosticLogger?.IsEnabled(SentryLevel.Debug) is true)
        {
            var payload = envelope.SerializeToString(_options.DiagnosticLogger, _clock);
            _options.LogDebug("Failed envelope '{0}' has payload:\n{1}\n", eventId, payload);

            // SDK is in debug mode, and envelope was too large. To help troubleshoot:
            const string persistLargeEnvelopePathEnvVar = "SENTRY_KEEP_LARGE_ENVELOPE_PATH";
            if (response.StatusCode == HttpStatusCode.RequestEntityTooLarge
                && _getEnvironmentVariable(persistLargeEnvelopePathEnvVar) is { } destinationDirectory)
            {
                _options.DiagnosticLogger?
                    .LogDebug("Environment variable '{0}' set. Writing envelope to {1}",
                        persistLargeEnvelopePathEnvVar,
                        destinationDirectory);

                var destination = Path.Combine(destinationDirectory, "envelope_too_large",
                    (eventId ?? SentryId.Create()).ToString());

                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);

                var envelopeFile = File.Create(destination);

                using (envelopeFile)
                {
                    envelope.Serialize(envelopeFile, _options.DiagnosticLogger);
                    envelopeFile.Flush();
                    _options.LogInfo("Envelope's {0} bytes written to: {1}",
                        envelopeFile.Length, destination);
                }
            }
        }
    }

    private async Task HandleFailureAsync(HttpResponseMessage response, Envelope envelope,
        CancellationToken cancellationToken)
    {
        IncrementDiscardsForHttpFailure(response.StatusCode, envelope);

        var eventId = envelope.TryGetEventId(_options.DiagnosticLogger);
        // Spare the overhead if level is not enabled
        if (_options.DiagnosticLogger?.IsEnabled(SentryLevel.Error) is true && response.Content is { } content)
        {
            if (HasJsonContent(content))
            {
                var responseJson = await content.ReadAsJsonAsync(cancellationToken).ConfigureAwait(false);
                LogFailure(responseJson, response.StatusCode, eventId);
            }
            else
            {
                var responseString = await content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                LogFailure(responseString, response.StatusCode, eventId);
            }
        }

        // If debug level, dump the whole envelope to the logger
        if (_options.DiagnosticLogger?.IsEnabled(SentryLevel.Debug) is true)
        {
            var payload = await envelope
                .SerializeToStringAsync(_options.DiagnosticLogger, _clock, cancellationToken).ConfigureAwait(false);
            _options.LogDebug("Failed envelope '{0}' has payload:\n{1}\n", eventId, payload);


            // SDK is in debug mode, and envelope was too large. To help troubleshoot:
            const string persistLargeEnvelopePathEnvVar = "SENTRY_KEEP_LARGE_ENVELOPE_PATH";
            if (response.StatusCode == HttpStatusCode.RequestEntityTooLarge
                && _getEnvironmentVariable(persistLargeEnvelopePathEnvVar) is { } destinationDirectory)
            {
                _options.DiagnosticLogger?
                    .LogDebug("Environment variable '{0}' set. Writing envelope to {1}",
                        persistLargeEnvelopePathEnvVar,
                        destinationDirectory);

                var destination = Path.Combine(destinationDirectory, "envelope_too_large",
                    (eventId ?? SentryId.Create()).ToString());

                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);

                var envelopeFile = File.Create(destination);
#if NETFRAMEWORK || NETSTANDARD2_0
                    using (envelopeFile)
#else
                await using (envelopeFile)
#endif
                {
                    await envelope
                        .SerializeAsync(envelopeFile, _options.DiagnosticLogger, cancellationToken)
                        .ConfigureAwait(false);
                    await envelopeFile.FlushAsync(cancellationToken).ConfigureAwait(false);
                    _options.LogInfo("Envelope's {0} bytes written to: {1}",
                        envelopeFile.Length, destination);
                }
            }
        }
    }

    private void IncrementDiscardsForHttpFailure(HttpStatusCode responseStatusCode, Envelope envelope)
    {
        if ((int)responseStatusCode is 429 or < 400)
        {
            //  Status == 429 or < 400 should not be counted by the client SDK
            //  See https://develop.sentry.dev/sdk/client-reports/#sdk-side-recommendations
            return;
        }

        _options.ClientReportRecorder.RecordDiscardedEvents(DiscardReason.NetworkError, envelope);

        // Also restore any counts that were trying to be sent, so they are not lost.
        var clientReportItems = envelope.Items.Where(x => x.TryGetType() == "client_report");
        foreach (var item in clientReportItems)
        {
            var clientReport = (ClientReport)((JsonSerializable)item.Payload).Source;
            _options.ClientReportRecorder.Load(clientReport);
        }
    }

    private void LogFailure(string responseString, HttpStatusCode responseStatusCode, SentryId? eventId)
    {
        _options.Log(SentryLevel.Error,
            "Sentry rejected the envelope {0}. Status code: {1}. Error detail: {2}.",
            null,
            eventId,
            responseStatusCode,
            responseString);
    }

    private void LogFailure(JsonElement responseJson, HttpStatusCode responseStatusCode, SentryId? eventId)
    {
        var errorMessage =
            responseJson.GetPropertyOrNull("detail")?.GetString()
            ?? HttpTransport.DefaultErrorMessage;

        var errorCauses =
            responseJson.GetPropertyOrNull("causes")?.EnumerateArray().Select(j => j.GetString()).ToArray()
            ?? Array.Empty<string>();

        _options.Log(SentryLevel.Error,
            "Sentry rejected the envelope {0}. Status code: {1}. Error detail: {2}. Error causes: {3}.",
            null,
            eventId,
            responseStatusCode,
            errorMessage,
            string.Join(", ", errorCauses));
    }

    private static bool HasJsonContent(HttpContent content) =>
        string.Equals(content.Headers.ContentType?.MediaType, "application/json",
            StringComparison.OrdinalIgnoreCase);
}
