using Sentry.Protocol.Envelopes;

namespace Sentry.Internal.Http;

// A shorter rate limit must not replace a longer active one, see https://develop.sentry.dev/sdk/foundations/transport/rate-limiting/#stage-2-determine-rate-limits
internal sealed class RateLimitResets
{
    private readonly ConcurrentDictionary<RateLimitCategory, DateTimeOffset> _resets = new();

    public void Extend(RateLimitCategory category, DateTimeOffset resetAt) =>
        _resets.AddOrUpdate(category, resetAt, (_, current) => current > resetAt ? current : resetAt);

    public bool IsRateLimited(EnvelopeItem item, DateTimeOffset now) =>
        _resets.Any(kvp => kvp.Value > now && kvp.Key.Matches(item));
}
