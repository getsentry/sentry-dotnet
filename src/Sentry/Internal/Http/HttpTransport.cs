using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Sentry.Extensibility;
using Sentry.Infrastructure;
using Sentry.Internal.Extensions;
using Sentry.Protocol.Envelopes;

namespace Sentry.Internal.Http
{
    /// <summary>
    /// Internal HTTP Transport logic implementation. This is only meant to be used by this and other Sentry SDKs.
    /// </summary>
    public class HttpTransport : ITransport
    {
        private readonly SentryOptions _options;
        private readonly HttpClient _httpClient;
        private readonly ISystemClock _clock = new SystemClock();

        private readonly Func<string, string?> _getEnvironmentVariable;

        // Keep track of rate limits and their expiry dates.
        // Internal for testing.
        internal ConcurrentDictionary<RateLimitCategory, DateTimeOffset> CategoryLimitResets { get; } = new();

        // Keep track of last discarded session init so that we can promote the next update.
        // We only track one because session updates are ordered.
        // Using string instead of SentryId here so that we can use Interlocked.Exchange(...).
        private string? _lastDiscardedSessionInitId;

        internal const string DefaultErrorMessage = "No message";

        /// <summary>
        /// Creates the internal HTTP Transport with the given option and HttpClient implementation.
        /// </summary>
        public HttpTransport(SentryOptions options, HttpClient httpClient)
            : this(options, httpClient, Environment.GetEnvironmentVariable)
        {
        }

        internal HttpTransport(SentryOptions options, HttpClient httpClient,
            Func<string, string?> getEnvironmentVariable)
        {
            _options = options;
            _httpClient = httpClient;
            _getEnvironmentVariable = getEnvironmentVariable;
        }

        /// <summary>
        /// Re-package the envelope, discarding items that don't fit the rate limit
        /// </summary>
        protected Envelope ProcessEnvelope(Envelope envelope, DateTimeOffset instant)
        {
            var envelopeItems = new List<EnvelopeItem>();
            foreach (var envelopeItem in envelope.Items)
            {
                // Check if there is at least one matching category for this item that is rate-limited
                var isRateLimited = CategoryLimitResets
                    .Any(kvp => kvp.Value > instant && kvp.Key.Matches(envelopeItem));

                if (isRateLimited)
                {
                    _options.LogDebug(
                        "Envelope item of type {0} was discarded because it's rate-limited.",
                        envelopeItem.TryGetType());

                    // Check if session update with init=true
                    if (envelopeItem.Payload is JsonSerializable { Source: SessionUpdate { IsInitial: true } discardedSessionUpdate })
                    {
                        _lastDiscardedSessionInitId = discardedSessionUpdate.Id.ToString();

                        _options.LogDebug(
                            "Discarded envelope item containing initial session update (SID: {0}).",
                            discardedSessionUpdate.Id);
                    }

                    continue;
                }

                // If attachment, needs to respect attachment size limit
                if (string.Equals(envelopeItem.TryGetType(), "attachment", StringComparison.OrdinalIgnoreCase) &&
                    envelopeItem.TryGetLength() > _options.MaxAttachmentSize)
                {
                    _options.LogWarning(
                        "Attachment '{0}' dropped because it's too large ({1} bytes).",
                        envelopeItem.TryGetFileName(),
                        envelopeItem.TryGetLength());

                    continue;
                }

                // If it's a session update (not discarded) with init=false, check if it continues
                // a session with previously dropped init and, if so, promote this update to init=true.
                if (envelopeItem.Payload is JsonSerializable { Source: SessionUpdate { IsInitial: false } sessionUpdate } &&
                    string.Equals(sessionUpdate.Id.ToString(), Interlocked.Exchange(ref _lastDiscardedSessionInitId, null),
                        StringComparison.Ordinal))
                {
                    var modifiedEnvelopeItem = new EnvelopeItem(
                        envelopeItem.Header,
                        new JsonSerializable(new SessionUpdate(sessionUpdate, true)));

                    envelopeItems.Add(modifiedEnvelopeItem);

                    _options.LogDebug(
                        "Promoted envelope item with session update to initial following a discarded update (SID: {0}).",
                        sessionUpdate.Id);
                }
                else
                {
                    envelopeItems.Add(envelopeItem);
                }
            }

            if (envelopeItems.Count == 0)
            {
                _options.LogInfo(
                    "Envelope {0} was discarded because all contained items are rate-limited.",
                    envelope.TryGetEventId());
            }

            return new Envelope(envelope.Header, envelopeItems);
        }

        /// <summary>
        /// Update local rate limits based on the response from the server.
        /// </summary>
        protected void ExtractRateLimits(HttpResponseMessage response, DateTimeOffset instant)
        {
            if (!response.Headers.TryGetValues("X-Sentry-Rate-Limits", out var rateLimitHeaderValues))
            {
                return;
            }

            // Join to a string to handle both single-header and multi-header cases
            var rateLimitsEncoded = string.Join(",", rateLimitHeaderValues);

            // Parse and order by retry-after so that the longer rate limits are set last (and not overwritten)
            var rateLimits = RateLimit.ParseMany(rateLimitsEncoded).OrderBy(rl => rl.RetryAfter);

            // Persist rate limits
            foreach (var rateLimit in rateLimits)
            {
                foreach (var rateLimitCategory in rateLimit.Categories)
                {
                    CategoryLimitResets[rateLimitCategory] = instant + rateLimit.RetryAfter;
                }
            }
        }

        /// <inheritdoc/>
        public async Task SendEnvelopeAsync(Envelope envelope, CancellationToken cancellationToken = default)
        {
            var instant = DateTimeOffset.Now;

            // Apply rate limiting and re-package envelope items
            using var processedEnvelope = ProcessEnvelope(envelope, instant);
            if (processedEnvelope.Items.Count == 0)
            {
                return;
            }

            // Send envelope to ingress
            using var request = CreateRequest(processedEnvelope);
            using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

            // Read & set rate limits for future requests
            ExtractRateLimits(response, instant);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                await HandleFailureAsync(response, processedEnvelope, cancellationToken).ConfigureAwait(false);
            }
            else if (_options.DiagnosticLogger?.IsEnabled(SentryLevel.Debug) is true)
            {
                _options.LogDebug("Envelope '{0}' sent successfully. Payload:\n{1}",
                    envelope.TryGetEventId(),
                    await envelope.SerializeToStringAsync(_options.DiagnosticLogger, cancellationToken).ConfigureAwait(false));
            }
            else
            {
                _options.LogInfo("Envelope '{0}' successfully received by Sentry.", processedEnvelope.TryGetEventId());
            }
        }

        private async Task HandleFailureAsync(
            HttpResponseMessage response,
            Envelope processedEnvelope,
            CancellationToken cancellationToken)
        {
            // Spare the overhead if level is not enabled
            if (_options.DiagnosticLogger?.IsEnabled(SentryLevel.Error) is true && response.Content is { } content)
            {
                if (string.Equals(content.Headers.ContentType?.MediaType, "application/json",
                    StringComparison.OrdinalIgnoreCase))
                {
                    LogFailure(response, processedEnvelope,
                        await content.ReadAsJsonAsync(cancellationToken).ConfigureAwait(false));
                }
                else
                {
                    LogFailure(response, processedEnvelope,
                        await content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false));
                }

                // If debug level, dump the whole envelope to the logger
                if (_options.DiagnosticLogger?.IsEnabled(SentryLevel.Debug) is true)
                {
                    _options.LogDebug("Failed envelope '{0}' has payload:\n{1}\n",
                        processedEnvelope.TryGetEventId(),
                        await processedEnvelope.SerializeToStringAsync(_options.DiagnosticLogger, cancellationToken).ConfigureAwait(false));
                }
            }

            // SDK is in debug mode, and envelope was too large. To help troubleshoot:
            const string persistLargeEnvelopePathEnvVar = "SENTRY_KEEP_LARGE_ENVELOPE_PATH";
            if (_options.DiagnosticLogger?.IsEnabled(SentryLevel.Debug) is true
                && response.StatusCode == HttpStatusCode.RequestEntityTooLarge
                && _getEnvironmentVariable(persistLargeEnvelopePathEnvVar) is { } destinationDirectory)
            {
                _options.DiagnosticLogger?
                    .LogDebug("Environment variable '{0}' set. Writing envelope to {1}",
                        persistLargeEnvelopePathEnvVar,
                        destinationDirectory);

                var destination = Path.Combine(destinationDirectory, "envelope_too_large",
                    (processedEnvelope.TryGetEventId() ?? SentryId.Create()).ToString());

                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);

                var envelopeFile = File.Create(destination);
#if NET461 || NETSTANDARD2_0
                using (envelopeFile)
#else
                await using (envelopeFile)
#endif
                {
                    await processedEnvelope.SerializeAsync(envelopeFile, _options.DiagnosticLogger, cancellationToken).ConfigureAwait(false);
                    await envelopeFile.FlushAsync(cancellationToken).ConfigureAwait(false);
                    _options.LogInfo("Envelope's {0} bytes written to: {1}",
                        envelopeFile.Length, destination);
                }
            }
        }

        /// <summary>
        /// Log failure response.
        /// </summary>
        protected void LogFailure(HttpResponseMessage response, Envelope processedEnvelope, JsonElement responseJson)
        {
            var errorMessage =
                responseJson.GetPropertyOrNull("detail")?.GetString()
                ?? DefaultErrorMessage;

            var errorCauses =
                responseJson.GetPropertyOrNull("causes")?.EnumerateArray().Select(j => j.GetString()).ToArray()
                ?? Array.Empty<string>();

            _options.Log(
                SentryLevel.Error,
                "Sentry rejected the envelope {0}. Status code: {1}. Error detail: {2}. Error causes: {3}.",
                null,
                processedEnvelope.TryGetEventId(),
                response.StatusCode,
                errorMessage,
                string.Join(", ", errorCauses));
        }

        /// <summary>
        /// Log failure response.
        /// </summary>
        protected void LogFailure(HttpResponseMessage response, Envelope processedEnvelope, string responseString)
        {
            _options.Log(
                SentryLevel.Error,
                "Sentry rejected the envelope {0}. Status code: {1}. Error detail: {2}.",
                null,
                processedEnvelope.TryGetEventId(),
                response.StatusCode,
                responseString);
        }

        /// <summary>
        /// Create HTTP request for the envelope.
        /// </summary>
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
                $"sentry_key={dsn.PublicKey}," +
                (dsn.SecretKey is { } secretKey ? $"sentry_secret={secretKey}," : null) +
                $"sentry_timestamp={_clock.GetUtcNow().ToUnixTimeSeconds()}";

            return new HttpRequestMessage
            {
                RequestUri = dsn.GetEnvelopeEndpointUri(),
                Method = HttpMethod.Post,
                Headers = { { "X-Sentry-Auth", authHeader } },
                Content = new EnvelopeHttpContent(envelope, _options.DiagnosticLogger)
            };
        }
    }
}
