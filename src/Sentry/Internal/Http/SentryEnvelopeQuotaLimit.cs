using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Sentry.Internal.Http
{
    internal class SentryEnvelopeQuotaLimit
    {
        public TimeSpan RetryAfter { get; }

        public IReadOnlyList<string> Categories { get; }

        public bool IsAllCategories => !Categories.Any();

        public SentryEnvelopeQuotaLimit(TimeSpan retryAfter, IReadOnlyList<string> categories)
        {
            RetryAfter = retryAfter;
            Categories = categories;
        }

        public static SentryEnvelopeQuotaLimit Parse(string quotaLimit)
        {
            // Don't remove empty entries because some components may be empty (e.g. categories)
            var components = quotaLimit.Split(':');

            var retryAfter = TimeSpan.FromSeconds(int.Parse(components[0], CultureInfo.InvariantCulture));
            var categories = components[1].Split(';');

            return new SentryEnvelopeQuotaLimit(
                retryAfter,
                categories
            );
        }
    }
}
