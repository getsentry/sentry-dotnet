namespace Sentry.Internal.Http;

internal class RateLimit
{
    public IReadOnlyList<RateLimitCategory> Categories { get; }

    public IReadOnlyList<string>? Namespaces { get; }

    internal bool IsDefaultNamespace =>
        Namespaces is null ||
        (Namespaces.Count == 1 && string.Equals(Namespaces[0], "custom", StringComparison.OrdinalIgnoreCase));

    public TimeSpan RetryAfter { get; }

    public RateLimit(TimeSpan retryAfter, IReadOnlyList<RateLimitCategory> categories, IReadOnlyList<string>? namespaces = null)
    {
        RetryAfter = retryAfter;
        Categories = categories;
        Namespaces = namespaces;
    }

    public static RateLimit Parse(string rateLimitEncoded)
    {
        // Don't remove empty entries because some components may be empty (e.g. categories)
        var components = rateLimitEncoded.Split(':');

        var retryAfter = TimeSpan.FromSeconds(int.Parse(components[0], CultureInfo.InvariantCulture));
        var categories = components[1].Split(';').Select(c => new RateLimitCategory(c)).ToArray();
        string[]? namespaces = null;
        foreach (var category in categories)
        {
            if (!string.Equals(category.Name, "metric_bucket", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // Response header looking like this: X-Sentry-Rate-Limits: 2700:metric_bucket:organization:quota_exceeded:custom
            namespaces = components.Length > 4 ? components[4].Split(';') : null;
            break;
        }

        return new RateLimit(retryAfter, categories, namespaces);
    }

    public static IEnumerable<RateLimit> ParseMany(string rateLimitsEncoded) =>
        rateLimitsEncoded.Split(',').Select(Parse);
}
