using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Sentry.Internal.Http
{
    internal class SentryEnvelopeQuotaLimit
    {
        public IReadOnlyList<string> Categories { get; }

        public TimeSpan RetryAfter { get; }

        public bool IsAllCategories => !Categories.Any();

        public SentryEnvelopeQuotaLimit(IReadOnlyList<string> categories, TimeSpan retryAfter)
        {
            Categories = categories;
            RetryAfter = retryAfter;
        }

        public static SentryEnvelopeQuotaLimit Parse(string quotaLimit)
        {
            // Don't remove empty entries because some components may be empty (e.g. categories)
            var components = quotaLimit.Split(':');

            var retryAfter = TimeSpan.FromSeconds(int.Parse(components[0], CultureInfo.InvariantCulture));
            var categories = components[1].Split(';');

            return new SentryEnvelopeQuotaLimit(
                categories,
                retryAfter
            );
        }
    }
}
